using XauAi.Application.AI;

namespace XauAi.Application.TargetAnalysis;

internal sealed class TargetWorkspaceRunner(
    TargetWorkspaceCatalog catalog,
    ITargetAiProviderFactory providerFactory,
    ITargetAiResponseValidator responseValidator,
    IScopedAiProviderRequestGate requestGate,
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
                "Target",
                configuration.Provider,
                configuration.BaseUrl,
                configuration.ApiKey,
                configuration.RequestsPerMinute,
                cancellationToken);
            var completion = await providerFactory.Create(configuration.Adapter)
                .AnalyzeAsync(request, configuration, cancellationToken);
            var effectiveConfiguration = string.IsNullOrWhiteSpace(completion.Model)
                ? configuration
                : configuration with { Model = completion.Model };
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
                effectiveConfiguration,
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
