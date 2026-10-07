using System.Collections.Concurrent;

namespace XauAi.Application.TargetAnalysis;

internal sealed class TargetWorkspaceRunner(
    TargetWorkspaceCatalog catalog,
    ITargetAiProviderFactory providerFactory,
    ITargetAiResponseValidator responseValidator,
    ITargetAiRequestGate requestGate,
    TimeProvider timeProvider) : ITargetWorkspaceRunner
{
    public Task<TargetWorkspaceRunResult> RunSpecialistAsync(
        TargetAiRequest request,
        CancellationToken cancellationToken = default) =>
        RunAsync(request, isMaster: false, cancellationToken);

    public Task<TargetWorkspaceRunResult> RunMasterAsync(
        TargetAiRequest request,
        CancellationToken cancellationToken = default) =>
        RunAsync(request, isMaster: true, cancellationToken);

    private async Task<TargetWorkspaceRunResult> RunAsync(
        TargetAiRequest request,
        bool isMaster,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var createdAt = timeProvider.GetUtcNow().ToUniversalTime();
        var configuration = catalog.Get(request.Workspace);
        if (!configuration.Enabled)
        {
            return Failure(
                id,
                request.Workspace,
                TargetWorkspaceExecutionStatus.Disabled,
                configuration,
                createdAt,
                TargetAnalysisErrorCodes.WorkspaceDisabled,
                $"The {request.Workspace} target AI workspace is disabled.");
        }

        string outputJson = "{}";
        try
        {
            await requestGate.WaitAsync(
                request.Workspace,
                configuration.RequestsPerMinute,
                cancellationToken);
            var completion = await providerFactory.Create(configuration.Adapter)
                .AnalyzeAsync(request, configuration, cancellationToken);
            outputJson = completion.Json;
            var completedAt = timeProvider.GetUtcNow().ToUniversalTime();
            var specialist = isMaster
                ? null
                : responseValidator.ValidateSpecialist(
                    outputJson,
                    request.Workspace,
                    request.EvidenceIds);
            var master = isMaster
                ? responseValidator.ValidateMaster(outputJson, request.EvidenceIds)
                : null;
            return new TargetWorkspaceRunResult(
                id,
                request.Workspace,
                TargetWorkspaceExecutionStatus.Completed,
                specialist,
                master,
                outputJson,
                configuration,
                completion.InputTokens,
                completion.OutputTokens,
                completion.LatencyMilliseconds,
                null,
                null,
                createdAt,
                completedAt);
        }
        catch (TargetAnalysisException exception)
        {
            return Failure(
                id,
                request.Workspace,
                TargetWorkspaceExecutionStatus.Failed,
                configuration,
                createdAt,
                exception.Code,
                exception.SafeMessage,
                outputJson);
        }
    }

    private TargetWorkspaceRunResult Failure(
        Guid id,
        TargetWorkspace workspace,
        TargetWorkspaceExecutionStatus status,
        TargetWorkspaceConfiguration configuration,
        DateTimeOffset createdAt,
        string code,
        string message,
        string outputJson = "{}") => new(
            id,
            workspace,
            status,
            null,
            null,
            outputJson,
            configuration,
            null,
            null,
            null,
            code,
            message,
            createdAt,
            timeProvider.GetUtcNow().ToUniversalTime());
}

internal sealed class TargetAiRequestGate(TimeProvider timeProvider) : ITargetAiRequestGate
{
    private readonly ConcurrentDictionary<TargetWorkspace, GateState> states = new();

    public async Task WaitAsync(
        TargetWorkspace workspace,
        int requestsPerMinute,
        CancellationToken cancellationToken = default)
    {
        if (requestsPerMinute <= 0)
        {
            return;
        }

        var state = states.GetOrAdd(workspace, static _ => new GateState());
        await state.Lock.WaitAsync(cancellationToken);
        try
        {
            var interval = TimeSpan.FromMinutes(1d / requestsPerMinute);
            var wait = state.LastRequestUtc + interval - timeProvider.GetUtcNow();
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, timeProvider, cancellationToken);
            }

            state.LastRequestUtc = timeProvider.GetUtcNow();
        }
        finally
        {
            state.Lock.Release();
        }
    }

    private sealed class GateState
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public DateTimeOffset LastRequestUtc { get; set; } = DateTimeOffset.MinValue;
    }
}
