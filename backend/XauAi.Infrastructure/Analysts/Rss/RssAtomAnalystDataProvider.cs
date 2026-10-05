using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using XauAi.Application.Analysts;
using XauAi.Infrastructure.Configuration.Options;

namespace XauAi.Infrastructure.Analysts.Rss;

internal sealed class RssAtomAnalystDataProvider(
    HttpClient httpClient,
    AnalystOptions options,
    TimeProvider timeProvider) : IAnalystDataProvider
{
    private readonly SemaphoreSlim _rateGate = new(1, 1);
    private DateTimeOffset _nextAllowedAtUtc = DateTimeOffset.MinValue;

    public Task<AnalystProviderPage> GetLatestAnalystItemsAsync(
        AnalystProviderRequest request,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(request, cancellationToken);

    public Task<AnalystProviderPage> GetHistoricalAnalystItemsAsync(
        AnalystProviderRequest request,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(request, cancellationToken);

    public async Task<ProviderAnalystItem?> GetAnalystItemAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new AnalystException(
                AnalystErrorCodes.InvalidRequest,
                "The analyst provider item identifier is required.");
        }

        var now = timeProvider.GetUtcNow();
        var page = await GetPageAsync(
            new AnalystProviderRequest(
                now.AddDays(-options.MaximumCollectionRangeDays),
                now,
                options.ProviderPageSize),
            cancellationToken);
        return page.Items.FirstOrDefault(item =>
            string.Equals(item.ExternalId, externalId, StringComparison.Ordinal));
    }

    public Task<AnalystProviderPage> SearchAsync(
        AnalystProviderRequest request,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(request, cancellationToken);

    public Task<AnalystProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var enabled = options.Enabled;
        var configured = options.Provider.Equals("RssAtom", StringComparison.OrdinalIgnoreCase)
            && options.SourceType == AnalystSourceType.RssFeed
            && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
            && (!options.RequiresApiKey || IsConfiguredSecret(options.ApiKey));
        var state = !enabled ? AnalystProviderState.Disabled
            : configured ? AnalystProviderState.Available
            : AnalystProviderState.ConfigurationError;
        var message = state switch
        {
            AnalystProviderState.Disabled => "Analyst RSS/Atom collection is disabled.",
            AnalystProviderState.Available => "The configured RSS/Atom source is ready; persisted synchronization state reports operational outcomes.",
            _ => "Analyst RSS/Atom configuration is incomplete."
        };
        return Task.FromResult(new AnalystProviderStatus(
            options.Provider,
            state,
            enabled,
            message,
            timeProvider.GetUtcNow()));
    }

    private async Task<AnalystProviderPage> GetPageAsync(
        AnalystProviderRequest request,
        CancellationToken cancellationToken)
    {
        EnsureEnabled();
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            return new AnalystProviderPage([], null, 0);
        }

        await WaitForLocalRateLimitAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Get, options.BaseUrl);
        message.Headers.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/xml, text/xml");
        message.Headers.UserAgent.ParseAdd("XAUUSD-AI/1.0 analyst-data-collector");
        if (options.RequiresApiKey)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }

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
            XDocument document;
            try
            {
                var xmlSettings = new XmlReaderSettings
                {
                    Async = true,
                    DtdProcessing = DtdProcessing.Prohibit,
                    MaxCharactersInDocument = 2_000_000,
                    XmlResolver = null
                };
                using var reader = XmlReader.Create(stream, xmlSettings);
                document = await XDocument.LoadAsync(reader, LoadOptions.None, timeout.Token);
            }
            catch (XmlException exception)
            {
                throw new AnalystException(
                    AnalystErrorCodes.InvalidResponse,
                    "The analyst RSS/Atom source returned malformed XML.",
                    innerException: exception);
            }

            var parsed = Parse(document);
            var filtered = parsed
                .Where(item => !item.PublishedAtUtc.HasValue
                    || (item.PublishedAtUtc.Value >= request.FromUtc
                        && item.PublishedAtUtc.Value <= request.ToUtc))
                .Where(item => string.IsNullOrWhiteSpace(request.Search)
                    || Contains(item.Title, request.Search)
                    || Contains(item.Summary, request.Search)
                    || Contains(item.PermittedContent, request.Search))
                .OrderByDescending(item => item.PublishedAtUtc)
                .ThenBy(item => item.ExternalId, StringComparer.Ordinal)
                .Take(request.PageSize)
                .ToArray();
            return new AnalystProviderPage(filtered, null, parsed.Count);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AnalystException(
                AnalystErrorCodes.Timeout,
                "The analyst provider request timed out.",
                transient: true,
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new AnalystException(
                AnalystErrorCodes.ProviderUnavailable,
                "The analyst provider is temporarily unavailable.",
                transient: true,
                innerException: exception);
        }
    }

    internal static IReadOnlyList<ProviderAnalystItem> Parse(XDocument document)
    {
        var root = document.Root ?? throw new AnalystException(
            AnalystErrorCodes.InvalidResponse,
            "The analyst RSS/Atom source returned an empty document.");
        if (root.Name.LocalName.Equals("feed", StringComparison.OrdinalIgnoreCase))
        {
            return ParseAtom(root);
        }

        if (root.Name.LocalName.Equals("rss", StringComparison.OrdinalIgnoreCase)
            || root.Name.LocalName.Equals("rdf", StringComparison.OrdinalIgnoreCase))
        {
            return ParseRss(root);
        }

        throw new AnalystException(
            AnalystErrorCodes.InvalidResponse,
            "The analyst source is not a supported RSS or Atom document.");
    }

    private static IReadOnlyList<ProviderAnalystItem> ParseRss(XElement root)
    {
        var channel = root.Elements().FirstOrDefault(element => LocalName(element) == "channel") ?? root;
        var sourceName = Text(channel, "title");
        var sourceWebsite = Text(channel, "link");
        var itemContainer = LocalName(root) == "rdf" ? root : channel;
        return [.. itemContainer.Elements()
            .Where(element => LocalName(element) == "item")
            .Select(item => new ProviderAnalystItem(
                Text(item, "guid") ?? Text(item, "link"),
                Clean(Text(item, "title")),
                Clean(Text(item, "description")),
                null,
                Text(item, "link"),
                ParseDate(Text(item, "pubDate") ?? Text(item, "published") ?? Text(item, "date")),
                ParseDate(Text(item, "updated")),
                Text(item, "language") ?? Text(channel, "language"),
                Text(item, "category"),
                new ProviderAnalystSource(
                    Text(channel, "id") ?? sourceWebsite,
                    sourceName,
                    "Publication",
                    sourceWebsite,
                    null),
                Identity(item),
                []))];
    }

    private static IReadOnlyList<ProviderAnalystItem> ParseAtom(XElement feed)
    {
        var sourceName = Clean(Text(feed, "title"));
        var sourceWebsite = Link(feed);
        return [.. feed.Elements()
            .Where(element => LocalName(element) == "entry")
            .Select(entry => new ProviderAnalystItem(
                Text(entry, "id") ?? Link(entry),
                Clean(Text(entry, "title")),
                Clean(Text(entry, "summary")),
                null,
                Link(entry),
                ParseDate(Text(entry, "published") ?? Text(entry, "updated")),
                ParseDate(Text(entry, "updated")),
                entry.Attribute(XNamespace.Xml + "lang")?.Value ?? feed.Attribute(XNamespace.Xml + "lang")?.Value,
                entry.Elements().FirstOrDefault(element => LocalName(element) == "category")?.Attribute("term")?.Value,
                new ProviderAnalystSource(
                    Text(feed, "id") ?? sourceWebsite,
                    sourceName,
                    "Publication",
                    sourceWebsite,
                    null),
                Identity(entry),
                []))];
    }

    private static ProviderAnalystIdentity? Identity(XElement item)
    {
        var author = item.Elements().FirstOrDefault(element => LocalName(element) == "author");
        var name = author is null ? Text(item, "creator") : Text(author, "name") ?? Clean(author.Value);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var uri = author is null ? null : Text(author, "uri");
        var email = author is null ? null : Text(author, "email");
        return new ProviderAnalystIdentity(email ?? uri, name, null, uri);
    }

    private static string? Link(XElement element) =>
        element.Elements()
            .Where(child => LocalName(child) == "link")
            .OrderBy(child => string.Equals(child.Attribute("rel")?.Value, "alternate", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(child => child.Attribute("href")?.Value ?? child.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

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
            throw new AnalystException(
                AnalystErrorCodes.Disabled,
                "Analyst-data collection is disabled by configuration.");
        }
    }

    private static AnalystException ProviderFailure(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new AnalystException(
                AnalystErrorCodes.AuthenticationFailed,
                "The analyst provider rejected authentication."),
            HttpStatusCode.TooManyRequests => new AnalystException(
                AnalystErrorCodes.RateLimited,
                "The analyst provider rate limit was reached.",
                transient: true,
                retryAfter: retryAfter),
            HttpStatusCode.RequestTimeout => new AnalystException(
                AnalystErrorCodes.Timeout,
                "The analyst provider request timed out.",
                transient: true),
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new AnalystException(
                AnalystErrorCodes.InvalidRequest,
                "The analyst provider rejected the bounded request."),
            _ when (int)response.StatusCode >= 500 => new AnalystException(
                AnalystErrorCodes.ProviderUnavailable,
                "The analyst provider is temporarily unavailable.",
                transient: true,
                retryAfter: retryAfter),
            _ => new AnalystException(
                AnalystErrorCodes.ProviderUnavailable,
                "The analyst provider request failed.")
        };
    }

    private static string? Text(XElement element, string localName) =>
        element.Elements()
            .FirstOrDefault(child => LocalName(child) == localName.ToLowerInvariant())?.Value;

    private static string LocalName(XElement element) => element.Name.LocalName.ToLowerInvariant();

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var withoutTags = Regex.Replace(
            value,
            "<[^>]+>",
            " ",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(
            decoded.Trim(),
            @"\s+",
            " ",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;

    private static bool Contains(string? value, string search) =>
        value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsConfiguredSecret(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("PROVIDED_LATER", StringComparison.OrdinalIgnoreCase);
}
