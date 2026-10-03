using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.Mt5;

internal sealed class Mt5PythonBridgeClient(
    IOptions<Mt5Options> options,
    ILogger<Mt5PythonBridgeClient> logger) : IMt5BridgeClient
{
    private readonly Mt5Options _options = options.Value;

    public async Task<Mt5BridgeResponse> ExecuteAsync(
        Mt5BridgeRequest request,
        CancellationToken cancellationToken = default)
    {
        var scriptPath = ResolveScriptPath();
        if (!File.Exists(scriptPath))
        {
            throw new Mt5BridgeException(
                MarketDataErrorCodes.ConfigurationInvalid,
                "The MT5 bridge script was not found.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.PythonExecutable,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (string.Equals(Path.GetFileNameWithoutExtension(_options.PythonExecutable), "py", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("-3");
        }

        startInfo.ArgumentList.Add(scriptPath);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new Mt5BridgeException(
                    MarketDataErrorCodes.ConfigurationInvalid,
                    "The MT5 Python bridge could not be started.");
            }
        }
        catch (Mt5BridgeException)
        {
            throw;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new Mt5BridgeException(
                MarketDataErrorCodes.ConfigurationInvalid,
                "The configured Python executable could not be started.",
                exception);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var requestJson = JsonSerializer.Serialize(request, Mt5BridgeJsonContext.Default.Mt5BridgeRequest);
        await process.StandardInput.WriteAsync(requestJson.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
        process.StandardInput.Close();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);

        try
        {
            await process.WaitForExitAsync(linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new Mt5BridgeException(
                MarketDataErrorCodes.Timeout,
                "The MT5 request timed out.");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        if (process.ExitCode != 0)
        {
            logger.LogWarning(
                "MT5 bridge process exited with code {ExitCode}; stderr length {ErrorLength}",
                process.ExitCode,
                standardError.Length);
        }

        Mt5BridgeResponse? response;
        try
        {
            response = JsonSerializer.Deserialize(
                standardOutput,
                Mt5BridgeJsonContext.Default.Mt5BridgeResponse);
        }
        catch (JsonException exception)
        {
            throw new Mt5BridgeException(
                MarketDataErrorCodes.InitializationFailed,
                "The MT5 bridge returned an invalid response.",
                exception);
        }

        if (response is null)
        {
            throw new Mt5BridgeException(
                MarketDataErrorCodes.InitializationFailed,
                "The MT5 bridge returned no response.");
        }

        if (!response.Success)
        {
            throw new Mt5BridgeException(
                response.ErrorCode ?? MarketDataErrorCodes.Unavailable,
                response.Message ?? "The MT5 integration is unavailable.");
        }

        return response;
    }

    private string ResolveScriptPath() => string.IsNullOrWhiteSpace(_options.BridgeScriptPath)
        ? Path.Combine(AppContext.BaseDirectory, "MarketData", "Mt5", "Bridge", "mt5_bridge.py")
        : Path.GetFullPath(_options.BridgeScriptPath);

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
