using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.FullAnalysis;
using XauAi.Infrastructure.FullAnalysis;

namespace XauAi.UnitTests.FullAnalysis;

public sealed class FullAiProviderTests
{
    [Fact]
    public async Task Compatible_adapter_sends_low_cost_controls_and_reports_the_serving_model()
    {
        var handler = new StubHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"models\":[\"fallback-model\"]", body, StringComparison.Ordinal);
            Assert.Contains("\"reasoning\":{\"enabled\":false}", body, StringComparison.Ordinal);
            Assert.Contains("\"usage\":{\"include\":true}", body, StringComparison.Ordinal);
            const string output = "{\"decision\":\"wait\"}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"content\":"
                    + System.Text.Json.JsonSerializer.Serialize(output)
                    + "}}],\"model\":\"fallback-model\",\"usage\":{\"prompt_tokens\":100,\"completion_tokens\":20}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var provider = new FullOpenAiCompatibleProvider(
            new HttpClient(handler),
            TimeProvider.System,
            NullLogger<FullOpenAiCompatibleProvider>.Instance);

        var result = await provider.AnalyzeAsync(
            new FullAiRequest(
                FullWorkspace.Master,
                "XAUUSD",
                "M5",
                DateTimeOffset.UtcNow,
                "phase14-master-v1",
                "state",
                "{\"snapshot\":{}}",
                []),
            new FullWorkspaceConfiguration(
                FullWorkspace.Master,
                true,
                "OpenRouter",
                "OpenAiCompatible",
                true,
                "secret-provider-key",
                "primary-model",
                ["fallback-model"],
                "https://provider.example/v1/",
                0.1,
                5,
                800,
                true,
                0,
                0,
                "phase14-master-v1",
                "phase14-lowcost-v2"));

        Assert.Equal("fallback-model", result.Model);
        Assert.Equal(100, result.InputTokens);
        Assert.Equal(20, result.OutputTokens);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task Groq_request_uses_supported_fields_and_advances_to_fallback(HttpStatusCode firstStatus)
    {
        var bodies = new List<string>();
        var handler = new StubHandler(async request =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) return new HttpResponseMessage(firstStatus);

            const string output = "{\"decision\":\"wait\"}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"content\":"
                    + System.Text.Json.JsonSerializer.Serialize(output)
                    + "}}],\"model\":\"fallback-model\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var provider = new FullOpenAiCompatibleProvider(
            new HttpClient(handler),
            TimeProvider.System,
            NullLogger<FullOpenAiCompatibleProvider>.Instance);

        var result = await provider.AnalyzeAsync(
            new FullAiRequest(FullWorkspace.Master, "XAUUSD", "M5", DateTimeOffset.UtcNow, "v1", "state", "{}", []),
            new FullWorkspaceConfiguration(
                FullWorkspace.Master, true, "Groq", "OpenAiCompatible", true, "secret", "primary-model",
                ["fallback-model"], "https://api.groq.com/openai/v1/", 0.1, 5, 800, true, 1, 0, "v1", "v1"));

        Assert.Equal(2, bodies.Count);
        Assert.Contains("\"model\":\"primary-model\"", bodies[0], StringComparison.Ordinal);
        Assert.Contains("\"model\":\"fallback-model\"", bodies[1], StringComparison.Ordinal);
        Assert.Contains("\"reasoning_effort\":\"low\"", bodies[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"models\"", bodies[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"reasoning\":{\"enabled\"", bodies[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"usage\"", bodies[0], StringComparison.Ordinal);
        Assert.Equal("fallback-model", result.Model);
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => response(request);
    }
}
