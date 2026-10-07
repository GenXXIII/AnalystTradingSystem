using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace XauAi.IntegrationTests;

public sealed class LocalAnalystApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Evaluate_returns_a_safe_disabled_response_when_local_analyst_is_opted_out()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/local-analyst/XAUUSD/M5/evaluate", null);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("LOCAL_ANALYST_DISABLED", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Invalid_timeframe_returns_safe_bad_request()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/local-analyst/XAUUSD/H2/evaluate", null);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("LOCAL_ANALYST_INVALID_REQUEST", body.GetProperty("error").GetProperty("code").GetString());
    }
}

