namespace XauAi.Infrastructure.MarketData.Mt5;

internal interface IMt5HostEnvironment
{
    bool IsWindows { get; }

    bool FileExists(string path);
}

internal sealed class Mt5HostEnvironment : IMt5HostEnvironment
{
    public bool IsWindows => OperatingSystem.IsWindows();

    public bool FileExists(string path) => File.Exists(path);
}
