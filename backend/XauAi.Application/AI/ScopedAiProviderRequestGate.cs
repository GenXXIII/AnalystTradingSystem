using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace XauAi.Application.AI;

internal sealed class ScopedAiProviderRequestGate(TimeProvider timeProvider) : IScopedAiProviderRequestGate
{
    private readonly ConcurrentDictionary<string, GateState> states = new(StringComparer.Ordinal);

    public async Task WaitAsync(
        string analysisScope,
        string provider,
        string baseUrl,
        string apiKey,
        int requestsPerMinute,
        CancellationToken cancellationToken = default)
    {
        if (requestsPerMinute <= 0)
        {
            return;
        }

        var state = states.GetOrAdd(ScopeKey(analysisScope, provider, baseUrl, apiKey), static _ => new GateState());
        await state.Lock.WaitAsync(cancellationToken);
        try
        {
            state.RequestsPerMinute = state.RequestsPerMinute == 0
                ? requestsPerMinute
                : Math.Min(state.RequestsPerMinute, requestsPerMinute);
            var interval = TimeSpan.FromMinutes(1d / state.RequestsPerMinute);
            var wait = state.LastRequestUtc + interval - timeProvider.GetUtcNow();
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, timeProvider, cancellationToken);
            }

            state.LastRequestUtc = timeProvider.GetUtcNow();
        }
        finally
        {
            state.Lock.Release();
        }
    }

    internal static string ScopeKey(string analysisScope, string provider, string baseUrl, string apiKey)
    {
        var normalized = $"{analysisScope.Trim().ToUpperInvariant()}|{provider.Trim().ToUpperInvariant()}|{baseUrl.Trim().TrimEnd('/').ToUpperInvariant()}|{apiKey.Trim()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }

    private sealed class GateState
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public DateTimeOffset LastRequestUtc { get; set; } = DateTimeOffset.MinValue;
        public int RequestsPerMinute { get; set; }
    }
}
