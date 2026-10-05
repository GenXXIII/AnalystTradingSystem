using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.MarketData.TwelveData;

internal sealed record TwelveDataCandle(
    DateTimeOffset OpenTimeUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal? Volume);

internal sealed class TwelveDataHttpClient(
    HttpClient httpClient,
    TwelveDataOptions options,
    TimeProvider timeProvider,
    ILogger<TwelveDataHttpClient> logger)
{
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private DateTimeOffset _nextRequestAtUtc = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<TwelveDataCandle>> GetCandlesAsync(
        MarketTimeframe timeframe,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var relativeUrl = "time_series"
            + $"?symbol={Uri.EscapeDataString(options.Symbol)}"
            + $"&interval={Uri.EscapeDataString(ToProviderInterval(timeframe))}"
            + $"&start_date={Uri.EscapeDataString(FormatUtc(fromUtc))}"
            + $"&end_date={Uri.EscapeDataString(FormatUtc(toUtc))}"
            + "&timezone=UTC&order=ASC"
            + $"&apikey={Uri.EscapeDataString(options.ApiKey)}";
        using var document = await SendAsync(relativeUrl, cancellationToken);
        var root = document.RootElement;
        EnsureSuccess(root);
        if (!root.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array)
        {
            throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "Twelve Data returned an incomplete candle response.");
        }

        return [.. values.EnumerateArray()
            .Select(value => new TwelveDataCandle(
                ParseDateTime(ReadString(value, "datetime")),
                ReadDecimal(value, "open"),
                ReadDecimal(value, "high"),
                ReadDecimal(value, "low"),
                ReadDecimal(value, "close"),
                TryReadDecimal(value, "volume")))
            .OrderBy(value => value.OpenTimeUtc)];
    }

    internal static string ToProviderInterval(MarketTimeframe timeframe) => timeframe switch
    {
        MarketTimeframe.M1 => "1min",
        MarketTimeframe.M5 => "5min",
        MarketTimeframe.M15 => "15min",
        MarketTimeframe.M30 => "30min",
        MarketTimeframe.H1 => "1h",
        MarketTimeframe.H4 => "4h",
        MarketTimeframe.D1 => "1day",
        _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported Twelve Data timeframe.")
    };

    private async Task<JsonDocument> SendAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (_nextRequestAtUtc > now)
            {
                await Task.Delay(_nextRequestAtUtc - now, timeProvider, cancellationToken);
            }

            _nextRequestAtUtc = timeProvider.GetUtcNow().AddSeconds(options.MinimumRequestIntervalSeconds);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
            try
            {
                using var response = await httpClient.GetAsync(relativeUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    throw Error(MarketDataErrorCodes.ProviderAuthenticationFailed, "Twelve Data authentication failed.");
                }

                if ((int)response.StatusCode == 429)
                {
                    throw Error(MarketDataErrorCodes.ProviderRateLimited, "The Twelve Data request limit was reached.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw Error(MarketDataErrorCodes.ProviderUnavailable, "Twelve Data is temporarily unavailable.");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                return await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("A Twelve Data request timed out with {ExceptionType}", exception.GetType().Name);
                throw Error(MarketDataErrorCodes.ProviderTimeout, "The Twelve Data request timed out.");
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning("A Twelve Data connection failed with {ExceptionType}", exception.GetType().Name);
                throw Error(MarketDataErrorCodes.ProviderConnectionFailed, "Could not connect to Twelve Data.");
            }
            catch (JsonException exception)
            {
                logger.LogWarning("Twelve Data returned invalid JSON with {ExceptionType}", exception.GetType().Name);
                throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "Twelve Data returned an invalid response.");
            }
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private void EnsureSuccess(JsonElement root)
    {
        if (!root.TryGetProperty("status", out var status)
            || !string.Equals(status.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var code = root.TryGetProperty("code", out var codeValue) && codeValue.TryGetInt32(out var parsedCode)
            ? parsedCode
            : 0;
        logger.LogWarning("Twelve Data rejected a request with code {ResponseCode}", code);
        if (code is 401 or 403)
        {
            throw Error(MarketDataErrorCodes.ProviderAuthenticationFailed, "Twelve Data authentication failed.");
        }

        if (code == 429)
        {
            throw Error(MarketDataErrorCodes.ProviderRateLimited, "The Twelve Data request limit was reached.");
        }

        throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "Twelve Data could not return market data.");
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseDateTime(string value)
    {
        var formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd" };
        if (DateTimeOffset.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed;
        }

        throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "Twelve Data returned an invalid candle timestamp.");
    }

    private static string ReadString(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "Twelve Data returned an incomplete candle response.");

    private static decimal ReadDecimal(JsonElement value, string name) =>
        TryReadDecimal(value, name)
        ?? throw Error(MarketDataErrorCodes.ProviderDataRequestFailed, "Twelve Data returned an invalid price.");

    private static decimal? TryReadDecimal(JsonElement value, string name)
    {
        if (!value.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null)
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
