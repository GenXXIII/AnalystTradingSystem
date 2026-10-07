using XauAi.Application.TargetAnalysis;

namespace XauAi.Infrastructure.TargetAnalysis;

internal sealed class TargetAiProviderFactory(IEnumerable<ITargetAiProvider> providers) : ITargetAiProviderFactory
{
    private readonly IReadOnlyDictionary<string, ITargetAiProvider> providers = providers.ToDictionary(
        provider => provider.Adapter,
        StringComparer.OrdinalIgnoreCase);

    public ITargetAiProvider Create(string adapter) =>
        providers.TryGetValue(adapter, out var provider)
            ? provider
            : throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.ProviderNotSupported,
                $"The configured target AI provider adapter '{adapter}' is not supported.");
}
