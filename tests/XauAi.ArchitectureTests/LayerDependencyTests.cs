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
    public void Domain_and_application_do_not_reference_the_mt5_sdk()
    {
        var domainReferences = XauAi.Domain.AssemblyReference.Assembly.GetReferencedAssemblies();
        var applicationReferences = XauAi.Application.AssemblyReference.Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(domainReferences, reference =>
            reference.Name?.Contains("MetaTrader", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(applicationReferences, reference =>
            reference.Name?.Contains("MetaTrader", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Infrastructure_contains_the_mt5_adapter_and_bridge()
    {
        var repositoryRoot = FindRepositoryRoot();

        Assert.True(File.Exists(Path.Combine(
            repositoryRoot,
            "backend",
            "XauAi.Infrastructure",
            "MarketData",
            "Mt5",
            "Mt5MarketDataProvider.cs")));
        Assert.True(File.Exists(Path.Combine(
            repositoryRoot,
            "backend",
            "XauAi.Infrastructure",
            "MarketData",
            "Mt5",
            "Bridge",
            "mt5_bridge.py")));
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
