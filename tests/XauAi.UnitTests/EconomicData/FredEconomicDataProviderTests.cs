using System.Net;
using System.Net.Http.Headers;
using System.Text;
using XauAi.Application.EconomicData;
using XauAi.Infrastructure.Configuration.Options;
using XauAi.Infrastructure.EconomicData.Fred;

namespace XauAi.UnitTests.EconomicData;

public sealed class FredEconomicDataProviderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-03T00:00:00Z");

    [Fact]
    public async Task Maps_series_metadata_without_exposing_fred_models()
    {
        var handler = new StubHandler(_ => Json("""
            {
              "seriess": [{
                "id": "CPIAUCSL",
                "title": "Consumer Price Index",
                "observation_start": "1947-01-01",
                "observation_end": "2026-08-01",
                "frequency": "Monthly",
                "units": "Index 1982-1984=100",
                "seasonal_adjustment": "Seasonally Adjusted",
                "last_updated": "2026-09-11 08:37:00-05",
                "notes": "TEST_ONLY metadata"
              }]
            }
            """));
        using var provider = CreateProvider(handler);

        var result = await provider.GetSeriesMetadataAsync("CPIAUCSL");

        Assert.Equal("CPIAUCSL", result.ExternalSeriesId);
        Assert.Equal("Monthly", result.Frequency);
        Assert.Equal(new DateOnly(1947, 1, 1), result.ObservationStartDate);
        Assert.Equal(TimeSpan.Zero, result.ProviderUpdatedAtUtc?.Offset);
        Assert.Contains("series_id=CPIAUCSL", handler.Requests.Single().Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Maps_valid_and_missing_observations_without_converting_missing_to_zero()
    {
        var handler = new StubHandler(_ => Json("""
            {
              "count": 2,
              "offset": 0,
              "limit": 100,
              "observations": [
                { "realtime_start": "2026-09-01", "realtime_end": "2026-09-30", "date": "2026-07-01", "value": "331.052" },
                { "realtime_start": "2026-09-01", "realtime_end": "2026-09-30", "date": "2026-08-01", "value": "." }
              ]
            }
            """));
        using var provider = CreateProvider(handler);

        var result = await provider.GetObservationsAsync(new EconomicObservationProviderRequest(
            "CPIAUCSL", new DateOnly(2026, 7, 1), new DateOnly(2026, 8, 1), 0, 100));

        Assert.Equal(2, result.Observations.Count);
        Assert.Equal(331.052m, result.Observations[0].Value);
        Assert.Equal("Available", result.Observations[0].Status);
        Assert.Null(result.Observations[1].Value);
        Assert.Equal(".", result.Observations[1].OriginalValue);
        Assert.Equal("Missing", result.Observations[1].Status);
    }

    [Fact]
    public async Task Rejects_malformed_or_non_numeric_observations()
    {
        var handler = new StubHandler(_ => Json("""
            { "count": 1, "offset": 0, "limit": 1,
              "observations": [{ "date": "2026-08-01", "value": "not-a-number" }] }
            """));
        using var provider = CreateProvider(handler);

        var exception = await Assert.ThrowsAsync<EconomicDataException>(() =>
            provider.GetLatestObservationAsync("CPIAUCSL"));

        Assert.Equal(EconomicDataErrorCodes.InvalidResponse, exception.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, EconomicDataErrorCodes.AuthenticationFailed, false)]
    [InlineData(HttpStatusCode.BadRequest, EconomicDataErrorCodes.InvalidRequest, false)]
    [InlineData(HttpStatusCode.Locked, EconomicDataErrorCodes.RateLimited, true)]
    [InlineData(HttpStatusCode.TooManyRequests, EconomicDataErrorCodes.RateLimited, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, EconomicDataErrorCodes.ProviderUnavailable, true)]
    public async Task Maps_provider_failures_to_safe_stable_errors(
        HttpStatusCode status,
        string expectedCode,
        bool transient)
    {
        var response = new HttpResponseMessage(status);
        if (status == HttpStatusCode.TooManyRequests)
        {
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(12));
        }

        using var provider = CreateProvider(new StubHandler(_ => response));

        var exception = await Assert.ThrowsAsync<EconomicDataException>(() =>
            provider.GetSeriesMetadataAsync("CPIAUCSL"));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(transient, exception.IsTransient);
        Assert.DoesNotContain("unit-test-fred-key", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Maps_timeout_without_leaking_the_request_or_key()
    {
        var handler = new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Json("{}");
        });
        using var provider = CreateProvider(handler, timeoutSeconds: 1);

        var exception = await Assert.ThrowsAsync<EconomicDataException>(() =>
            provider.GetSeriesMetadataAsync("CPIAUCSL"));

        Assert.Equal(EconomicDataErrorCodes.Timeout, exception.Code);
        Assert.True(exception.IsTransient);
        Assert.DoesNotContain("unit-test-fred-key", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Status_is_configuration_only_and_spends_no_provider_request()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("Must not send a request"));
        using var provider = CreateProvider(handler);

        var status = await provider.GetStatusAsync();

        Assert.Equal(EconomicProviderState.Available, status.State);
        Assert.Empty(handler.Requests);
    }

    private static FredEconomicDataProvider CreateProvider(
        StubHandler handler,
        int timeoutSeconds = 5) =>
        new(
            new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan },
            new EconomicDataOptions
            {
                Enabled = true,
                Provider = "FRED",
                ApiKey = "unit-test-fred-key",
                BaseUrl = "https://fred.test/fred/",
                TimeoutSeconds = timeoutSeconds,
                RateLimitPerMinute = 0
            },
            new FixedTimeProvider(Now));

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) :
            this((request, _) => Task.FromResult(handler(request)))
        {
        }

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return _handler(request, cancellationToken);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
