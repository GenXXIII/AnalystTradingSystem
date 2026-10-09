using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.TargetAnalysis;
using XauAi.Infrastructure.TargetAnalysis;

namespace XauAi.UnitTests.TargetAnalysis;

public sealed class TargetAiProviderTests
{
    [Fact]
    public async Task Compatible_adapter_uses_the_independently_configured_provider_url_and_model()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler(async request =>
        {
            captured = request;
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("provider-specific-model", body, StringComparison.Ordinal);
            Assert.Contains("\"models\":[\"fallback-model\"]", body, StringComparison.Ordinal);
            Assert.Contains("\"reasoning\":{\"enabled\":false}", body, StringComparison.Ordinal);
            Assert.Contains("\"usage\":{\"include\":true}", body, StringComparison.Ordinal);
            Assert.Contains("target_master_result", body, StringComparison.Ordinal);
            const string output = "{\"validTarget\":false}";
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
        var provider = Provider(handler);

        var result = await provider.AnalyzeAsync(Request(), Configuration(maxRetries: 0));

        Assert.Equal("https://provider.example/v1/chat/completions", captured?.RequestUri?.ToString());
        Assert.Equal("{\"validTarget\":false}", result.Json);
        Assert.Equal("fallback-model", result.Model);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, TargetAnalysisErrorCodes.AuthenticationFailed)]
    [InlineData(HttpStatusCode.TooManyRequests, TargetAnalysisErrorCodes.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, TargetAnalysisErrorCodes.Unavailable)]
    public async Task Compatible_adapter_maps_provider_failures_to_safe_codes(
        HttpStatusCode statusCode,
        string expectedCode)
    {
        var provider = Provider(new StubHandler(_ =>
            Task.FromResult(new HttpResponseMessage(statusCode))));

        var exception = await Assert.ThrowsAsync<TargetAnalysisException>(() =>
            provider.AnalyzeAsync(Request(), Configuration(maxRetries: 0)));

        Assert.Equal(expectedCode, exception.Code);
        Assert.DoesNotContain("secret-provider-key", exception.SafeMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    public async Task Groq_request_uses_supported_fields_and_advances_to_fallback(HttpStatusCode firstStatus)
    {
        var bodies = new List<string>();
        var handler = new StubHandler(async request =>
        {
            bodies.Add(await request.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) return new HttpResponseMessage(firstStatus);

            const string output = "{\"validTarget\":false}";
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

        var result = await Provider(handler).AnalyzeAsync(Request(), Configuration(maxRetries: 1, provider: "Groq"));

        Assert.Equal(2, bodies.Count);
        Assert.Contains("\"model\":\"provider-specific-model\"", bodies[0], StringComparison.Ordinal);
        Assert.Contains("\"model\":\"fallback-model\"", bodies[1], StringComparison.Ordinal);
        Assert.Contains("\"reasoning_effort\":\"low\"", bodies[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"models\"", bodies[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"reasoning\":{\"enabled\"", bodies[0], StringComparison.Ordinal);
        Assert.DoesNotContain("\"usage\"", bodies[0], StringComparison.Ordinal);
        Assert.Equal("fallback-model", result.Model);
    }

    private static TargetOpenAiCompatibleProvider Provider(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        TimeProvider.System,
        NullLogger<TargetOpenAiCompatibleProvider>.Instance);

    private static TargetAiRequest Request() => new(
        TargetWorkspace.Master,
        "XAUUSD",
        "M5",
        DateTimeOffset.UtcNow,
        "phase13-master-v1",
        "{\"snapshot\":{}}",
        []);

    private static TargetWorkspaceConfiguration Configuration(int maxRetries, string provider = "OpenRouter") => new(
        TargetWorkspace.Master,
        true,
        provider,
        "OpenAiCompatible",
        true,
        "secret-provider-key",
        "provider-specific-model",
        ["fallback-model"],
        "https://provider.example/v1/",
        0.1,
        5,
        1000,
        true,
        maxRetries,
        0,
        "phase13-master-v1",
        "phase13-v1");

    private sealed class StubHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => response(request);
    }
}
