using Microsoft.Extensions.Configuration;
using XauAi.Infrastructure.Configuration;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Configuration.Validation;

namespace XauAi.UnitTests.Configuration;

public sealed class AiSpecialistConfigurationTests
{
    [Fact]
    public void Environment_variables_map_each_specialist_to_an_independent_configuration_path()
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["NEWS_AI_PROVIDER"] = "ProviderA",
            ["NEWS_AI_API_KEY"] = "news-key",
            ["NEWS_AI_MODEL"] = "news-model",
            ["CANDLE_AI_PROVIDER"] = "ProviderB",
            ["CANDLE_AI_API_KEY"] = "candle-key",
            ["CANDLE_AI_MODEL"] = "candle-model",
            ["MASTER_AI_PROVIDER"] = "ProviderC",
            ["MASTER_AI_MODEL"] = "master-model"
        };
        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => environment.GetValueOrDefault(name))
            .Build();

        Assert.Equal("ProviderA", configuration["AiSpecialists:News:Provider"]);
        Assert.Equal("news-key", configuration["AiSpecialists:News:ApiKey"]);
        Assert.Equal("news-model", configuration["AiSpecialists:News:Model"]);
        Assert.Equal("ProviderB", configuration["AiSpecialists:Candle:Provider"]);
        Assert.Equal("candle-key", configuration["AiSpecialists:Candle:ApiKey"]);
        Assert.Equal("candle-model", configuration["AiSpecialists:Candle:Model"]);
        Assert.Equal("ProviderC", configuration["AiSpecialists:Master:Provider"]);
        Assert.Equal("master-model", configuration["AiSpecialists:Master:Model"]);
    }

    [Fact]
    public void Enabling_one_specialist_validates_only_that_specialists_credentials()
    {
        var options = new AiSpecialistsOptions
        {
            News = new AiSpecialistOptions
            {
                Enabled = true,
                Provider = "ProviderA",
                Model = "news-model",
                BaseUrl = "https://example.test/v1/",
                ApiKey = "USER_PROVIDED_LATER"
            },
            Candle = new AiSpecialistOptions
            {
                Enabled = false,
                Provider = "ProviderB",
                Model = string.Empty,
                BaseUrl = string.Empty,
                ApiKey = string.Empty
            }
        };

        var result = new AiSpecialistsOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("AiSpecialists:News:ApiKey", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Failures, failure => failure.Contains("AiSpecialists:Candle", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Current_context_cache_window_is_bounded(int minutes)
    {
        var result = new AiInterpretationOptionsValidator().Validate(null, new AiInterpretationOptions
        {
            CurrentContextCacheMinutes = minutes
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains("AiInterpretation:CurrentContextCacheMinutes", StringComparison.Ordinal));
    }
}
