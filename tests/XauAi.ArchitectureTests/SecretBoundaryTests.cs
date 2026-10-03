namespace XauAi.ArchitectureTests;

public sealed class SecretBoundaryTests
{
    private static readonly string[] ServerOnlyNames =
    [
        "AI_API_KEY",
        "OPENAI_API_KEY",
        "MT5_LOGIN",
        "MT5_PASSWORD",
        "DATABASE_CONNECTION_STRING",
        "MSSQL_SA_PASSWORD",
        "DATABASE_APP_PASSWORD",
        "NEWS_API_KEY",
        "ECONOMIC_DATA_API_KEY",
        "FRED_API_KEY",
        "ANALYST_PROVIDER_API_KEY"
    ];

    [Fact]
    public void Frontend_source_does_not_reference_server_only_configuration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var frontendRoot = Path.Combine(repositoryRoot, "frontend", "xau-ai-web");
        var sourceFiles = Directory.EnumerateFiles(frontendRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.next{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => Path.GetExtension(path) is ".ts" or ".tsx" or ".mjs"
                || Path.GetFileName(path) == "Dockerfile");

        foreach (var sourceFile in sourceFiles)
        {
            var content = File.ReadAllText(sourceFile);
            foreach (var serverOnlyName in ServerOnlyNames)
            {
                Assert.DoesNotContain(serverOnlyName, content, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Example_environment_contains_only_safe_secret_placeholders()
    {
        var examplePath = Path.Combine(FindRepositoryRoot(), ".env.example");
        var values = File.ReadAllLines(examplePath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

        Assert.Equal("YOUR_API_HOST_PORT", values["API_PORT"]);
        Assert.Equal("YOUR_WEB_HOST_PORT", values["WEB_PORT"]);
        Assert.Equal("YOUR_BROWSER_API_URL", values["NEXT_PUBLIC_API_URL"]);
        Assert.Equal("YOUR_WEB_ORIGIN", values["CORS_ALLOWED_ORIGINS"]);
        Assert.Equal("YOUR_SQLSERVER_HOST_PORT", values["SQLSERVER_PORT"]);
        Assert.Equal("YOUR_REDIS_HOST_PORT", values["REDIS_PORT"]);

        foreach (var serverOnlyName in ServerOnlyNames.Where(values.ContainsKey))
        {
            var value = values[serverOnlyName];
            Assert.True(
                value.Contains("USER_PROVIDED_LATER", StringComparison.Ordinal)
                || value.Contains("REPLACE_WITH", StringComparison.Ordinal),
                $"{serverOnlyName} must contain a clear non-secret placeholder.");
        }

        Assert.Contains("OPENAI_API_KEY", values.Keys);
        Assert.Contains("MT5_PASSWORD", values.Keys);
        Assert.Contains("DATABASE_CONNECTION_STRING", values.Keys);
        Assert.Contains("NEWS_API_KEY", values.Keys);
        Assert.Contains("FRED_API_KEY", values.Keys);
        Assert.Contains("ANALYST_PROVIDER_API_KEY", values.Keys);

        Assert.DoesNotContain(values.Keys, key =>
            key.StartsWith("NEXT_PUBLIC_", StringComparison.Ordinal)
            && key != "NEXT_PUBLIC_API_URL");
    }

    [Fact]
    public void Gitignore_protects_local_environment_files_but_keeps_the_example()
    {
        var gitignore = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), ".gitignore"));

        Assert.Contains(".env", gitignore);
        Assert.Contains(".env.*", gitignore);
        Assert.Contains("!.env.example", gitignore);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "XauAi.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
