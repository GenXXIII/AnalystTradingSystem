using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.AllTick;

internal sealed class AllTickRealtimeWorker(
    AllTickOptions options,
    MarketDataPipelineSettings settings,
    AllTickRealtimeState state,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AllTickRealtimeWorker> logger) : BackgroundService
{
    private readonly AllTickCandleAccumulator _accumulator = new(
        options.ApplicationSymbol,
        options.Symbol,
        "alltick",
        settings.Timeframes);
    private DateTimeOffset _nextPersistAtUtc = DateTimeOffset.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled
            || !string.Equals(settings.Provider, "AllTick", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("AllTick real-time stream is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is WebSocketException or HttpRequestException or JsonException)
            {
                state.SetConnection(false, "The AllTick real-time stream is reconnecting.", timeProvider.GetUtcNow());
                logger.LogWarning(
                    "AllTick real-time stream disconnected with {ExceptionType}; reconnecting after the configured delay",
                    exception.GetType().Name);
            }

            await Task.Delay(TimeSpan.FromSeconds(options.ReconnectDelaySeconds), timeProvider, stoppingToken);
        }
    }

    private async Task RunConnectionAsync(CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(options.HeartbeatIntervalSeconds);
        await socket.ConnectAsync(BuildWebSocketUri(), cancellationToken);

        await SendAsync(socket, AllTickWebSocketProtocol.Subscription(22004, options.Symbol), cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), timeProvider, cancellationToken);
        await SendAsync(socket, AllTickWebSocketProtocol.Subscription(22002, options.Symbol), cancellationToken);
        state.SetConnection(true, "AllTick real-time data is connected.", timeProvider.GetUtcNow());
        logger.LogInformation("AllTick real-time stream connected for {ProviderSymbol}", options.Symbol);

        using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = SendHeartbeatsAsync(socket, heartbeatCancellation.Token);
        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var message = await ReceiveAsync(socket, cancellationToken);
                if (message is null)
                {
                    break;
                }

                await ProcessMessageAsync(message, cancellationToken);
            }
        }
        finally
        {
            heartbeatCancellation.Cancel();
            try
            {
                await heartbeat;
            }
            catch (OperationCanceledException) when (heartbeatCancellation.IsCancellationRequested)
            {
            }

            state.SetConnection(false, "The AllTick real-time stream is disconnected.", timeProvider.GetUtcNow());
        }
    }

    private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (AllTickWebSocketProtocol.IsRejected(message, out var commandId))
        {
            throw new WebSocketException($"AllTick rejected WebSocket command {commandId}.");
        }

        if (AllTickWebSocketProtocol.TryReadOrderBook(message, out var orderBook) && orderBook is not null)
        {
            state.SetQuote(
                new MarketQuote(
                    options.ApplicationSymbol,
                    options.Symbol,
                    orderBook.Bid,
                    orderBook.Ask,
                    orderBook.TimestampUtc),
                timeProvider.GetUtcNow());
        }

        if (!AllTickWebSocketProtocol.TryReadTrade(message, out var trade) || trade is null)
        {
            return;
        }

        var observedAtUtc = timeProvider.GetUtcNow();
        _accumulator.ApplyTick(trade.TimestampUtc, trade.Price, trade.Volume, observedAtUtc);
        if (settings.SyncEnabled && observedAtUtc >= _nextPersistAtUtc)
        {
            await PersistCurrentCandlesAsync(observedAtUtc, cancellationToken);
            _nextPersistAtUtc = observedAtUtc.AddSeconds(options.RealtimePersistIntervalSeconds);
        }
    }

    private async Task PersistCurrentCandlesAsync(
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken)
    {
        var snapshots = _accumulator.Snapshot(observedAtUtc);
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IMarketCandleStore>();
        foreach (var group in snapshots.GroupBy(candle => candle.Timeframe))
        {
            try
            {
                await store.SaveAsync(group.ToArray(), cancellationToken);
            }
            catch (MarketDataException exception) when (exception.Code == MarketDataErrorCodes.DatabaseDisabled)
            {
                logger.LogWarning("AllTick candle persistence is enabled but database persistence is disabled");
                return;
            }
        }
    }

    private async Task SendHeartbeatsAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            await Task.Delay(TimeSpan.FromSeconds(options.HeartbeatIntervalSeconds), timeProvider, cancellationToken);
            await SendAsync(socket, AllTickWebSocketProtocol.Heartbeat(), cancellationToken);
        }
    }

    private static async Task SendAsync(
        ClientWebSocket socket,
        string message,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string?> ReceiveAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new WebSocketException("AllTick returned a non-text WebSocket message.");
            }

            await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private Uri BuildWebSocketUri()
    {
        var separator = options.WebSocketUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return new Uri($"{options.WebSocketUrl}{separator}token={Uri.EscapeDataString(options.Token)}");
    }
}
