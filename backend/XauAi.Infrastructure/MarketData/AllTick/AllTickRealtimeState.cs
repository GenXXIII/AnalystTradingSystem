using XauAi.Application.MarketData;

namespace XauAi.Infrastructure.MarketData.AllTick;

internal sealed record AllTickRealtimeSnapshot(
    bool Connected,
    MarketQuote? Quote,
    DateTimeOffset? LastMessageAtUtc,
    string Message);

internal sealed class AllTickRealtimeState
{
    private readonly object _lock = new();
    private AllTickRealtimeSnapshot _snapshot = new(false, null, null, "The real-time stream has not connected yet.");

    public AllTickRealtimeSnapshot Get() => Volatile.Read(ref _snapshot);

    public void SetConnection(bool connected, string message, DateTimeOffset observedAtUtc)
    {
        lock (_lock)
        {
            _snapshot = _snapshot with
            {
                Connected = connected,
                LastMessageAtUtc = connected ? observedAtUtc : _snapshot.LastMessageAtUtc,
                Message = message
            };
        }
    }

    public void SetQuote(MarketQuote quote, DateTimeOffset observedAtUtc)
    {
        lock (_lock)
        {
            _snapshot = new AllTickRealtimeSnapshot(true, quote, observedAtUtc, "AllTick real-time data is connected.");
        }
    }
}
