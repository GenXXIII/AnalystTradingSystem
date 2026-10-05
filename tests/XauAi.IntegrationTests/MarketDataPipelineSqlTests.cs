using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XauAi.Application;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;
using XauAi.Infrastructure;
using XauAi.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace XauAi.IntegrationTests;

public sealed class MarketDataPipelineSqlTests(ITestOutputHelper output)
{
    private static readonly DateTimeOffset StartUtc = new(2025, 1, 6, 0, 0, 0, TimeSpan.Zero);

    [SqlServerFact]
    public async Task Pipeline_persists_idempotent_incremental_multi_timeframe_data_and_recovers_after_failure_and_restart()
    {
        await using var fixture = await PipelineFixture.CreateAsync(batchSize: 4);

        var firstM5 = await fixture.SynchronizeAsync(MarketTimeframe.M5, StartUtc, StartUtc.AddMinutes(30));
        var repeatedM5 = await fixture.SynchronizeAsync(MarketTimeframe.M5, StartUtc, StartUtc.AddMinutes(30));
        var incrementalM5 = await fixture.SynchronizeAsync(MarketTimeframe.M5, null, StartUtc.AddMinutes(40));
        var firstH1 = await fixture.SynchronizeAsync(MarketTimeframe.H1, StartUtc, StartUtc.AddHours(2));

        Assert.Equal(7, firstM5.Inserted);
        Assert.Equal(0, repeatedM5.Inserted);
        Assert.Equal(7, repeatedM5.Skipped);
        Assert.Equal(2, incrementalM5.Inserted);
        Assert.Equal(StartUtc.AddMinutes(35), incrementalM5.RequestedFromUtc);
        Assert.Equal(3, firstH1.Inserted);

        fixture.Provider.FailTimeframe = MarketTimeframe.M15;
        fixture.Provider.FailOnRequestNumber = 2;
        var failure = await Assert.ThrowsAsync<MarketDataException>(() =>
            fixture.SynchronizeAsync(MarketTimeframe.M15, StartUtc, StartUtc.AddHours(2)));
        Assert.Equal(MarketDataErrorCodes.ProviderAuthenticationFailed, failure.Code);

        await using (var failureScope = fixture.Services.CreateAsyncScope())
        {
            var context = failureScope.ServiceProvider.GetRequiredService<XauAiDbContext>();
            Assert.Equal(4, await CountAsync(context, "M15"));
            var m15TimeframeId = await TimeframeIdAsync(context, "M15");
            var failedState = await context.MarketDataSyncStates
                .SingleAsync(state => state.TimeframeId == m15TimeframeId);
            Assert.Equal("Failed", failedState.Status);
            Assert.Equal(1, failedState.ConsecutiveFailures);
        }

        fixture.Provider.ClearFailure();
        var recoveredM15 = await fixture.SynchronizeAsync(MarketTimeframe.M15, StartUtc, StartUtc.AddHours(2));
        Assert.Equal(5, recoveredM15.Inserted);

        Guid interruptedRunId;
        await using (var interruptedScope = fixture.Services.CreateAsyncScope())
        {
            var stateStore = interruptedScope.ServiceProvider.GetRequiredService<IMarketDataSyncStateStore>();
            interruptedRunId = await stateStore.StartRunAsync(
                "XAUUSD",
                MarketTimeframe.M30,
                StartUtc,
                StartUtc.AddHours(2),
                DateTimeOffset.UtcNow.AddMinutes(-1));
        }

        var restartedM30 = await fixture.SynchronizeAsync(MarketTimeframe.M30, StartUtc, StartUtc.AddHours(2));
        Assert.Equal(5, restartedM30.Inserted);

        await using var verificationScope = fixture.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<XauAiDbContext>();
        Assert.Equal(9, await CountAsync(verification, "M5"));
        Assert.Equal(3, await CountAsync(verification, "H1"));
        Assert.Equal(9, await CountAsync(verification, "M15"));
        Assert.Equal(5, await CountAsync(verification, "M30"));
        Assert.Equal("Interrupted", (await verification.MarketDataSyncRuns.FindAsync(interruptedRunId))!.Status);
        Assert.Equal(0, await verification.MarketCandles
            .GroupBy(candle => new { candle.InstrumentId, candle.TimeframeId, candle.DataProviderId, candle.OpenTimeUtc })
            .Where(group => group.Count() > 1)
            .CountAsync());

        var verificationM15Id = await TimeframeIdAsync(verification, "M15");
        var m15State = await verification.MarketDataSyncStates
            .SingleAsync(state => state.TimeframeId == verificationM15Id);
        Assert.Equal("Healthy", m15State.Status);
        Assert.Equal(0, m15State.ConsecutiveFailures);
        Assert.NotNull(m15State.LastSuccessfulSyncAtUtc);

        var analysis = verificationScope.ServiceProvider.GetRequiredService<ITechnicalAnalysisService>();
        var historical = await analysis.AnalyzeAsync(new TechnicalAnalysisRequest(
            "XAUUSD",
            MarketTimeframe.H1,
            StartUtc.AddHours(3)));
        Assert.Equal(3, historical.Diagnostics.CandlesUsed);
        Assert.True(historical.LastCandleCloseTimeUtc <= StartUtc.AddHours(3));
        Assert.All(historical.Indicators.Ema, indicator =>
            Assert.Equal(AnalysisReadiness.InsufficientData, indicator.Readiness));
    }

    [SqlServerFact]
    public async Task Realistic_ten_thousand_candle_dataset_is_measured()
    {
        await using var fixture = await PipelineFixture.CreateAsync(batchSize: 1000, maxApiLimit: 10000);
        var process = Process.GetCurrentProcess();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryBefore = process.WorkingSet64;
        var cpuBefore = process.TotalProcessorTime;
        var syncTimer = Stopwatch.StartNew();

        var result = await fixture.SynchronizeAsync(
            MarketTimeframe.M1,
            StartUtc,
            StartUtc.AddMinutes(9_999));
        syncTimer.Stop();

        var queryTimer = Stopwatch.StartNew();
        await using var scope = fixture.Services.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IMarketDataQueryService>();
        var records = await query.GetRangeAsync(new MarketDataQuery(
            "XAUUSD",
            MarketTimeframe.M1,
            StartUtc,
            StartUtc.AddMinutes(9_999),
            10_000,
            CompletedOnly: true));
        queryTimer.Stop();

        var analysisTimer = Stopwatch.StartNew();
        var analysis = scope.ServiceProvider.GetRequiredService<ITechnicalAnalysisService>();
        var technical = await analysis.AnalyzeAsync(new TechnicalAnalysisRequest(
            "XAUUSD",
            MarketTimeframe.M1,
            StartUtc.AddMinutes(10_000)));
        analysisTimer.Stop();
        process.Refresh();
        var memoryDeltaMb = (process.WorkingSet64 - memoryBefore) / 1024d / 1024d;
        var cpuMilliseconds = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;

        output.WriteLine(
            "10,000 candles: sync={0} ms; query={1} ms; analysis={2} ms; cpu={3:F0} ms; working-set delta={4:F1} MB; batches={5}",
            syncTimer.ElapsedMilliseconds,
            queryTimer.ElapsedMilliseconds,
            analysisTimer.ElapsedMilliseconds,
            cpuMilliseconds,
            memoryDeltaMb,
            result.BatchesProcessed);

        Assert.Equal(10_000, result.Inserted);
        Assert.Equal(10_000, records.Candles.Count);
        Assert.Equal(10, result.BatchesProcessed);
        Assert.Equal(10_000, technical.Diagnostics.CandlesUsed);
        Assert.True(syncTimer.Elapsed < TimeSpan.FromMinutes(2));
        Assert.True(queryTimer.Elapsed < TimeSpan.FromSeconds(10));
        Assert.True(analysisTimer.Elapsed < TimeSpan.FromSeconds(10));
    }

    private static async Task<int> CountAsync(XauAiDbContext context, string timeframe)
    {
        var timeframeId = await TimeframeIdAsync(context, timeframe);
        return await context.MarketCandles.CountAsync(candle => candle.TimeframeId == timeframeId);
    }

    private static Task<Guid> TimeframeIdAsync(XauAiDbContext context, string timeframe) =>
        context.Timeframes.Where(value => value.Code == timeframe).Select(value => value.Id).SingleAsync();

    private sealed class PipelineFixture : IAsyncDisposable
    {
        private readonly DbContextOptions<XauAiDbContext> _cleanupOptions;

        private PipelineFixture(
            ServiceProvider services,
            DeterministicMarketDataProvider provider,
            DbContextOptions<XauAiDbContext> cleanupOptions)
        {
            Services = services;
            Provider = provider;
            _cleanupOptions = cleanupOptions;
        }

        public ServiceProvider Services { get; }

        public DeterministicMarketDataProvider Provider { get; }

        public static async Task<PipelineFixture> CreateAsync(int batchSize, int maxApiLimit = 5000)
        {
            var serverConnection = Environment.GetEnvironmentVariable(SqlServerFactAttribute.EnvironmentVariableName)!;
            var databaseName = $"XauAiPipelineTests_{Guid.NewGuid():N}";
            var connectionBuilder = new SqlConnectionStringBuilder(serverConnection) { InitialCatalog = databaseName };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = "XAUUSD-AI Pipeline Tests",
                ["Application:Version"] = "1.0.0-test",
                ["Api:Cors:AllowedOrigins:0"] = "http://localhost",
                ["Database:Enabled"] = "true",
                ["Database:ConnectionString"] = connectionBuilder.ConnectionString,
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["MarketData:Symbol"] = "XAUUSD",
                ["MarketData:Timeframes"] = "M1,M5,M15,M30,H1,H4,D1",
                ["MarketData:InitialHistoryDays"] = "7",
                ["MarketData:BatchSize"] = batchSize.ToString(),
                ["MarketData:MaxApiLimit"] = maxApiLimit.ToString(),
                ["MarketData:MaxQueryRangeDays"] = "30",
                ["MarketData:MaxRetries"] = "0",
                ["TechnicalAnalysis:Enabled"] = "true",
                ["TechnicalAnalysis:Symbol"] = "XAUUSD",
                ["TechnicalAnalysis:Timeframes"] = "M1,M5,M15,M30,H1,H4,D1",
                ["TechnicalAnalysis:HistoryLimit"] = maxApiLimit.ToString()
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.RemoveAll<IMarketDataProvider>();
            var provider = new DeterministicMarketDataProvider();
            services.AddSingleton<IMarketDataProvider>(provider);
            var serviceProvider = services.BuildServiceProvider();
            var cleanupOptions = new DbContextOptionsBuilder<XauAiDbContext>()
                .UseSqlServer(connectionBuilder.ConnectionString)
                .Options;
            var fixture = new PipelineFixture(serviceProvider, provider, cleanupOptions);
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<XauAiDbContext>();
                await context.Database.MigrateAsync();
                return fixture;
            }
            catch
            {
                await fixture.DisposeAsync();
                throw;
            }
        }

        public async Task<MarketDataPipelineResult> SynchronizeAsync(
            MarketTimeframe timeframe,
            DateTimeOffset? fromUtc,
            DateTimeOffset toUtc)
        {
            await using var scope = Services.CreateAsyncScope();
            var synchronization = scope.ServiceProvider.GetRequiredService<IMarketDataSynchronizationService>();
            return await synchronization.SynchronizeAsync(new MarketDataSynchronizationRequest(
                "XAUUSD", timeframe, fromUtc, toUtc, IncludeFormingCandle: false));
        }

        public async ValueTask DisposeAsync()
        {
            await using (var cleanup = new XauAiDbContext(_cleanupOptions))
            {
                await cleanup.Database.EnsureDeletedAsync();
            }
            await Services.DisposeAsync();
        }
    }

    private sealed class DeterministicMarketDataProvider : IMarketDataProvider
    {
        private readonly Dictionary<MarketTimeframe, int> _requestsByTimeframe = [];

        public MarketTimeframe? FailTimeframe { get; set; }

        public int? FailOnRequestNumber { get; set; }

        public void ClearFailure()
        {
            FailTimeframe = null;
            FailOnRequestNumber = null;
            _requestsByTimeframe.Clear();
        }

        public Task<MarketDataProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<MarketCandleSnapshot>> GetCandlesAsync(
            string symbol,
            MarketTimeframe timeframe,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken = default)
        {
            var requestNumber = _requestsByTimeframe.GetValueOrDefault(timeframe) + 1;
            _requestsByTimeframe[timeframe] = requestNumber;
            if (FailTimeframe == timeframe && FailOnRequestNumber == requestNumber)
            {
                throw new MarketDataException(MarketDataErrorCodes.ProviderAuthenticationFailed, "Provider authentication failed.");
            }

            var candles = new List<MarketCandleSnapshot>();
            for (var cursor = timeframe.AlignDown(fromUtc); cursor <= toUtc; cursor = cursor.Add(timeframe.Duration()))
            {
                if (cursor < fromUtc) continue;
                var offset = (decimal)(cursor - StartUtc).TotalMinutes / 1000m;
                candles.Add(new MarketCandleSnapshot(
                    symbol,
                    "XAUUSD.test",
                    timeframe,
                    cursor,
                    cursor.Add(timeframe.Duration()),
                    2300m + offset,
                    2302m + offset,
                    2299m + offset,
                    2301m + offset,
                    100m,
                    0m,
                    20m,
                    true,
                    "UTC",
                    cursor.AddMinutes(1)));
            }

            return Task.FromResult<IReadOnlyList<MarketCandleSnapshot>>(candles);
        }
    }
}
