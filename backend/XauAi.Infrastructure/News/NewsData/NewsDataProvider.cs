using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using XauAi.Application.News;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.News.NewsData;

internal sealed class NewsDataProvider(
    HttpClient httpClient,
    NewsOptions options,
    TimeProvider timeProvider) : INewsProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _rateGate = new(1, 1);
    private DateTimeOffset _nextAllowedAtUtc = DateTimeOffset.MinValue;

    public async Task<NewsProviderPage> GetNewsAsync(
        NewsProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        await WaitForLocalRateLimitAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Get, BuildRequestUri(request));
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
            NewsDataResponse? body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<NewsDataResponse>(stream, JsonOptions, timeout.Token);
            }
            catch (JsonException exception)
            {
                throw new NewsException(
                    NewsErrorCodes.InvalidResponse,
                    "The news provider returned a malformed response.",
                    innerException: exception);
            }

            if (body is null || !string.Equals(body.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                throw new NewsException(
                    NewsErrorCodes.InvalidResponse,
                    "The news provider returned an unsuccessful or incomplete response.");
            }

            return new NewsProviderPage(
                [.. (body.Results ?? []).Select(Map)],
                EmptyToNull(body.NextPage),
                body.TotalResults);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new NewsException(
                NewsErrorCodes.Timeout,
                "The news provider request timed out.",
                transient: true,
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new NewsException(
                NewsErrorCodes.ProviderUnavailable,
                "The news provider is temporarily unavailable.",
                transient: true,
                innerException: exception);
        }
    }

    public Task<NewsProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var enabled = options.Enabled;
        var configured = !string.IsNullOrWhiteSpace(options.ApiKey)
            && !options.ApiKey.Contains("PROVIDED_LATER", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _);
        var state = !enabled ? NewsProviderState.Disabled
            : configured ? NewsProviderState.Available
            : NewsProviderState.ConfigurationError;
        var message = state switch
        {
            NewsProviderState.Disabled => "NewsData.io collection is disabled.",
            NewsProviderState.Available => "NewsData.io is configured; persisted collection state reports operational outcomes.",
            _ => "NewsData.io configuration is incomplete."
        };
        return Task.FromResult(new NewsProviderStatus(
            "NewsData",
            state,
            enabled,
            message,
            timeProvider.GetUtcNow()));
    }

    private Uri BuildRequestUri(NewsProviderRequest request)
    {
        var endpoint = request.UseArchive ? "archive" : "latest";
        var values = new Dictionary<string, string>
        {
            ["apikey"] = options.ApiKey,
            ["q"] = request.Query,
            ["language"] = request.Language,
            ["timezone"] = "UTC",
            ["size"] = request.PageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(request.PageToken))
        {
            values["page"] = request.PageToken;
        }

        if (request.UseArchive)
        {
            values["from_date"] = request.FromUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            values["to_date"] = request.ToUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        else if (options.UseTimeframeParameter)
        {
            var minutes = Math.Clamp(
                (int)Math.Ceiling((timeProvider.GetUtcNow() - request.FromUtc).TotalMinutes),
                1,
                2880);
            values["timeframe"] = $"{minutes}m";
        }

        using var content = new FormUrlEncodedContent(values);
        var query = content.ReadAsStringAsync().GetAwaiter().GetResult();
        return new Uri(new Uri(EnsureTrailingSlash(options.BaseUrl)), $"{endpoint}?{query}");
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
                await Task.Delay(_nextAllowedAtUtc - now, timeProvider, cancellationToken);
            }

            _nextAllowedAtUtc = timeProvider.GetUtcNow()
                .Add(TimeSpan.FromMinutes(1d / options.RateLimitPerMinute));
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
            throw new NewsException(NewsErrorCodes.Disabled, "News collection is disabled by configuration.");
        }
    }

    private static NewsException ProviderFailure(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new NewsException(
                NewsErrorCodes.AuthenticationFailed,
                "The news provider rejected authentication."),
            HttpStatusCode.TooManyRequests => new NewsException(
                NewsErrorCodes.RateLimited,
                "The news provider rate limit was reached.",
                transient: true,
                retryAfter: retryAfter),
            HttpStatusCode.RequestTimeout => new NewsException(
                NewsErrorCodes.Timeout,
                "The news provider request timed out.",
                transient: true),
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new NewsException(
                NewsErrorCodes.InvalidRequest,
                "The news provider rejected the bounded request."),
            _ when (int)response.StatusCode >= 500 => new NewsException(
                NewsErrorCodes.ProviderUnavailable,
                "The news provider is temporarily unavailable.",
                transient: true,
                retryAfter: retryAfter),
            _ => new NewsException(
                NewsErrorCodes.ProviderUnavailable,
                "The news provider request failed.")
        };
    }

    private static ProviderNewsArticle Map(NewsDataArticle article) => new(
        EmptyToNull(article.ArticleId),
        EmptyToNull(article.Title),
        EmptyToNull(article.Description),
        EmptyToNull(article.Content),
        EmptyToNull(article.Link),
        EmptyToNull(article.SourceName) ?? EmptyToNull(article.SourceId),
        EmptyToNull(article.SourceUrl),
        Values(article.Creator),
        ParsePublishedAt(article.PubDate),
        EmptyToNull(article.Language),
        Values(article.Country),
        Values(article.Category),
        Values(article.Keywords),
        EmptyToNull(article.ImageUrl));

    private static DateTimeOffset? ParsePublishedAt(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;

    private static IReadOnlyList<string> Values(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return [.. value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)];
        }

        return value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? [value.GetString()!]
            : [];
    }

    private static string EnsureTrailingSlash(string value) => value.EndsWith('/') ? value : $"{value}/";

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed class NewsDataResponse
    {
        public string? Status { get; set; }

        public int? TotalResults { get; set; }

        public List<NewsDataArticle>? Results { get; set; }

        public string? NextPage { get; set; }
    }

    private sealed class NewsDataArticle
    {
        [JsonPropertyName("article_id")]
        public string? ArticleId { get; set; }

        public string? Title { get; set; }

        public string? Link { get; set; }

        public JsonElement Keywords { get; set; }

        public JsonElement Creator { get; set; }

        public string? Description { get; set; }

        public string? Content { get; set; }

        [JsonPropertyName("pubDate")]
        public string? PubDate { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("source_name")]
        public string? SourceName { get; set; }

        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        public JsonElement Country { get; set; }

        public JsonElement Category { get; set; }

        public string? Language { get; set; }
    }
}
