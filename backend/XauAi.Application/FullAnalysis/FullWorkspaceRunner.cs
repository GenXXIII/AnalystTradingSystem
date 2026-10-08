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

        string outputJson = "{}";
        try
        {
            await requestGate.WaitAsync(
                "Future",
                configuration.Provider,
                configuration.BaseUrl,
                configuration.ApiKey,
                configuration.RequestsPerMinute,
                cancellationToken);
            var completion = await providerFactory.Create(configuration.Adapter)
                .AnalyzeAsync(request, configuration, cancellationToken);
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
                configuration,
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
            return Failure(
                id,
                request.Workspace,
                FullWorkspaceExecutionStatus.Failed,
                configuration,
                inputHash,
                createdAt,
                exception.Code,
                exception.SafeMessage,
                outputJson);
        }
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

    private sealed record CachedResult(FullWorkspaceRunResult Result, DateTimeOffset StoredAtUtc);
}
