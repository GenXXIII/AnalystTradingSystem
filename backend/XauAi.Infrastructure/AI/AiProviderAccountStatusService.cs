using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XauAi.Application.AI;

namespace XauAi.Infrastructure.AI;

internal sealed class AiProviderAccountStatusService(
    HttpClient httpClient,
    TimeProvider timeProvider) : IAiProviderAccountStatusService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, CachedStatus> cache = new(StringComparer.Ordinal);

    public async Task<AiProviderAccountStatus> GetStatusAsync(
        AiProviderAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return new AiProviderAccountStatus(
                request.Provider,
                AiProviderAccountStates.Unavailable,
                false,
                null,
                null,
                null,
                null,
                $"{request.Provider} is missing its API key.",
                now);
        }

        if (!IsOpenRouter(request))
        {
            return new AiProviderAccountStatus(
                request.Provider,
                AiProviderAccountStates.Unverified,
                true,
                null,
                null,
                null,
                null,
                $"{request.Provider} does not expose a supported account-quota endpoint.",
                now);
        }

        var scope = ScopeKey(request);
        if (cache.TryGetValue(scope, out var cached) && now - cached.StoredAtUtc <= CacheDuration)
        {
            return cached.Status;
        }

        AiProviderAccountStatus status;
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, BuildOpenRouterAuthUri(request.BaseUrl));
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey.Trim());
            using var response = await httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                status = Unavailable(request.Provider, "OpenRouter rejected the configured API key.", now);
            }
            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                status = QuotaExhausted(
                    request.Provider,
                    null,
                    null,
                    null,
                    "OpenRouter is currently rate-limiting this API key. Wait for the provider reset or use another funded key.",
                    now);
            }
            else if (!response.IsSuccessStatusCode)
            {
                status = Unverified(
                    request.Provider,
                    $"OpenRouter account quota could not be verified (HTTP {(int)response.StatusCode}).",
                    now);
            }
            else
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                status = ParseOpenRouterStatus(request.Provider, document.RootElement, now);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            status = Unverified(request.Provider, "OpenRouter account quota check timed out.", now);
        }
        catch (HttpRequestException)
        {
            status = Unverified(request.Provider, "OpenRouter account quota could not be reached.", now);
        }
        catch (JsonException)
        {
            status = Unverified(request.Provider, "OpenRouter returned an unreadable account-quota response.", now);
        }

        cache[scope] = new CachedStatus(status, now);
        return status;
    }

    private static AiProviderAccountStatus ParseOpenRouterStatus(
        string provider,
        JsonElement root,
        DateTimeOffset checkedAtUtc)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return Unverified(provider, "OpenRouter returned an incomplete account-quota response.", checkedAtUtc);
        }

        var isFreeTier = data.TryGetProperty("is_free_tier", out var freeTierElement)
            && freeTierElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? freeTierElement.GetBoolean()
                : (bool?)null;
        int? used = null;
        int? limit = null;
        int? remaining = null;
        if (data.TryGetProperty("free_model_daily_requests", out var daily)
            && daily.ValueKind == JsonValueKind.Object)
        {
            used = ReadInt(daily, "used");
            limit = ReadInt(daily, "limit");
            remaining = ReadInt(daily, "remaining");
        }

        remaining ??= used.HasValue && limit.HasValue ? Math.Max(0, limit.Value - used.Value) : null;
        if (remaining is <= 0 && limit.HasValue && isFreeTier != false)
        {
            var usage = used.HasValue ? $"{used}/{limit}" : $"{limit}/{limit}";
            return QuotaExhausted(
                provider,
                isFreeTier,
                used,
                limit,
                $"OpenRouter free-model daily quota is exhausted ({usage} used). Wait for the provider reset or use a funded key.",
                checkedAtUtc);
        }

        var message = isFreeTier == false && remaining is <= 0
            ? "OpenRouter funded access is ready; the free-model daily quota is exhausted, so paid primary models remain available."
            : remaining.HasValue
                ? $"OpenRouter is ready with {remaining} free-model daily requests remaining."
                : "OpenRouter authentication is ready; no free-model daily limit was reported.";
        return new AiProviderAccountStatus(
            provider,
            AiProviderAccountStates.Available,
            true,
            isFreeTier,
            used,
            limit,
            remaining,
            message,
            checkedAtUtc);
    }

    private static int? ReadInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static AiProviderAccountStatus QuotaExhausted(
        string provider,
        bool? isFreeTier,
        int? used,
        int? limit,
        string message,
        DateTimeOffset checkedAtUtc) => new(
            provider,
            AiProviderAccountStates.QuotaExhausted,
            false,
            isFreeTier,
            used,
            limit,
            used.HasValue && limit.HasValue ? Math.Max(0, limit.Value - used.Value) : 0,
            message,
            checkedAtUtc);

    private static AiProviderAccountStatus Unavailable(
        string provider,
        string message,
        DateTimeOffset checkedAtUtc) => new(
            provider,
            AiProviderAccountStates.Unavailable,
            false,
            null,
            null,
            null,
            null,
            message,
            checkedAtUtc);

    private static AiProviderAccountStatus Unverified(
        string provider,
        string message,
        DateTimeOffset checkedAtUtc) => new(
            provider,
            AiProviderAccountStates.Unverified,
            true,
            null,
            null,
            null,
            null,
            message,
            checkedAtUtc);

    private static bool IsOpenRouter(AiProviderAccountRequest request) =>
        string.Equals(request.Provider.Trim(), "OpenRouter", StringComparison.OrdinalIgnoreCase)
        || (Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out var uri)
            && uri.Host.EndsWith("openrouter.ai", StringComparison.OrdinalIgnoreCase));

    private static Uri BuildOpenRouterAuthUri(string baseUrl)
    {
        var normalized = baseUrl.Trim().TrimEnd('/');
        if (normalized.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri($"{normalized}/auth/key", UriKind.Absolute);
        }

        var root = new Uri(normalized, UriKind.Absolute);
        return new Uri(root, "/api/v1/auth/key");
    }

    private static string ScopeKey(AiProviderAccountRequest request)
    {
        var normalized = $"{request.Provider.Trim().ToUpperInvariant()}|{request.BaseUrl.Trim().TrimEnd('/').ToUpperInvariant()}|{request.ApiKey.Trim()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
    }

    private sealed record CachedStatus(AiProviderAccountStatus Status, DateTimeOffset StoredAtUtc);
}
