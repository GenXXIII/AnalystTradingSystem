namespace XauAi.Infrastructure.Configuration.Options;

public sealed class Mt5Options
{
    public const string SectionName = "MT5";

    public bool Enabled { get; set; }

    public string Login { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Server { get; set; } = string.Empty;

    public string TerminalPath { get; set; } = string.Empty;

    public string ApplicationSymbol { get; set; } = "XAUUSD";

    public string Symbol { get; set; } = "XAUUSD";

    public string TimeZone { get; set; } = "UTC";

    public int ConnectionTimeoutSeconds { get; set; } = 30;

    public int RequestTimeoutSeconds { get; set; } = 60;

    public int ReconnectDelaySeconds { get; set; } = 5;

    public int MaxBarsPerRequest { get; set; } = 10000;

    public string PythonExecutable { get; set; } = "py";

    public string BridgeScriptPath { get; set; } = string.Empty;
}
