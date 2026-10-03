using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using XauAi.Application.MarketData;
using XauAi.Infrastructure.MarketData.Persistence;
using XauAi.Infrastructure.Persistence;

namespace XauAi.UnitTests.MarketData;

public sealed class EfMarketCandleStoreTests
{
    [Fact]
    public async Task Repeated_complete_candle_is_idempotent()
    {
        await using var context = await CreateContextAsync();
        var store = new EfMarketCandleStore(context, NullLogger<EfMarketCandleStore>.Instance);
        var candle = CreateCandle(MarketTimeframe.H1, isComplete: true);

        var first = await store.SaveAsync([candle]);
        var second = await store.SaveAsync([candle]);

        Assert.Equal(1, first.Inserted);
        Assert.Equal(1, second.Skipped);
        Assert.Equal(1, await context.MarketCandles.CountAsync());
    }

    [Fact]
    public async Task Incomplete_candle_can_be_updated_once_it_changes()
    {
        await using var context = await CreateContextAsync();
        var store = new EfMarketCandleStore(context, NullLogger<EfMarketCandleStore>.Instance);
        var incomplete = CreateCandle(MarketTimeframe.M5, isComplete: false);
        await store.SaveAsync([incomplete]);
        var completed = incomplete with { Close = 2346m, IsComplete = true };

        var result = await store.SaveAsync([completed]);

        Assert.Equal(1, result.Updated);
        var stored = await context.MarketCandles.SingleAsync();
        Assert.True(stored.IsComplete);
        Assert.Equal(2346m, stored.Close);
    }

    [Fact]
    public async Task Multiple_timeframes_are_stored_independently()
    {
        await using var context = await CreateContextAsync();
        var store = new EfMarketCandleStore(context, NullLogger<EfMarketCandleStore>.Instance);

        await store.SaveAsync([CreateCandle(MarketTimeframe.M1, isComplete: true)]);
        await store.SaveAsync([CreateCandle(MarketTimeframe.H4, isComplete: true)]);

        Assert.Equal(2, await context.MarketCandles.CountAsync());
    }

    private static async Task<XauAiDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<XauAiDbContext>()
            .UseInMemoryDatabase($"mt5-store-{Guid.NewGuid():N}")
            .Options;
        var context = new XauAiDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static MarketCandleSnapshot CreateCandle(
        MarketTimeframe timeframe,
        bool isComplete)
    {
        var openTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return new MarketCandleSnapshot(
            "XAUUSD",
            "XAUUSD.test",
            timeframe,
            openTime,
            openTime.Add(timeframe.Duration()),
            2345m,
            2347m,
            2344m,
            2345.5m,
            100m,
            0m,
            25m,
            isComplete,
            "UTC",
            openTime.AddDays(1));
    }
}
