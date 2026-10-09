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

        var models = ModelCandidates(configuration);
        string outputJson = "{}";
        for (var modelIndex = 0; modelIndex < models.Count; modelIndex++)
        {
            var attemptConfiguration = configuration with
            {
                Model = models[modelIndex],
                FallbackModels = []
            };
            outputJson = "{}";
            try
            {
                await requestGate.WaitAsync(
                    "Target",
                    attemptConfiguration.Provider,
                    attemptConfiguration.BaseUrl,
                    attemptConfiguration.ApiKey,
                    attemptConfiguration.RequestsPerMinute,
                    cancellationToken);
                var completion = await providerFactory.Create(attemptConfiguration.Adapter)
                    .AnalyzeAsync(request, attemptConfiguration, cancellationToken);
                var effectiveConfiguration = configuration with
                {
                    Model = string.IsNullOrWhiteSpace(completion.Model)
                        ? attemptConfiguration.Model
                        : completion.Model
                };
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
                if (CanRecoverWithAnotherModel(exception.Code) && modelIndex < models.Count - 1)
                {
                    continue;
                }

                var failedConfiguration = configuration with { Model = attemptConfiguration.Model };
                var message = CanRecoverWithAnotherModel(exception.Code) && models.Count > 1
                    ? $"{exception.SafeMessage} Automatic recovery exhausted {models.Count} configured models."
                    : exception.SafeMessage;
                return Failure(
                    id,
                    request.Workspace,
                    TargetWorkspaceExecutionStatus.Failed,
                    failedConfiguration,
                    createdAt,
                    exception.Code,
                    message,
                    outputJson);
            }
        }

        throw new InvalidOperationException("At least one Target AI model must be configured.");
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

    private static IReadOnlyList<string> ModelCandidates(TargetWorkspaceConfiguration configuration) =>
        [.. new[] { configuration.Model }
            .Concat(configuration.FallbackModels)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static bool CanRecoverWithAnotherModel(string errorCode) => errorCode is
        TargetAnalysisErrorCodes.RateLimited
        or TargetAnalysisErrorCodes.Timeout
        or TargetAnalysisErrorCodes.TokenLimit
        or TargetAnalysisErrorCodes.InvalidResponse
        or TargetAnalysisErrorCodes.Unavailable;
}
