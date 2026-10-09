using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using XauAi.Application.AI;

namespace XauAi.Application.FullAnalysis;

internal sealed class FullWorkspaceRunner(
    FullWorkspaceCatalog catalog,
    IFullAiProviderFactory providerFactory,
    IFullAiResponseValidator responseValidator,
    IScopedAiProviderRequestGate requestGate,
    FullAnalystSettings settings,
    TimeProvider timeProvider) : IFullWorkspaceRunner
{
    private readonly ConcurrentDictionary<string, CachedResult> cache = new(StringComparer.Ordinal);

    public Task<FullWorkspaceRunResult> RunSpecialistAsync(
        FullAiRequest request,
        CancellationToken cancellationToken = default) =>
        RunAsync(request, isMaster: false, cancellationToken);

    public Task<FullWorkspaceRunResult> RunMasterAsync(
        FullAiRequest request,
        CancellationToken cancellationToken = default) =>
        RunAsync(request, isMaster: true, cancellationToken);

    private async Task<FullWorkspaceRunResult> RunAsync(
        FullAiRequest request,
        bool isMaster,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var createdAt = timeProvider.GetUtcNow().ToUniversalTime();
        var configuration = catalog.Get(request.Workspace);
        var inputHash = Hash($"{request.Workspace}|{configuration.ConfigurationVersion}|{configuration.PromptVersion}|{request.StateHash}");
        if (!configuration.Enabled)
        {
            return Failure(
                id,
                request.Workspace,
                FullWorkspaceExecutionStatus.Disabled,
                configuration,
                inputHash,
                createdAt,
                FullAnalysisErrorCodes.WorkspaceDisabled,
                $"The {request.Workspace} Full AI workspace is disabled.");
        }

        if (cache.TryGetValue(inputHash, out var cached)
            && createdAt - cached.StoredAtUtc <= TimeSpan.FromMinutes(settings.CacheMinutes))
        {
            return cached.Result with
            {
                Id = id,
                Status = FullWorkspaceExecutionStatus.Cached,
                CacheHit = true,
                InputTokens = 0,
                OutputTokens = 0,
                LatencyMilliseconds = 0,
                CreatedAtUtc = createdAt,
                CompletedAtUtc = createdAt
            };
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
                    "Future",
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
                    : responseValidator.ValidateSpecialist(outputJson, request.Workspace, request.EvidenceIds);
                var master = isMaster
                    ? responseValidator.ValidateMaster(outputJson, request.EvidenceIds)
                    : null;
                var result = new FullWorkspaceRunResult(
                    id,
                    request.Workspace,
                    FullWorkspaceExecutionStatus.Completed,
                    specialist,
                    master,
                    outputJson,
                    effectiveConfiguration,
                    inputHash,
                    false,
                    completion.InputTokens,
                    completion.OutputTokens,
                    completion.LatencyMilliseconds,
                    null,
                    null,
                    createdAt,
                    completedAt);
                cache[inputHash] = new CachedResult(result, completedAt);
                return result;
            }
            catch (FullAnalysisException exception)
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
                    FullWorkspaceExecutionStatus.Failed,
                    failedConfiguration,
                    inputHash,
                    createdAt,
                    exception.Code,
                    message,
                    outputJson);
            }
        }

        throw new InvalidOperationException("At least one Full AI model must be configured.");
    }

    private FullWorkspaceRunResult Failure(
        Guid id,
        FullWorkspace workspace,
        FullWorkspaceExecutionStatus status,
        FullWorkspaceConfiguration configuration,
        string inputHash,
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
            inputHash,
            false,
            null,
            null,
            null,
            code,
            message,
            createdAt,
            timeProvider.GetUtcNow().ToUniversalTime());

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static IReadOnlyList<string> ModelCandidates(FullWorkspaceConfiguration configuration) =>
        [.. new[] { configuration.Model }
            .Concat(configuration.FallbackModels)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static bool CanRecoverWithAnotherModel(string errorCode) => errorCode is
        FullAnalysisErrorCodes.RateLimited
        or FullAnalysisErrorCodes.Timeout
        or FullAnalysisErrorCodes.TokenLimit
        or FullAnalysisErrorCodes.InvalidResponse
        or FullAnalysisErrorCodes.Unavailable;

    private sealed record CachedResult(FullWorkspaceRunResult Result, DateTimeOffset StoredAtUtc);
}
