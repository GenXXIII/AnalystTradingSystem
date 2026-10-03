namespace XauAi.Application.SystemStatus;

public sealed record PlatformStatus(
    string Service,
    string Status,
    DateTimeOffset CheckedAtUtc);
