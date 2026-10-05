using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.AllTick;

internal sealed record AllTickKline(
    MarketTimeframe Timeframe,
    DateTimeOffset OpenTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal? Volume);

internal sealed class AllTickHttpClient(
    HttpClient httpClient,
    AllTickOptions options,
    TimeProvider timeProvider,
    ILogger<AllTickHttpClient> logger)
{
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private DateTimeOffset _nextRequestAtUtc = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<AllTickKline>> GetCandlesAsync(
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var duration = timeframe.Duration();
        var requestedBars = checked((int)(((toUtc - fromUtc).Ticks / duration.Ticks) + 1));
        if (requestedBars > options.MaxBarsPerRequest)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                $"The candle request exceeds the {options.MaxBarsPerRequest} bar AllTick limit.");
        }

        var query = JsonSerializer.Serialize(new
        {
            trace = Guid.NewGuid().ToString("N"),
            data = new
            {
                code = options.Symbol,
                kline_type = ToProviderTimeframe(timeframe),
                kline_timestamp_end = toUtc.ToUniversalTime().ToUnixTimeSeconds(),
                query_kline_num = requestedBars,
                adjust_type = 0
            }
        });
        var relative = $"kline?token={Uri.EscapeDataString(options.Token)}&query={Uri.EscapeDataString(query)}";
        using var document = await SendAsync(HttpMethod.Get, relative, content: null, cancellationToken);
        var root = RequireSuccess(document.RootElement);
        var data = RequireProperty(root, "data");
        var candles = ParseKlines(RequireProperty(data, "kline_list"), timeframe);
        return candles
            .Where(candle => candle.OpenTimeUtc >= fromUtc.ToUniversalTime()
                && candle.OpenTimeUtc <= toUtc.ToUniversalTime())
            .OrderBy(candle => candle.OpenTimeUtc)
            .ToArray();
    }

    public async Task<IReadOnlyList<AllTickKline>> GetLatestCandlesAsync(
        IReadOnlyCollection<MarketTimeframe> timeframes,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            trace = Guid.NewGuid().ToString("N"),
            data = new
            {
                data_list = timeframes.Select(timeframe => new
                {
                    code = options.Symbol,
                    kline_type = ToProviderTimeframe(timeframe),
                    kline_timestamp_end = 0,
                    query_kline_num = 2,
                    adjust_type = 0
                })
            }
        });
        var relative = $"batch-kline?token={Uri.EscapeDataString(options.Token)}";
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var document = await SendAsync(HttpMethod.Post, relative, content, cancellationToken);
        var root = RequireSuccess(document.RootElement);
        var data = RequireProperty(root, "data");
        var results = new List<AllTickKline>();
        foreach (var group in RequireProperty(data, "kline_list").EnumerateArray())
        {
            var providerType = ReadInt32(group, "kline_type");
            var timeframe = FromProviderTimeframe(providerType);
            results.AddRange(ParseKlines(RequireProperty(group, "kline_data"), timeframe));
        }

        return results
            .OrderBy(candle => candle.Timeframe)
            .ThenBy(candle => candle.OpenTimeUtc)
            .ToArray();
    }

    private async Task<JsonDocument> SendAsync(
        HttpMethod method,
        string relativeUrl,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (_nextRequestAtUtc > now)
            {
                await Task.Delay(_nextRequestAtUtc - now, timeProvider, cancellationToken);
            }

            _nextRequestAtUtc = timeProvider.GetUtcNow()
                .AddSeconds(options.MinimumHttpRequestIntervalSeconds);
            using var request = new HttpRequestMessage(method, relativeUrl) { Content = content };
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));

            try
            {
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token);
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw Error(MarketDataErrorCodes.ProviderAuthenticationFailed, "AllTick authentication failed.");
                }

                if ((int)response.StatusCode == 429)
                {
                    throw Error(MarketDataErrorCodes.ProviderRateLimited, "The AllTick request limit was reached.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw Error(MarketDataErrorCodes.ProviderUnavailable, "AllTick is temporarily unavailable.");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                return await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("An AllTick request timed out with {ExceptionType}", exception.GetType().Name);
                throw Error(MarketDataErrorCodes.ProviderTimeout, "The AllTick request timed out.");
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning("An AllTick connection failed with {ExceptionType}", exception.GetType().Name);
                throw Error(MarketDataErrorCodes.ProviderConnectionFailed, "Could not connect to AllTick.");
            }
            catch (JsonException exception)
            {
                logger.LogWarning("AllTick returned invalid JSON with {ExceptionType}", exception.GetType().Name);
                throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "AllTick returned an invalid response.");
            }
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private JsonElement RequireSuccess(JsonElement root)
    {
        var returnCode = ReadInt32(root, "ret");
        if (returnCode == 200)
        {
            return root;
        }

        var message = root.TryGetProperty("msg", out var value) ? value.GetString() ?? string.Empty : string.Empty;
        logger.LogWarning("AllTick rejected a request with return code {ReturnCode}", returnCode);
        if (message.Contains("token", StringComparison.OrdinalIgnoreCase)
            || message.Contains("auth", StringComparison.OrdinalIgnoreCase))
        {
            throw Error(MarketDataErrorCodes.ProviderAuthenticationFailed, "AllTick authentication failed.");
        }

        if (message.Contains("frequency", StringComparison.OrdinalIgnoreCase)
            || message.Contains("limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("frequent", StringComparison.OrdinalIgnoreCase))
        {
            throw Error(MarketDataErrorCodes.ProviderRateLimited, "The AllTick request limit was reached.");
        }

        throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "AllTick could not return market data.");
    }

    private static IReadOnlyList<AllTickKline> ParseKlines(JsonElement values, MarketTimeframe timeframe)
    {
        if (values.ValueKind != JsonValueKind.Array)
        {
            throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "AllTick returned an invalid candle response.");
        }

        var candles = new List<AllTickKline>();
        foreach (var value in values.EnumerateArray())
        {
            var timestamp = ReadInt64(value, "timestamp");
            candles.Add(new AllTickKline(
                timeframe,
                FromUnixTimestamp(timestamp),
                ReadDecimal(value, "open_price"),
                ReadDecimal(value, "high_price"),
                ReadDecimal(value, "low_price"),
                ReadDecimal(value, "close_price"),
                TryReadDecimal(value, "volume")));
        }

        return candles;
    }

    internal static int ToProviderTimeframe(MarketTimeframe timeframe) => timeframe switch
    {
        MarketTimeframe.M1 => 1,
        MarketTimeframe.M5 => 2,
        MarketTimeframe.M15 => 3,
        MarketTimeframe.M30 => 4,
        MarketTimeframe.H1 => 5,
        MarketTimeframe.H4 => 7,
        MarketTimeframe.D1 => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported AllTick timeframe.")
    };

    internal static MarketTimeframe FromProviderTimeframe(int value) => value switch
    {
        1 => MarketTimeframe.M1,
        2 => MarketTimeframe.M5,
        3 => MarketTimeframe.M15,
        4 => MarketTimeframe.M30,
        5 => MarketTimeframe.H1,
        7 => MarketTimeframe.H4,
        8 => MarketTimeframe.D1,
        _ => throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "AllTick returned an unsupported timeframe.")
    };

    internal static DateTimeOffset FromUnixTimestamp(long value) =>
        value >= 1_000_000_000_000
            ? DateTimeOffset.FromUnixTimeMilliseconds(value)
            : DateTimeOffset.FromUnixTimeSeconds(value);

    private static JsonElement RequireProperty(JsonElement value, string name)
    {
        if (!value.TryGetProperty(name, out var property))
        {
            throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "AllTick returned an incomplete response.");
        }

        return property;
    }

    private static int ReadInt32(JsonElement value, string name) =>
        checked((int)ReadInt64(value, name));

    private static long ReadInt64(JsonElement value, string name)
    {
        var property = RequireProperty(value, name);
        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String
            && long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "AllTick returned an invalid numeric value.");
    }

    private static decimal ReadDecimal(JsonElement value, string name) =>
        TryReadDecimal(value, name)
        ?? throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "AllTick returned an invalid price.");

    private static decimal? TryReadDecimal(JsonElement value, string name)
    {
        if (!value.TryGetProperty(name, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var number))
        {
            return number;
        }

        return property.ValueKind == JsonValueKind.String
            && decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number)
                ? number
                : null;
    }

    private static MarketDataException Error(string code, string message) => new(code, message);
}
