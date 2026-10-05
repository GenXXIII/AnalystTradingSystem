namespace XauAi.Application.MarketData;

public enum MarketDataProviderState
{
    Disabled,
    Available,
    Connected,
    Disconnected,
    ConfigurationError,
    AuthenticationFailed,
    ConnectionFailed
}

public sealed record MarketDataProviderStatus(
    string Provider,
    MarketDataProviderState State,
    bool Enabled,
    bool Connected,
    string ApplicationSymbol,
    string ProviderSymbol,
    string? Server,
    string Message,
    DateTimeOffset CheckedAtUtc);

public sealed record MarketQuote(
    string Symbol,
    string ProviderSymbol,
    decimal Bid,
    decimal Ask,
    DateTimeOffset TimestampUtc);

public sealed record MarketCandleSnapshot(
    string Symbol,
    string ProviderSymbol,
    MarketTimeframe Timeframe,
    DateTimeOffset OpenTimeUtc,
    DateTimeOffset CloseTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal? TickVolume,
    decimal? RealVolume,
    decimal? Spread,
    bool IsComplete,
    string SourceTimeZone,
    DateTimeOffset FetchedAtUtc,
    string ProviderKey = "");

public sealed record StoredCandleCursor(DateTimeOffset OpenTimeUtc, bool IsComplete);

public sealed record CandleSaveResult(int Inserted, int Updated, int Skipped);

public sealed record MarketDataSyncResult(
    string Symbol,
    MarketTimeframe Timeframe,
    DateTimeOffset RequestedFromUtc,
    DateTimeOffset RequestedToUtc,
    DateTimeOffset? EffectiveFromUtc,
    int Retrieved,
    int Inserted,
    int Updated,
    int Skipped);
