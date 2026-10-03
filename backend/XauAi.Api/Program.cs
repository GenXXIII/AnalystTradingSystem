using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using XauAi.Api.Configuration;
using XauAi.Api.Endpoints;
using XauAi.Api.Middleware;
using XauAi.Api.Models;
using XauAi.Application;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Configuration;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Persistence.Extensions;
using AspNetCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddXauAiEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "XAUUSD AI API",
        Version = "v1",
        Description = "Normalized market, technical, news, and economic evidence for the XAUUSD AI Trading Intelligence System."
    });
});

builder.Services.AddCors();
builder.Services.AddSingleton<IConfigureOptions<AspNetCorsOptions>, ApiCorsOptionsSetup>();

var app = builder.Build();
await app.Services.MigrateDatabaseAsync();

if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase))
{
    app.Logger.LogInformation("Database migrations completed successfully; migration-only process is exiting");
    return;
}

var apiOptions = app.Services.GetRequiredService<IOptions<ApiOptions>>().Value;
var applicationOptions = app.Services.GetRequiredService<IOptions<ApplicationOptions>>().Value;

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();

if (apiOptions.EnableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponseAsync
});

app.MapSystemStatusEndpoints();
app.MapMt5Endpoints();
app.MapMarketDataEndpoints();
app.MapTechnicalAnalysisEndpoints();
app.MapNewsEndpoints();
app.MapEconomicDataEndpoints();

app.MapFallback((HttpContext context) => Results.Json(
    ApiErrorResponse.Create(
        "RESOURCE_NOT_FOUND",
        "The requested resource was not found.",
        context.TraceIdentifier),
    statusCode: StatusCodes.Status404NotFound));

if (app.Logger.IsEnabled(LogLevel.Information))
{
    app.Logger.LogInformation(
        "{ApplicationName} {ApplicationVersion} starting in {Environment}",
        applicationOptions.Name,
        applicationOptions.Version,
        app.Environment.EnvironmentName);
}

app.Run();

static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            state = entry.Value.Data.TryGetValue("state", out var state) ? state : null,
            enabled = entry.Value.Data.TryGetValue("enabled", out var enabled) ? enabled : null,
            connected = entry.Value.Data.TryGetValue("connected", out var connected) ? connected : null
        }),
        traceId = context.TraceIdentifier
    };

    return context.Response.WriteAsJsonAsync(response);
}
