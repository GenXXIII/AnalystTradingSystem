namespace XauAi.Infrastructure.Configuration.Options;

public sealed class AllTickOptions
{
    public const string SectionName = "AllTick";

    public bool Enabled { get; set; }

    public string Token { get; set; } = string.Empty;

    public string ApplicationSymbol { get; set; } = "XAUUSD";

    public string Symbol { get; set; } = "GOLD";

    public string HttpBaseUrl { get; set; } = "https://quote.alltick.co/quote-b-api/";

    public string WebSocketUrl { get; set; } = "wss://quote.alltick.co/quote-b-ws-api";

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int ReconnectDelaySeconds { get; set; } = 10;

    public int HeartbeatIntervalSeconds { get; set; } = 10;

    public int QuoteMaxAgeSeconds { get; set; } = 30;

    public int RealtimePersistIntervalSeconds { get; set; } = 10;

    public int MaxBarsPerRequest { get; set; } = 500;

    public int MinimumHttpRequestIntervalSeconds { get; set; } = 10;
}
