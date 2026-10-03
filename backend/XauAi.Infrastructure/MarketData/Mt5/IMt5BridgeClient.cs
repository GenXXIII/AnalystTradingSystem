namespace XauAi.Infrastructure.MarketData.Mt5;

internal interface IMt5BridgeClient
{
    Task<Mt5BridgeResponse> ExecuteAsync(
        Mt5BridgeRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class Mt5BridgeException(
    string code,
    string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public string Code { get; } = code;
}
