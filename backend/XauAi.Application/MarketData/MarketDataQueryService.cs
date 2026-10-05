namespace XauAi.Application.MarketData;

internal sealed class MarketDataQueryService(
    IMarketDataQueryStore queryStore,
    IMarketDataSyncStateStore stateStore,
    IMarketSessionCalendar sessionCalendar,
    MarketDataPipelineSettings settings) : IMarketDataQueryService
{
    public async Task<MarketDataQueryResult> GetRangeAsync(
        MarketDataQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var normalized = query with
        {
            Symbol = settings.Symbol,
            FromUtc = query.FromUtc.ToUniversalTime(),
            ToUtc = query.ToUtc.ToUniversalTime()
        };
        var candles = await queryStore.GetRangeAsync(normalized, cancellationToken);
        return new MarketDataQueryResult(
            normalized.Symbol,
            normalized.Timeframe,
            normalized.FromUtc,
            normalized.ToUtc,
            normalized.Limit,
            normalized.CompletedOnly,
            candles);
    }

    public Task<IReadOnlyList<StoredMarketCandle>> GetLatestAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        bool completedOnly,
        CancellationToken cancellationToken = default)
    {
        ValidateSymbolAndTimeframe(symbol, timeframe);
        ValidateLimit(limit);
        return queryStore.GetLatestAsync(settings.Symbol, timeframe, limit, completedOnly, cancellationToken);
    }

    public async Task<StoredMarketCandle?> GetLastCompletedAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        var candles = await GetLatestAsync(symbol, timeframe, 1, completedOnly: true, cancellationToken);
        return candles.SingleOrDefault();
    }

    public async Task<MarketDataPipelineStatus> GetStatusAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        ValidateSymbolAndTimeframe(symbol, timeframe);
        var availability = await queryStore.GetAvailabilityAsync(settings.Symbol, timeframe, cancellationToken);
        var synchronization = await stateStore.GetStatusAsync(settings.Symbol, timeframe, cancellationToken);
        return new MarketDataPipelineStatus(availability, synchronization);
    }

    public async Task<IReadOnlyList<MarketDataGap>> GetGapsAsync(
        MarketDataQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var openTimes = await queryStore.GetOpenTimesAsync(
            settings.Symbol,
            query.Timeframe,
            query.FromUtc.ToUniversalTime(),
            query.ToUtc.ToUniversalTime(),
            cancellationToken);
        return MarketDataGapDetector.Detect(
            settings.Symbol,
            query.Timeframe,
            openTimes,
            sessionCalendar,
            Math.Min(query.Limit, settings.MaxGapResults));
    }

    private void ValidateQuery(MarketDataQuery query)
    {
        ValidateSymbolAndTimeframe(query.Symbol, query.Timeframe);
        MarketDataRequestValidation.Validate(query.Symbol, query.FromUtc, query.ToUtc);
        ValidateLimit(query.Limit);

        if (query.ToUtc.ToUniversalTime() - query.FromUtc.ToUniversalTime()
            > TimeSpan.FromDays(settings.MaxQueryRangeDays))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.QueryRangeTooLarge,
                $"The market-data query range cannot exceed {settings.MaxQueryRangeDays} days.");
        }
    }

    private void ValidateSymbolAndTimeframe(string symbol, MarketTimeframe timeframe)
    {
        if (!string.Equals(symbol, settings.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.ProviderSymbolNotFound,
                "The requested market symbol is not configured.");
        }

        if (!settings.Timeframes.Contains(timeframe))
        {
            throw new MarketDataException(
                MarketDataErrorCodes.InvalidRequest,
                "The requested timeframe is not enabled.");
        }
    }

    private void ValidateLimit(int limit)
    {
        if (limit is < 1 || limit > 100000 || limit > settings.MaxApiLimit)
        {
            throw new MarketDataException(
                MarketDataErrorCodes.QueryLimitExceeded,
                $"The market-data limit must be between 1 and {settings.MaxApiLimit}.");
        }
    }
}
