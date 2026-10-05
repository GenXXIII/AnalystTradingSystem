using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.AI;
using XauAi.Infrastructure.AI;

namespace XauAi.UnitTests.AI;

public sealed class OpenAiCompatibleProviderTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AiInterpretationErrorCodes.AuthenticationFailed)]
    [InlineData(HttpStatusCode.TooManyRequests, AiInterpretationErrorCodes.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AiInterpretationErrorCodes.Unavailable)]
    public async Task Provider_maps_failures_to_safe_structured_codes(
        HttpStatusCode statusCode,
        string expectedCode)
    {
        var provider = CreateProvider(new StubHandler(_ => new HttpResponseMessage(statusCode)));

        var exception = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.InterpretAsync(Request(), Configuration(maxRetries: 0)));

        Assert.Equal(expectedCode, exception.Code);
        Assert.DoesNotContain("secret-test-key", exception.SafeMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Provider_rejects_success_responses_without_structured_content()
    {
        var provider = CreateProvider(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"choices\":[]}", Encoding.UTF8, "application/json")
        }));

        var exception = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.InterpretAsync(Request(), Configuration(maxRetries: 0)));

        Assert.Equal(AiInterpretationErrorCodes.InvalidResponse, exception.Code);
    }

    [Fact]
    public async Task Provider_maps_malformed_success_json_to_a_safe_invalid_response()
    {
        var provider = CreateProvider(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not-json", Encoding.UTF8, "application/json")
        }));

        var exception = await Assert.ThrowsAsync<AiProviderException>(() =>
            provider.InterpretAsync(Request(), Configuration(maxRetries: 0)));

        Assert.Equal(AiInterpretationErrorCodes.InvalidResponse, exception.Code);
        Assert.DoesNotContain("{not-json", exception.SafeMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Provider_returns_json_and_token_usage_from_compatible_response()
    {
        const string interpretation = "{\"interpretationType\":\"newsEvent\"}";
        var responseJson = "{\"choices\":[{\"message\":{\"content\":"
            + System.Text.Json.JsonSerializer.Serialize(interpretation)
            + "}}],\"usage\":{\"prompt_tokens\":120,\"completion_tokens\":45}}";
        var provider = CreateProvider(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));

        var result = await provider.InterpretAsync(Request(), Configuration(maxRetries: 0));

        Assert.Equal(interpretation, result.Json);
        Assert.Equal(120, result.InputTokens);
        Assert.Equal(45, result.OutputTokens);
    }

    private static OpenAiCompatibleProvider CreateProvider(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        TimeProvider.System,
        NullLogger<OpenAiCompatibleProvider>.Instance);

    private static AiProviderRequest Request() => new(
        AiSpecialist.News,
        AiInterpretationType.NewsEvent,
        "XAUUSD",
        null,
        DateTimeOffset.UtcNow,
        "phase11-v2",
        "{\"evidence\":[]}",
        [Guid.NewGuid()]);

    private static AiSpecialistConfiguration Configuration(int maxRetries) => new(
        AiSpecialist.News,
        true,
        "TestProvider",
        "OpenAiCompatible",
        true,
        "secret-test-key",
        "test-model",
        "https://example.test/v1/",
        0.2,
        5,
        1000,
        maxRetries,
        0);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
