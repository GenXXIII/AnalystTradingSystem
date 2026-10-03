namespace XauAi.Application.SystemStatus;

internal sealed class PlatformStatusService(TimeProvider timeProvider) : IPlatformStatusService
{
    public PlatformStatus GetCurrent() =>
        new("XAUUSD AI API", "operational", timeProvider.GetUtcNow());
}
