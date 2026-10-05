using XauAi.Application.AI;

namespace XauAi.Infrastructure.AI;

internal sealed class AiProviderFactory(IEnumerable<IAiProvider> providers) : IAiProviderFactory
{
    private readonly IReadOnlyDictionary<string, IAiProvider> _providers = providers.ToDictionary(
        provider => provider.Adapter,
        StringComparer.OrdinalIgnoreCase);

    public IAiProvider Create(string adapter) =>
        _providers.TryGetValue(adapter, out var provider)
            ? provider
            : throw new AiProviderException(
                AiInterpretationErrorCodes.ProviderNotSupported,
                $"The configured AI provider adapter '{adapter}' is not supported.");
}
