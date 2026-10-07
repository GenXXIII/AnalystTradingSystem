using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace XauAi.IntegrationTests;

public sealed class TargetAnalystApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task User_trigger_returns_safe_disabled_response_when_target_analyst_is_opted_out()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/target-analyst/jobs", new
        {
            symbol = "XAUUSD",
            timeframe = "M5"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("TARGET_ANALYSIS_DISABLED", body.GetProperty("error").GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Configuration_lists_eight_workspaces_without_secret_values()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/target-analyst/configuration");
        var bodyText = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(bodyText);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(8, body.RootElement.GetProperty("data").GetArrayLength());
        Assert.All(
            body.RootElement.GetProperty("data").EnumerateArray(),
            workspace => Assert.False(workspace.TryGetProperty("apiKey", out _)));
    }
}
