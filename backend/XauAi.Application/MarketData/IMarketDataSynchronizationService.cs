namespace XauAi.Application.MarketData;

public interface IMarketDataSynchronizationService
{
    Task<MarketDataPipelineResult> SynchronizeAsync(
        MarketDataSynchronizationRequest request,
        CancellationToken cancellationToken = default);
}
