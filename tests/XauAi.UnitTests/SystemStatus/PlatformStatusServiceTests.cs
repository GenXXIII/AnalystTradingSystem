using Microsoft.Extensions.DependencyInjection;
using XauAi.Application;
using XauAi.Application.SystemStatus;

namespace XauAi.UnitTests.SystemStatus;

public sealed class PlatformStatusServiceTests
{
    [Fact]
    public void GetCurrent_returns_deterministic_platform_metadata()
    {
        var expectedTime = new DateTimeOffset(2026, 10, 1, 8, 30, 0, TimeSpan.Zero);
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(expectedTime));

        using var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<IPlatformStatusService>();

        var result = sut.GetCurrent();

        Assert.Equal("XAUUSD AI API", result.Service);
        Assert.Equal("operational", result.Status);
        Assert.Equal(expectedTime, result.CheckedAtUtc);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
