using Microsoft.Extensions.Configuration;
using XauAi.Infrastructure.Configuration;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.UnitTests.Configuration;

public sealed class FullAnalystConfigurationTests
{
    [Fact]
    public void Environment_variables_bind_each_full_workspace_independently()
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["FULL_ANALYST_ENABLED"] = "true",
            ["FULL_STRUCTURE_AI_ENABLED"] = "true",
            ["FULL_STRUCTURE_AI_PROVIDER"] = "OpenRouter",
            ["FULL_STRUCTURE_AI_API_KEY"] = "full-structure-secret",
            ["FULL_STRUCTURE_AI_MODEL"] = "structure-model",
            ["FULL_STRUCTURE_AI_FALLBACK_MODELS"] = "fallback-a,fallback-b",
            ["FULL_STRUCTURE_AI_DISABLE_REASONING"] = "true",
            ["FULL_STRUCTURE_AI_BASE_URL"] = "https://openrouter.ai/api/v1",
            ["FULL_MASTER_AI_PROVIDER"] = "DifferentProvider",
            ["FULL_MASTER_AI_API_KEY"] = "full-master-secret",
            ["TARGET_AI_API_KEY"] = "must-not-leak"
        };
        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => values.GetValueOrDefault(name))
            .Build();

        var analyst = configuration.GetSection(FullAnalystOptions.SectionName).Get<FullAnalystOptions>();
        var workspaces = configuration.GetSection(FullAiWorkspacesOptions.SectionName).Get<FullAiWorkspacesOptions>();

        Assert.NotNull(analyst);
        Assert.True(analyst.Enabled);
        Assert.NotNull(workspaces);
        Assert.Equal("full-structure-secret", workspaces.Structure.ApiKey);
        Assert.Equal("fallback-a,fallback-b", workspaces.Structure.FallbackModels);
        Assert.True(workspaces.Structure.DisableReasoning);
        Assert.Equal("full-master-secret", workspaces.Master.ApiKey);
        Assert.NotEqual(workspaces.Structure.ApiKey, workspaces.Master.ApiKey);
    }

    [Fact]
    public void Full_shared_api_key_is_used_unless_a_workspace_overrides_it()
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["FULL_AI_API_KEY"] = "shared-full-secret",
            ["FULL_STRUCTURE_AI_ENABLED"] = "true",
            ["FULL_MASTER_AI_ENABLED"] = "true",
            ["FULL_MASTER_AI_API_KEY"] = "master-only-secret"
        };
        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => values.GetValueOrDefault(name))
            .Build();

        var workspaces = configuration.GetSection(FullAiWorkspacesOptions.SectionName).Get<FullAiWorkspacesOptions>();

        Assert.NotNull(workspaces);
        Assert.Equal("shared-full-secret", workspaces.Structure.ApiKey);
        Assert.Equal("master-only-secret", workspaces.Master.ApiKey);
    }

    [Fact]
    public void Target_credentials_are_not_reused_by_full_workspaces()
    {
        var configuration = new ConfigurationBuilder()
            .AddXauAiEnvironmentVariables(name => name == "TARGET_AI_API_KEY" ? "target-only-secret" : null)
            .Build();

        var workspaces = configuration.GetSection(FullAiWorkspacesOptions.SectionName).Get<FullAiWorkspacesOptions>();

        Assert.Null(workspaces);
        Assert.Null(configuration["FullAiWorkspaces:Structure:ApiKey"]);
    }
}
