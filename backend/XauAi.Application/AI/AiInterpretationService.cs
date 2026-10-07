using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using XauAi.Application.Evidence;

namespace XauAi.Application.AI;

internal sealed class AiInterpretationService(
    IAiInterpretationStore store,
    IAiEvidenceSelector selector,
    IAiEvidenceCompressor compressor,
    IAiResponseValidator responseValidator,
    IAiSpecialistRunner specialistRunner,
    IAiInterpretationExecutionGate executionGate,
    AiSpecialistCatalog specialistCatalog,
    AiInterpretationSettings settings,
    TimeProvider timeProvider,
    ILogger<AiInterpretationService> logger) : IAiInterpretationService
{
    public async Task<AiInterpretationResult> InterpretAsync(
        CreateAiInterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var instrument = EvidenceNormalization.NormalizeInstrument(request.Instrument)
            ?? throw Invalid("The AI interpretation instrument must be a supported XAUUSD alias.");
        var timeframe = string.IsNullOrWhiteSpace(request.Timeframe)
            ? null
            : EvidenceNormalization.NormalizeTimeframe(request.Timeframe)
                ?? throw Invalid("The AI interpretation timeframe is invalid.");
        var analysisTime = (request.AnalysisTimeUtc ?? now).ToUniversalTime();
        if (analysisTime > now)
        {
            throw Invalid("The AI interpretation analysis time cannot be in the future.");
        }

        var lookbackHours = request.LookbackHours ?? settings.DefaultLookbackHours;
        if (lookbackHours is < 1 || lookbackHours > settings.MaximumLookbackHours)
        {
            throw Invalid($"AI interpretation lookback must be between 1 and {settings.MaximumLookbackHours} hours.");
        }

        var configuration = specialistCatalog.Get(request.Specialist);
        ValidateSpecialistInterpretation(request.Specialist, request.InterpretationType);
        if (!configuration.Enabled)
        {
            throw new AiInterpretationException(
                AiInterpretationErrorCodes.SpecialistDisabled,
                $"The {request.Specialist} AI specialist is disabled.");
        }

        await store.MarkChangedInterpretationsStaleAsync(instrument, now, cancellationToken);
        var selectionQuery = new AiEvidenceSelectionQuery(
            instrument,
            request.Specialist,
            request.InterpretationType,
            timeframe,
            analysisTime.AddHours(-lookbackHours),
            analysisTime,
            settings.MaximumEvidenceItems * 4);
        var candidates = await store.LoadEvidenceAsync(selectionQuery, cancellationToken);
        var selection = selector.Select(candidates, selectionQuery, settings.MaximumEvidenceItems);
        if (selection.Items.Count == 0)
        {
            throw new AiInterpretationException(
                AiInterpretationErrorCodes.NoEvidence,
                "No eligible evidence was available at the requested analysis time.");
        }

        var compressed = compressor.Compress(selection, selectionQuery, settings.MaximumCompressedCharacters);
        var selectedForPrompt = selection.Items
            .Where(item => compressed.EvidenceIds.Contains(item.Id))
            .ToArray();
        var evidenceVersion = Hash(string.Join('|', selectedForPrompt
            .OrderBy(item => item.Id)
            .Select(item => $"{item.Id:D}:{item.ContentHash}:{item.UpdatedAtUtc:O}")));
        var evidenceUpdatedAt = selectedForPrompt.Max(item => item.UpdatedAtUtc);
        var inputDigest = Hash(compressed.Json);
        var configurationVersion = CreateConfigurationVersion(configuration, settings);
        var cacheAnalysisTime = request.AnalysisTimeUtc.HasValue
            ? analysisTime
            : FloorToWindow(analysisTime, TimeSpan.FromMinutes(settings.CurrentContextCacheMinutes));
        var cacheKey = Hash(string.Join('|',
            instrument,
            timeframe ?? "ALL",
            request.Specialist,
            request.InterpretationType,
            settings.PromptVersion,
            configuration.Provider,
            configuration.Adapter,
            configuration.Model,
            configurationVersion,
            cacheAnalysisTime.ToString("O", CultureInfo.InvariantCulture),
            evidenceVersion,
            string.Join(',', compressed.EvidenceIds.Order())));

        var cached = await store.FindCurrentByCacheKeyAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation(
                "AI interpretation cache hit for {Specialist} {InterpretationType} at {AnalysisTimeUtc} with {EvidenceCount} evidence records",
                request.Specialist,
                request.InterpretationType,
                analysisTime,
                compressed.SelectedCount);
            return cached with { CacheHit = true };
        }

        await using var executionLease = await executionGate.AcquireAsync(cacheKey, cancellationToken);
        cached = await store.FindCurrentByCacheKeyAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation(
                "AI interpretation coalesced to a cache hit for {Specialist} {InterpretationType} at {AnalysisTimeUtc}",
                request.Specialist,
                request.InterpretationType,
                analysisTime);
            return cached with { CacheHit = true };
        }

        var providerRequest = new AiProviderRequest(
            request.Specialist,
            request.InterpretationType,
            instrument,
            timeframe,
            analysisTime,
            settings.PromptVersion,
            compressed.Json,
            compressed.EvidenceIds);
        var id = Guid.NewGuid();
        var startedAt = timeProvider.GetUtcNow().ToUniversalTime();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var (completion, usedConfiguration) = await specialistRunner.RunAsync(providerRequest, cancellationToken);
            var structured = responseValidator.Validate(
                completion.Json,
                request.InterpretationType,
                selectedForPrompt);
            var completedAt = timeProvider.GetUtcNow().ToUniversalTime();
            var result = await store.SaveCompletedAsync(
                new AiInterpretationWriteModel(
                    id,
                    instrument,
                    timeframe,
                    request.Specialist,
                    structured,
                    usedConfiguration.Provider,
                    usedConfiguration.Model,
                    settings.PromptVersion,
                    configurationVersion,
                    evidenceVersion,
                    cacheKey,
                    inputDigest,
                    completion.Json,
                    analysisTime,
                    evidenceUpdatedAt,
                    startedAt,
                    completedAt,
                    completion.InputTokens,
                    completion.OutputTokens,
                    completion.LatencyMilliseconds,
                    compressed.EvidenceIds),
                cancellationToken);
            logger.LogInformation(
                "AI interpretation completed for {Specialist} using {Provider}/{Model} at {AnalysisTimeUtc}: selected {SelectedEvidenceCount} of {EvidenceCount}, cache hit {CacheHit}, input tokens {InputTokens}, output tokens {OutputTokens}, duration {DurationMilliseconds} ms, status {Status}",
                request.Specialist,
                usedConfiguration.Provider,
                usedConfiguration.Model,
                analysisTime,
                compressed.SelectedCount,
                compressed.OriginalCount,
                false,
                completion.InputTokens,
                completion.OutputTokens,
                completion.LatencyMilliseconds,
                result.Status);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AiProviderException exception)
        {
            stopwatch.Stop();
            var completedAt = timeProvider.GetUtcNow().ToUniversalTime();
            var failure = await store.SaveFailureAsync(
                new AiInterpretationFailureWriteModel(
                    id,
                    instrument,
                    timeframe,
                    request.Specialist,
                    request.InterpretationType,
                    configuration.Provider,
                    configuration.Model,
                    settings.PromptVersion,
                    configurationVersion,
                    evidenceVersion,
                    cacheKey,
                    inputDigest,
                    analysisTime,
                    evidenceUpdatedAt,
                    startedAt,
                    completedAt,
                    checked((int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue)),
                    exception.Code,
                    exception.SafeMessage,
                    compressed.EvidenceIds),
                cancellationToken);
            logger.LogWarning(
                "AI interpretation failed for {Specialist} using {Provider}/{Model} at {AnalysisTimeUtc} with {EvidenceCount} evidence records after {DurationMilliseconds} ms: {FailureReason}",
                request.Specialist,
                configuration.Provider,
                configuration.Model,
                analysisTime,
                compressed.SelectedCount,
                stopwatch.ElapsedMilliseconds,
                exception.Code);
            return failure;
        }
    }

    public async Task<AiInterpretationResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await store.MarkChangedInterpretationsStaleAsync(
            null,
            timeProvider.GetUtcNow().ToUniversalTime(),
            cancellationToken);
        return await store.GetAsync(id, cancellationToken)
            ?? throw new AiInterpretationException(
                AiInterpretationErrorCodes.NotFound,
                "The requested AI interpretation was not found.");
    }

    public async Task<AiInterpretationResult?> GetLatestAsync(
        string instrument,
        AiSpecialist? specialist,
        AiInterpretationType? interpretationType,
        string? timeframe,
        CancellationToken cancellationToken = default)
    {
        var normalizedInstrument = EvidenceNormalization.NormalizeInstrument(instrument)
            ?? throw Invalid("The AI interpretation instrument must be a supported XAUUSD alias.");
        var normalizedTimeframe = string.IsNullOrWhiteSpace(timeframe)
            ? null
            : EvidenceNormalization.NormalizeTimeframe(timeframe)
                ?? throw Invalid("The AI interpretation timeframe is invalid.");
        await store.MarkChangedInterpretationsStaleAsync(
            normalizedInstrument,
            timeProvider.GetUtcNow().ToUniversalTime(),
            cancellationToken);
        return await store.GetLatestAsync(
            normalizedInstrument,
            specialist,
            interpretationType,
            normalizedTimeframe,
            cancellationToken);
    }

    public async Task<PagedAiInterpretations> QueryAsync(
        AiInterpretationQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.Page, query.PageSize);
        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc >= query.ToUtc)
        {
            throw Invalid("AI interpretation query start must be before the end.");
        }

        var instrument = string.IsNullOrWhiteSpace(query.Instrument)
            ? null
            : EvidenceNormalization.NormalizeInstrument(query.Instrument)
                ?? throw Invalid("The AI interpretation instrument filter must be a supported XAUUSD alias.");
        var timeframe = string.IsNullOrWhiteSpace(query.Timeframe)
            ? null
            : EvidenceNormalization.NormalizeTimeframe(query.Timeframe)
                ?? throw Invalid("The AI interpretation timeframe filter is invalid.");
        await store.MarkChangedInterpretationsStaleAsync(
            instrument,
            timeProvider.GetUtcNow().ToUniversalTime(),
            cancellationToken);
        return await store.QueryAsync(query with
        {
            Instrument = instrument,
            Timeframe = timeframe,
            FromUtc = query.FromUtc?.ToUniversalTime(),
            ToUtc = query.ToUtc?.ToUniversalTime()
        }, cancellationToken);
    }

    public async Task<PagedAiInterpretations> GetByEvidenceAsync(
        Guid evidenceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(page, pageSize);
        await store.MarkChangedInterpretationsStaleAsync(
            null,
            timeProvider.GetUtcNow().ToUniversalTime(),
            cancellationToken);
        return await store.GetByEvidenceAsync(evidenceId, page, pageSize, cancellationToken);
    }

    private void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 || pageSize > settings.MaximumPageSize)
        {
            throw Invalid($"AI interpretation paging requires page >= 1 and pageSize between 1 and {settings.MaximumPageSize}.");
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string CreateConfigurationVersion(
        AiSpecialistConfiguration configuration,
        AiInterpretationSettings settings) =>
        Hash(string.Join('|',
            "phase11",
            configuration.Specialist,
            configuration.Enabled,
            configuration.Provider.Trim(),
            configuration.Adapter.Trim(),
            configuration.RequiresApiKey,
            configuration.Model.Trim(),
            configuration.BaseUrl.Trim().TrimEnd('/'),
            configuration.Temperature.ToString("R", CultureInfo.InvariantCulture),
            configuration.TimeoutSeconds,
            configuration.MaxOutputTokens,
            configuration.MaxRetries,
            configuration.RequestsPerMinute,
            settings.PromptVersion,
            settings.DefaultLookbackHours,
            settings.MaximumLookbackHours,
            settings.MaximumEvidenceItems,
            settings.MaximumCompressedCharacters,
            settings.CurrentContextCacheMinutes,
            settings.MaximumPageSize));

    private static DateTimeOffset FloorToWindow(DateTimeOffset value, TimeSpan window)
    {
        var utc = value.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - utc.Ticks % window.Ticks, TimeSpan.Zero);
    }

    private static void ValidateSpecialistInterpretation(
        AiSpecialist specialist,
        AiInterpretationType interpretationType)
    {
        var valid = specialist switch
        {
            AiSpecialist.News => interpretationType is AiInterpretationType.NewsEvent
                or AiInterpretationType.MacroData
                or AiInterpretationType.AnalystClaim
                or AiInterpretationType.GeopoliticalRisk,
            AiSpecialist.Candle or AiSpecialist.Structure or AiSpecialist.Liquidity
                or AiSpecialist.Flow or AiSpecialist.Ktr =>
                interpretationType == AiInterpretationType.TechnicalEvidence,
            AiSpecialist.Risk => interpretationType is AiInterpretationType.GeopoliticalRisk
                or AiInterpretationType.EvidenceSynthesis,
            AiSpecialist.Master => interpretationType == AiInterpretationType.EvidenceSynthesis,
            _ => false
        };
        if (!valid)
        {
            throw Invalid($"The {specialist} specialist does not support {interpretationType} interpretations.");
        }
    }

    private static AiInterpretationException Invalid(string message) =>
        new(AiInterpretationErrorCodes.InvalidRequest, message);
}
