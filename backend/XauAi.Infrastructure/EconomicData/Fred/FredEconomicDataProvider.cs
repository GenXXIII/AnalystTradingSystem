using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using XauAi.Application.EconomicData;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.EconomicData.Fred;

internal sealed class FredEconomicDataProvider(
    HttpClient httpClient,
    EconomicDataOptions options,
    TimeProvider timeProvider) : IEconomicDataProvider, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly DateOnly EarliestFredDate = new(1776, 7, 4);
    private readonly SemaphoreSlim _rateGate = new(1, 1);
    private DateTimeOffset _nextAllowedAtUtc = DateTimeOffset.MinValue;

    public async Task<ProviderEconomicSeries> GetSeriesMetadataAsync(
        string externalSeriesId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        ValidateSeriesId(externalSeriesId);
        var response = await GetAsync<FredSeriesResponse>(
            "series",
            [("series_id", externalSeriesId)],
            cancellationToken);
        if (response.Series is null || response.Series.Count != 1)
        {
            throw InvalidResponse("The economic provider did not return exactly one requested series metadata record.");
        }

        var series = response.Series[0];

        if (!string.Equals(series.Id, externalSeriesId, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(series.Title)
            || string.IsNullOrWhiteSpace(series.Units)
            || string.IsNullOrWhiteSpace(series.Frequency)
            || string.IsNullOrWhiteSpace(series.SeasonalAdjustment))
        {
            throw InvalidResponse("The economic provider returned incomplete series metadata.");
        }

        return new ProviderEconomicSeries(
            series.Id!.Trim().ToUpperInvariant(),
            series.Title.Trim(),
            Limit(EmptyToNull(series.Notes), 4000),
            series.Units.Trim(),
            series.Frequency.Trim(),
            series.SeasonalAdjustment.Trim(),
            ParseOptionalDate(series.ObservationStart, "observation_start"),
            ParseOptionalDate(series.ObservationEnd, "observation_end"),
            ParseOptionalTimestamp(series.LastUpdated));
    }

    public async Task<EconomicObservationProviderPage> GetObservationsAsync(
        EconomicObservationProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        ValidateRequest(request);
        var response = await GetAsync<FredObservationsResponse>(
            "series/observations",
            [
                ("series_id", request.ExternalSeriesId),
                ("observation_start", request.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("observation_end", request.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("sort_order", request.Descending ? "desc" : "asc"),
                ("offset", request.Offset.ToString(CultureInfo.InvariantCulture)),
                ("limit", request.Limit.ToString(CultureInfo.InvariantCulture))
            ],
            cancellationToken);

        if (response.Count < 0 || response.Offset < 0 || response.Limit < 1 || response.Observations is null)
        {
            throw InvalidResponse("The economic provider returned invalid observation paging metadata.");
        }

        var observations = new List<ProviderEconomicObservation>(response.Observations.Count);
        foreach (var observation in response.Observations)
        {
            if (!DateOnly.TryParseExact(
                    observation.Date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var observationDate))
            {
                throw InvalidResponse("The economic provider returned an invalid observation date.");
            }

            var originalValue = observation.Value?.Trim() ?? string.Empty;
            decimal? value;
            string status;
            if (originalValue is "" or ".")
            {
                value = null;
                status = "Missing";
                originalValue = originalValue.Length == 0 ? "." : originalValue;
            }
            else if (decimal.TryParse(
                originalValue,
                NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture,
                out var parsedValue))
            {
                value = parsedValue;
                status = "Available";
            }
            else
            {
                throw InvalidResponse("The economic provider returned a non-numeric observation value.");
            }

            observations.Add(new ProviderEconomicObservation(
                observationDate,
                value,
                originalValue,
                status,
                ParseOptionalDate(observation.RealtimeStart, "realtime_start"),
                ParseOptionalDate(observation.RealtimeEnd, "realtime_end")));
        }

        return new EconomicObservationProviderPage(
            observations,
            response.Count,
            response.Offset,
            response.Limit);
    }

    public async Task<ProviderEconomicObservation?> GetLatestObservationAsync(
        string externalSeriesId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var page = await GetObservationsAsync(
            new EconomicObservationProviderRequest(
                externalSeriesId,
                EarliestFredDate,
                today,
                0,
                1,
                Descending: true),
            cancellationToken);
        return page.Observations.SingleOrDefault();
    }

    public Task<EconomicObservationProviderPage> GetObservationsSinceAsync(
        string externalSeriesId,
        DateOnly sinceExclusive,
        int offset,
        int limit,
        CancellationToken cancellationToken = default) =>
        GetObservationsAsync(
            new EconomicObservationProviderRequest(
                externalSeriesId,
                sinceExclusive.AddDays(1),
                DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
                offset,
                limit),
            cancellationToken);

    public Task<EconomicProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Enabled
            && string.Equals(options.Provider, "FRED", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            && baseUri.Scheme is "http" or "https"
            && IsConfigured(options.ApiKey);
        var status = new EconomicProviderStatus(
            "FRED",
            !options.Enabled
                ? EconomicProviderState.Disabled
                : configured
                    ? EconomicProviderState.Available
                    : EconomicProviderState.ConfigurationError,
            options.Enabled,
            !options.Enabled
                ? "Economic-data collection is disabled."
                : configured
                    ? "FRED configuration is available; no provider request was spent on this health check."
                    : "FRED configuration is incomplete or invalid.",
            timeProvider.GetUtcNow());
        return Task.FromResult(status);
    }

    private async Task<T> GetAsync<T>(
        string path,
        IReadOnlyList<(string Name, string Value)> parameters,
        CancellationToken cancellationToken)
    {
        await WaitForLocalRateLimitAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Get, BuildUri(path, parameters));
        message.Headers.Accept.ParseAdd("application/json");
        try
        {
            using var response = await httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw ProviderFailure(response);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            try
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, timeout.Token)
                    ?? throw InvalidResponse("The economic provider returned an empty response.");
            }
            catch (JsonException)
            {
                throw InvalidResponse("The economic provider returned a malformed response.");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.Timeout,
                "The economic provider request timed out.",
                transient: true);
        }
        catch (HttpRequestException)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.ProviderUnavailable,
                "The economic provider is temporarily unavailable.",
                transient: true);
        }
    }

    private Uri BuildUri(string path, IReadOnlyList<(string Name, string Value)> parameters)
    {
        var baseUri = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/");
        var query = parameters
            .Append((Name: "api_key", Value: options.ApiKey))
            .Append((Name: "file_type", Value: "json"))
            .Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Name)}={Uri.EscapeDataString(parameter.Value)}");
        return new UriBuilder(new Uri(baseUri, path)) { Query = string.Join('&', query) }.Uri;
    }

    private async Task WaitForLocalRateLimitAsync(CancellationToken cancellationToken)
    {
        if (options.RateLimitPerMinute <= 0)
        {
            return;
        }

        await _rateGate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (_nextAllowedAtUtc > now)
            {
                await Task.Delay(_nextAllowedAtUtc - now, cancellationToken);
                now = timeProvider.GetUtcNow();
            }

            _nextAllowedAtUtc = now.AddMinutes(1d / options.RateLimitPerMinute);
        }
        finally
        {
            _rateGate.Release();
        }
    }

    private void EnsureEnabled()
    {
        if (!options.Enabled)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.Disabled,
                "Economic-data collection is disabled by configuration.");
        }
    }

    private static void ValidateRequest(EconomicObservationProviderRequest request)
    {
        ValidateSeriesId(request.ExternalSeriesId);
        if (request.From > request.To || request.Offset < 0 || request.Limit is < 1 or > 100_000)
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.InvalidRequest,
                "The economic provider request contains an invalid range or page boundary.");
        }
    }

    private static void ValidateSeriesId(string externalSeriesId)
    {
        if (string.IsNullOrWhiteSpace(externalSeriesId)
            || externalSeriesId.Length > 120
            || externalSeriesId.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
        {
            throw new EconomicDataException(
                EconomicDataErrorCodes.InvalidRequest,
                "The economic series identifier is invalid.");
        }
    }

    private EconomicDataException ProviderFailure(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta
            ?? response.Headers.RetryAfter?.Date - timeProvider.GetUtcNow();
        if (retryAfter < TimeSpan.Zero)
        {
            retryAfter = null;
        }
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new EconomicDataException(
                EconomicDataErrorCodes.AuthenticationFailed,
                "The economic provider rejected its server-side credentials."),
            HttpStatusCode.TooManyRequests or HttpStatusCode.Locked => new EconomicDataException(
                EconomicDataErrorCodes.RateLimited,
                "The economic provider rate limit was reached.",
                transient: true,
                retryAfter: retryAfter),
            HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity =>
                new EconomicDataException(
                    EconomicDataErrorCodes.InvalidRequest,
                    "The economic provider rejected the request."),
            _ when (int)response.StatusCode >= 500 => new EconomicDataException(
                EconomicDataErrorCodes.ProviderUnavailable,
                "The economic provider is temporarily unavailable.",
                transient: true),
            _ => new EconomicDataException(
                EconomicDataErrorCodes.ProviderUnavailable,
                "The economic provider request failed.")
        };
    }

    private static EconomicDataException InvalidResponse(string message) =>
        new(EconomicDataErrorCodes.InvalidResponse, message);

    private static DateOnly? ParseOptionalDate(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            return parsed;
        }

        throw InvalidResponse($"The economic provider returned an invalid {field} date.");
    }

    private static DateTimeOffset? ParseOptionalTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        throw InvalidResponse("The economic provider returned an invalid last-updated timestamp.");
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Limit(string? value, int maximumLength) =>
        value is not null && value.Length > maximumLength
            ? value[..maximumLength]
            : value;

    private static bool IsConfigured(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("USER_PROVIDED_LATER", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        _rateGate.Dispose();
        httpClient.Dispose();
    }

    private sealed class FredSeriesResponse
    {
        [JsonPropertyName("seriess")]
        public List<FredSeries>? Series { get; init; }
    }

    private sealed class FredSeries
    {
        public string? Id { get; init; }
        public string? Title { get; init; }
        public string? Notes { get; init; }
        public string? Units { get; init; }
        public string? Frequency { get; init; }
        [JsonPropertyName("seasonal_adjustment")]
        public string? SeasonalAdjustment { get; init; }
        [JsonPropertyName("observation_start")]
        public string? ObservationStart { get; init; }
        [JsonPropertyName("observation_end")]
        public string? ObservationEnd { get; init; }
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; init; }
    }

    private sealed class FredObservationsResponse
    {
        public int Count { get; init; }
        public int Offset { get; init; }
        public int Limit { get; init; }
        public List<FredObservation>? Observations { get; init; }
    }

    private sealed class FredObservation
    {
        public string? Date { get; init; }
        public string? Value { get; init; }
        [JsonPropertyName("realtime_start")]
        public string? RealtimeStart { get; init; }
        [JsonPropertyName("realtime_end")]
        public string? RealtimeEnd { get; init; }
    }
}
