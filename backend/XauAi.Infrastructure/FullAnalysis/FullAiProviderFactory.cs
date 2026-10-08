using XauAi.Application.FullAnalysis;

namespace XauAi.Infrastructure.FullAnalysis;

internal sealed class FullAiProviderFactory(IEnumerable<IFullAiProvider> providers) : IFullAiProviderFactory
{
    private readonly IReadOnlyDictionary<string, IFullAiProvider> providers = providers.ToDictionary(
        provider => provider.Adapter,
        StringComparer.OrdinalIgnoreCase);

    public IFullAiProvider Create(string adapter) =>
        providers.TryGetValue(adapter, out var provider)
            ? provider
            : throw new FullAnalysisException(
                FullAnalysisErrorCodes.ProviderNotSupported,
                $"The configured Full AI provider adapter '{adapter}' is not supported.");
}
