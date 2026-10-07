using System.Reflection;

namespace XauAi.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_has_no_dependencies_on_other_solution_layers()
    {
        var references = XauAi.Domain.AssemblyReference.Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, IsSolutionAssembly);
    }

    [Fact]
    public void Application_does_not_depend_on_api_or_infrastructure()
    {
        var references = XauAi.Application.AssemblyReference.Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference =>
            reference.Name is "XauAi.Api" or "XauAi.Infrastructure");
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var references = XauAi.Infrastructure.AssemblyReference.Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference => reference.Name == "XauAi.Api");
    }

    [Fact]
    public void Solution_does_not_reference_the_metatrader_sdk()
    {
        var domainReferences = XauAi.Domain.AssemblyReference.Assembly.GetReferencedAssemblies();
        var applicationReferences = XauAi.Application.AssemblyReference.Assembly.GetReferencedAssemblies();
        var infrastructureReferences = XauAi.Infrastructure.AssemblyReference.Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(domainReferences, reference =>
            reference.Name?.Contains("MetaTrader", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(applicationReferences, reference =>
            reference.Name?.Contains("MetaTrader", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(infrastructureReferences, reference =>
            reference.Name?.Contains("MetaTrader", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Fred_adapter_is_isolated_in_infrastructure_behind_application_contracts()
    {
        var applicationTypes = XauAi.Application.AssemblyReference.Assembly.GetTypes();
        Assert.Contains(applicationTypes, type => type.Name == "IEconomicDataProvider");
        Assert.DoesNotContain(applicationTypes, type =>
            type.Name.Contains("Fred", StringComparison.OrdinalIgnoreCase));

        var repositoryRoot = FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(
            repositoryRoot,
            "backend",
            "XauAi.Infrastructure",
            "EconomicData",
            "Fred",
            "FredEconomicDataProvider.cs")));
    }

    [Fact]
    public void Analyst_provider_adapter_is_isolated_in_infrastructure_behind_application_contracts()
    {
        var applicationTypes = XauAi.Application.AssemblyReference.Assembly.GetTypes();
        Assert.Contains(applicationTypes, type => type.Name == "IAnalystDataProvider");
        Assert.DoesNotContain(applicationTypes, type =>
            type.Name.Contains("RssAtom", StringComparison.OrdinalIgnoreCase));

        var repositoryRoot = FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(
            repositoryRoot,
            "backend",
            "XauAi.Infrastructure",
            "Analysts",
            "Rss",
            "RssAtomAnalystDataProvider.cs")));
    }

    [Fact]
    public void Evidence_contracts_are_application_owned_and_sql_persistence_is_in_infrastructure()
    {
        var applicationTypes = XauAi.Application.AssemblyReference.Assembly.GetTypes();
        Assert.Contains(applicationTypes, type => type.Name == "IEvidenceStore");
        Assert.Contains(applicationTypes, type => type.Name == "IEvidenceQueryService");
        Assert.Contains(applicationTypes, type => type.Name == "EvidencePack");

        var repositoryRoot = FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(
            repositoryRoot,
            "backend",
            "XauAi.Infrastructure",
            "Evidence",
            "Persistence",
            "EfEvidenceStore.cs")));
    }

    [Fact]
    public void Local_analyst_has_no_ai_or_later_phase_dependency()
    {
        var repositoryRoot = FindRepositoryRoot();
        var localAnalysisRoot = Path.Combine(
            repositoryRoot,
            "backend",
            "XauAi.Application",
            "LocalAnalysis");
        var source = Directory.EnumerateFiles(localAnalysisRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();

        Assert.NotEmpty(source);
        Assert.DoesNotContain(source, value => value.Contains("XauAi.Application.AI", StringComparison.Ordinal));
        Assert.DoesNotContain(source, value => value.Contains("TargetAnalyst", StringComparison.Ordinal));
        Assert.DoesNotContain(source, value => value.Contains("Telegram", StringComparison.Ordinal));
    }

    private static bool IsSolutionAssembly(AssemblyName reference) =>
        reference.Name?.StartsWith("XauAi.", StringComparison.Ordinal) == true;

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
