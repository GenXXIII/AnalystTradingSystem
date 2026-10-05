using System.Net;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Api.Middleware;

namespace XauAi.IntegrationTests;

public sealed class ApiFoundationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_endpoint_starts_and_returns_healthy()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.True(response.Headers.Contains(CorrelationIdMiddleware.HeaderName));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Status_endpoint_preserves_a_safe_correlation_id()
    {
        const string correlationId = "integration-test-001";
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        using var response = await client.GetAsync("/api/system/status");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.Equal("operational", body.GetProperty("data").GetProperty("status").GetString());
        Assert.Equal(correlationId, body.GetProperty("traceId").GetString());
        Assert.Equal(correlationId, response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task Unknown_route_uses_the_safe_error_contract()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/does-not-exist");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("RESOURCE_NOT_FOUND", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Market_data_provider_status_is_safe_and_disabled_by_default()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/market-data/provider/status");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Disabled", body.GetProperty("data").GetProperty("state").GetString());
        Assert.False(body.GetProperty("data").GetProperty("enabled").GetBoolean());
        Assert.False(body.GetProperty("data").TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Market_quote_returns_safe_disabled_error_by_default()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/market/xauusd/quote");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("MARKET_DATA_PROVIDER_DISABLED", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Market_data_query_returns_safe_database_unavailable_error_by_default()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/market-data/XAUUSD/status?timeframe=H1");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("DATABASE_DISABLED", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Analyst_status_is_safe_and_disabled_by_default()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/analyst-predictions/status");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Disabled", body.GetProperty("data").GetProperty("provider").GetProperty("state").GetString());
        Assert.False(body.GetProperty("data").GetProperty("provider").GetProperty("enabled").GetBoolean());
        Assert.Equal(0, body.GetProperty("data").GetProperty("storedPredictions").GetInt32());
        Assert.False(body.GetProperty("data").TryGetProperty("apiKey", out _));
    }

    [Fact]
    public async Task Analyst_prediction_query_enforces_maximum_page_size()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/analyst-predictions?pageSize=501");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("ANALYST_REQUEST_INVALID", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Evidence_query_returns_safe_database_unavailable_error_by_default()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/evidence?instrument=XAUUSD");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("EVIDENCE_DATABASE_DISABLED", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Evidence_pack_rejects_future_analysis_time_before_querying_storage()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/evidence/pack?instrument=XAUUSD&primaryTimeframe=M15&analysisTime=2099-01-01T00%3A00%3A00Z");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("EVIDENCE_REQUEST_INVALID", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Ai_specialist_configuration_is_independent_safe_and_disabled_by_default()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/ai-interpretations/specialists");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var specialists = body.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, specialists.GetArrayLength());
        Assert.All(specialists.EnumerateArray(), specialist =>
        {
            Assert.False(specialist.GetProperty("enabled").GetBoolean());
            Assert.False(specialist.GetProperty("hasApiKey").GetBoolean());
            Assert.False(specialist.TryGetProperty("apiKey", out _));
        });
    }

    [Fact]
    public async Task Ai_interpretation_rejects_future_analysis_time_before_provider_or_storage_access()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/ai-interpretations", new
        {
            instrument = "XAUUSD",
            specialist = "news",
            interpretationType = "newsEvent",
            analysisTimeUtc = "2099-01-01T00:00:00Z",
            lookbackHours = 24
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("AI_INTERPRETATION_REQUEST_INVALID", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Ai_interpretation_query_returns_safe_database_disabled_error_by_default()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/ai-interpretations?instrument=XAUUSD");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("AI_INTERPRETATION_DATABASE_DISABLED", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Server_secrets_are_absent_from_responses_and_logs()
    {
        const string sentinelSecret = "integration-secret-must-never-leak";
        var loggerProvider = new RecordingLoggerProvider();
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("AiSpecialists:News:ApiKey", sentinelSecret);
            builder.ConfigureLogging(logging => logging.AddProvider(loggerProvider));
        });
        using var client = configuredFactory.CreateClient();

        var responseBodies = new List<string>();
        foreach (var path in new[]
                 {
                     "/health", "/api/system/status", "/api/ai-interpretations/specialists", "/does-not-exist"
                 })
        {
            using var response = await client.GetAsync(path);
            responseBodies.Add(await response.Content.ReadAsStringAsync());
        }

        Assert.All(responseBodies, body =>
            Assert.DoesNotContain(sentinelSecret, body, StringComparison.Ordinal));
        Assert.DoesNotContain(sentinelSecret, loggerProvider.CombinedMessages, StringComparison.Ordinal);
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages = new();

        public string CombinedMessages => string.Join(Environment.NewLine, _messages);

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(_messages);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                messages.Enqueue(formatter(state, exception));
        }
    }
}
