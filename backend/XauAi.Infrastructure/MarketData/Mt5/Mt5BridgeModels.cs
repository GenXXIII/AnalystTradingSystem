using System.Text.Json.Serialization;

namespace XauAi.Infrastructure.MarketData.Mt5;

internal sealed record Mt5BridgeRequest(
    string Operation,
    string TerminalPath,
    long Login,
    string Password,
    string Server,
    int ConnectionTimeoutMilliseconds,
    string? Symbol = null,
    string? Timeframe = null,
    long? FromUnixSeconds = null,
    long? ToUnixSeconds = null,
    int? MaxBars = null);

internal sealed class Mt5BridgeResponse
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public string? Message { get; init; }

    public Mt5BridgeStatus? Status { get; init; }

    public Mt5BridgeQuote? Quote { get; init; }

    public IReadOnlyList<Mt5BridgeCandle> Candles { get; init; } = [];
}

internal sealed record Mt5BridgeStatus(
    bool Connected,
    long? Login,
    string? Server,
    string? TerminalName,
    int? TerminalBuild);

internal sealed record Mt5BridgeQuote(
    string Symbol,
    decimal Bid,
    decimal Ask,
    long TimestampMilliseconds);

internal sealed record Mt5BridgeCandle(
    string Symbol,
    string Timeframe,
    long OpenTimeSeconds,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal? TickVolume,
    decimal? RealVolume,
    decimal? Spread,
    bool IsComplete);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(Mt5BridgeRequest))]
[JsonSerializable(typeof(Mt5BridgeResponse))]
internal sealed partial class Mt5BridgeJsonContext : JsonSerializerContext;
