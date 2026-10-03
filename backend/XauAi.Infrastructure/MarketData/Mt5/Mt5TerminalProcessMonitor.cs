using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.Mt5;

internal sealed class Mt5TerminalProcessMonitor(
    IOptions<Mt5Options> options,
    ILogger<Mt5TerminalProcessMonitor> logger) : BackgroundService
{
    private readonly Mt5Options _options = options.Value;
    private bool _missingPathLogged;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            logger.LogInformation("MT5 terminal process monitoring is available only on Windows hosts");
            return;
        }

        logger.LogInformation("MT5 terminal process monitoring started");
        while (!stoppingToken.IsCancellationRequested)
        {
            EnsureTerminalRunning();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void EnsureTerminalRunning()
    {
        if (IsConfiguredTerminalRunning())
        {
            _missingPathLogged = false;
            return;
        }

        if (!File.Exists(_options.TerminalPath))
        {
            if (!_missingPathLogged)
            {
                logger.LogError("The configured MT5 terminal executable was not found");
                _missingPathLogged = true;
            }

            return;
        }

        try
        {
            var workingDirectory = Path.GetDirectoryName(_options.TerminalPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = _options.TerminalPath,
                WorkingDirectory = workingDirectory ?? string.Empty,
                UseShellExecute = true
            });
            logger.LogInformation("MT5 terminal was started because its process was not running");
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            logger.LogError(exception, "MT5 terminal could not be started");
        }
    }

    private bool IsConfiguredTerminalRunning()
    {
        var expectedPath = Path.GetFullPath(_options.TerminalPath);
        var processName = Path.GetFileNameWithoutExtension(expectedPath);

        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (processPath is not null
                        && string.Equals(
                            Path.GetFullPath(processPath),
                            expectedPath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    logger.LogDebug(exception, "Unable to inspect an MT5 terminal process");
                }
            }
        }

        return false;
    }
}
