namespace XauAi.Application.AI;

public static class AiProviderAccountStates
{
    public const string Available = "Available";
    public const string QuotaExhausted = "QuotaExhausted";
    public const string Unavailable = "Unavailable";
    public const string Unverified = "Unverified";
}

public sealed record AiProviderAccountRequest(
    string Provider,
    string BaseUrl,
    string ApiKey);

public sealed record AiProviderAccountStatus(
    string Provider,
    string State,
    bool CanGenerate,
    bool? IsFreeTier,
    int? DailyUsed,
    int? DailyLimit,
    int? DailyRemaining,
    string Message,
    DateTimeOffset CheckedAtUtc);

public interface IAiProviderAccountStatusService
{
    Task<AiProviderAccountStatus> GetStatusAsync(
        AiProviderAccountRequest request,
        CancellationToken cancellationToken = default);
}

public interface IScopedAiProviderRequestGate
{
    Task WaitAsync(
        string analysisScope,
        string provider,
        string baseUrl,
        string apiKey,
        int requestsPerMinute,
        CancellationToken cancellationToken = default);
}
