using XauAi.Application.MarketData;

namespace XauAi.UnitTests.MarketData;

public sealed class MarketDataIngestionServiceTests
{
    [Fact]
    public async Task Phase_four_contract_delegates_to_the_phase_five_pipeline()
    {
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var pipeline = new FakeSynchronizationService(from);
        var service = new MarketDataIngestionService(pipeline);

        var result = await service.SyncAsync("XAUUSD", MarketTimeframe.H1, from, from.AddHours(4));

        Assert.NotNull(pipeline.Request);
        Assert.Equal(from, pipeline.Request.FromUtc);
        Assert.True(pipeline.Request.IncludeFormingCandle);
        Assert.Equal(4, result.Retrieved);
        Assert.Equal(3, result.Inserted);
    }

    private sealed class FakeSynchronizationService(DateTimeOffset from) : IMarketDataSynchronizationService
    {
        public MarketDataSynchronizationRequest? Request { get; private set; }

        public Task<MarketDataPipelineResult> SynchronizeAsync(
            MarketDataSynchronizationRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new MarketDataPipelineResult(
                Guid.NewGuid(),
                request.Symbol,
                request.Timeframe,
                from,
                from.AddHours(4),
                from.AddHours(3),
                1,
                4,
                4,
                3,
                0,
                1,
                0,
                0,
                10));
        }
    }
}
