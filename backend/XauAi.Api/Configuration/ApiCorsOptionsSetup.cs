using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using ApiConfigurationOptions = XauAi.Infrastructure.Configuration.Options.ApiOptions;

namespace XauAi.Api.Configuration;

public sealed class ApiCorsOptionsSetup(IOptions<ApiConfigurationOptions> apiOptions)
    : IConfigureOptions<CorsOptions>
{
    public void Configure(CorsOptions options)
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(apiOptions.Value.Cors.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    }
}
