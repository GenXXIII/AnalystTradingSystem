using Microsoft.Extensions.Configuration;
using XauAi.Infrastructure.Configuration;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.Configuration.Validation;

namespace XauAi.UnitTests.Configuration;

public sealed class TargetAnalystConfigurationTests
{
    [Fact]
    public void Environment_variables_keep_target_workspaces_independent_from_phase_eleven_specialists()
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["TARGET_STRUCTURE_AI_PROVIDER"] = "TargetProviderA",
            ["TARGET_STRUCTURE_AI_API_KEY"] = "target-structure-key",
            ["TARGET_STRUCTURE_AI_MODEL"] = "target-structure-model",
            ["TARGET_STRUCTURE_AI_FALLBACK_MODELS"] = "fallback-a,fallback-b",
            ["TARGET_STRUCTURE_AI_DISABLE_REASONING"] = "true",
            ["TARGET_MASTER_AI_PROVIDER"] = "TargetProviderB",
            ["TARGET_MASTER_AI_MODEL"] = "target-master-model",
            ["STRUCTURE_AI_PROVIDER"] = "InterpretationProvider"
        };
        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => environment.GetValueOrDefault(name))
            .Build();

        Assert.Equal("TargetProviderA", configuration["TargetAiWorkspaces:Structure:Provider"]);
        Assert.Equal("target-structure-key", configuration["TargetAiWorkspaces:Structure:ApiKey"]);
        Assert.Equal("target-structure-model", configuration["TargetAiWorkspaces:Structure:Model"]);
        Assert.Equal("fallback-a,fallback-b", configuration["TargetAiWorkspaces:Structure:FallbackModels"]);
        Assert.Equal("true", configuration["TargetAiWorkspaces:Structure:DisableReasoning"]);
        Assert.Equal("TargetProviderB", configuration["TargetAiWorkspaces:Master:Provider"]);
        Assert.Equal("InterpretationProvider", configuration["AiSpecialists:Structure:Provider"]);
    }

    [Fact]
    public void Disabled_workspaces_do_not_require_credentials()
    {
        var result = new TargetAiWorkspacesOptionsValidator().Validate(null, new TargetAiWorkspacesOptions());

        Assert.False(result.Failed);
    }

    [Fact]
    public void Shared_target_key_is_a_fallback_for_all_workspaces()
    {
        var environment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["TARGET_STRUCTURE_AI_API_KEY"] = string.Empty,
            ["TARGET_AI_API_KEY"] = "shared-target-key"
        };
        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => environment.GetValueOrDefault(name))
            .Build();

        Assert.Equal("shared-target-key", configuration["TargetAiWorkspaces:Structure:ApiKey"]);
        Assert.Equal("shared-target-key", configuration["TargetAiWorkspaces:Master:ApiKey"]);
    }

    [Fact]
    public void Groq_key_is_a_shared_fallback_for_target_and_future_profiles()
    {
        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => name == "GROQ_API_KEY" ? "shared-groq-key" : null)
            .Build();

        Assert.Equal("shared-groq-key", configuration["TargetAiWorkspaces:Structure:ApiKey"]);
        Assert.Equal("shared-groq-key", configuration["FullAiWorkspaces:Master:ApiKey"]);
    }

    [Fact]
    public void Enabled_workspace_requires_its_own_provider_model_url_and_key()
    {
        var options = new TargetAiWorkspacesOptions
        {
            Structure = new TargetAiWorkspaceOptions
            {
                Enabled = true,
                Provider = "None",
                Model = string.Empty,
                BaseUrl = string.Empty,
                ApiKey = string.Empty,
                PromptVersion = "phase13-structure-v1",
                ConfigurationVersion = "phase13-v1"
            }
        };

        var result = new TargetAiWorkspacesOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("TargetAiWorkspaces:Structure:Provider", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("TargetAiWorkspaces:Structure:Model", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("TargetAiWorkspaces:Structure:BaseUrl", StringComparison.Ordinal));
        Assert.Contains(result.Failures, failure => failure.Contains("TargetAiWorkspaces:Structure:ApiKey", StringComparison.Ordinal));
    }
}
