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
        Assert.Equal("full-master-secret", workspaces.Master.ApiKey);
        Assert.NotEqual(workspaces.Structure.ApiKey, workspaces.Master.ApiKey);
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
