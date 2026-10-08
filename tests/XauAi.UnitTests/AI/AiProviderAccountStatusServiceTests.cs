using System.Net;
using System.Text;
using XauAi.Application.AI;
using XauAi.Infrastructure.AI;

namespace XauAi.UnitTests.AI;

public sealed class AiProviderAccountStatusServiceTests
{
    [Fact]
    public async Task Free_tier_is_blocked_when_its_daily_model_quota_is_exhausted()
    {
        var status = await Service(isFreeTier: true).GetStatusAsync(Request());

        Assert.Equal(AiProviderAccountStates.QuotaExhausted, status.State);
        Assert.False(status.CanGenerate);
        Assert.Equal(0, status.DailyRemaining);
    }

    [Fact]
    public async Task Funded_account_can_use_paid_primary_models_when_free_model_quota_is_exhausted()
    {
        var status = await Service(isFreeTier: false).GetStatusAsync(Request());

        Assert.Equal(AiProviderAccountStates.Available, status.State);
        Assert.True(status.CanGenerate);
        Assert.Equal(0, status.DailyRemaining);
        Assert.Contains("paid primary models remain available", status.Message, StringComparison.Ordinal);
    }

    private static AiProviderAccountStatusService Service(bool isFreeTier)
    {
        var json = $$"""
            {
              "data": {
                "is_free_tier": {{isFreeTier.ToString().ToLowerInvariant()}},
                "free_model_daily_requests": {
                  "used": 50,
                  "limit": 50,
                  "remaining": 0
                }
              }
            }
            """;
        return new AiProviderAccountStatusService(
            new HttpClient(new StubHandler(json)),
            TimeProvider.System);
    }

    private static AiProviderAccountRequest Request() => new(
        "OpenRouter",
        "https://openrouter.ai/api/v1/",
        "test-key");

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
