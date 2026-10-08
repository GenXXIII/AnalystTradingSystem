using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using XauAi.Application.AI;
using XauAi.Application.Evidence;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.Application.TargetAnalysis;

internal sealed class TargetAnalystService(
    ITargetAnalysisStore store,
    IAiInterpretationStore interpretationStore,
    IAiEvidenceSelector evidenceSelector,
    IAiEvidenceCompressor evidenceCompressor,
    IMarketDataQueryStore marketData,
    ITechnicalAnalysisService technicalAnalysis,
    ILocalAnalystService localAnalyst,
    ITargetWorkspaceRunner workspaceRunner,
    ITargetResultValidator resultValidator,
    IAiProviderAccountStatusService providerAccountStatus,
    TargetWorkspaceCatalog workspaceCatalog,
    TargetAnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<TargetAnalystService> logger) : ITargetAnalystService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly TargetWorkspace[] SpecialistWorkspaces =
    [
        TargetWorkspace.Structure,
        TargetWorkspace.Liquidity,
        TargetWorkspace.Candle,
        TargetWorkspace.Flow,
        TargetWorkspace.Ktr,
        TargetWorkspace.News
    ];

    public async Task<TargetAnalysisResult> AnalyzeAsync(
        CreateTargetAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var symbol = EvidenceNormalization.NormalizeInstrument(request.Symbol)
            ?? throw Invalid("The target-analysis symbol must be a supported XAUUSD alias.");
        if (!string.Equals(symbol, settings.Symbol, StringComparison.Ordinal))
        {
            throw Invalid("The requested symbol is not configured for Target Analyst.");
        }

        if (!MarketTimeframes.TryParse(request.Timeframe, out var timeframe)
            || !settings.Timeframes.Contains(timeframe))
        {
            throw Invalid("The target-analysis timeframe is not enabled.");
        }

        if ((await store.GetActiveAsync(symbol, cancellationToken)).Count > 0)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.InvalidState,
                "Cancel or wait for the active target to finish before generating another target.");
        }

        await EnsureProviderCanGenerateAsync(cancellationToken);

        var analysisTime = (request.AnalysisTimeUtc ?? now).ToUniversalTime();
        if (analysisTime > now)
        {
            throw Invalid("The target-analysis time cannot be in the future.");
        }

        var id = Guid.NewGuid();
        await store.CreateJobAsync(
            new TargetAnalysisJobWriteModel(
                id,
                symbol,
                timeframe,
                analysisTime,
                workspaceCatalog.Get(TargetWorkspace.Master).PromptVersion,
                settings.ConfigurationVersion,
                now),
            cancellationToken);

        TargetAnalysisContext context;
        try
        {
            context = await BuildContextAsync(symbol, timeframe, analysisTime, cancellationToken);
            await store.UpdateSnapshotAsync(
                new TargetSnapshotWriteModel(
                    id,
                    context.Snapshot.CurrentPrice,
                    JsonSerializer.Serialize(context.Snapshot, SerializerOptions),
                    context.Snapshot.EvidenceIds,
                    timeProvider.GetUtcNow().ToUniversalTime()),
                cancellationToken);
        }
        catch (TechnicalAnalysisException exception)
        {
            logger.LogWarning(
                exception,
                "Target analysis {AnalysisId} could not create a valid market snapshot",
                id);
            return await NoTargetAsync(
                id,
                exception.Code,
                "No defensible target can be produced because the current market snapshot is incomplete.",
                "Market-data or technical-analysis readiness is insufficient.",
                [],
                cancellationToken);
        }
        catch (TargetAnalysisException exception) when (exception.Code is
                   TargetAnalysisErrorCodes.MissingMarketData or TargetAnalysisErrorCodes.NoEvidence)
        {
            return await NoTargetAsync(
                id,
                exception.Code,
                exception.SafeMessage,
                "The immutable evidence snapshot did not meet the minimum data-quality gate.",
                [],
                cancellationToken);
        }

        var specialistTasks = SpecialistWorkspaces.Select(workspace => workspaceRunner.RunSpecialistAsync(
            new TargetAiRequest(
                workspace,
                symbol,
                timeframe.Code(),
                analysisTime,
                workspaceCatalog.Get(workspace).PromptVersion,
                BuildSpecialistPayload(workspace, context),
                context.Evidence[workspace].Compressed.EvidenceIds),
            cancellationToken));
        var runs = (await Task.WhenAll(specialistTasks)).ToList();

        var riskRun = await workspaceRunner.RunSpecialistAsync(
            new TargetAiRequest(
                TargetWorkspace.Risk,
                symbol,
                timeframe.Code(),
                analysisTime,
                workspaceCatalog.Get(TargetWorkspace.Risk).PromptVersion,
                BuildRiskPayload(context, runs),
                context.Snapshot.EvidenceIds),
            cancellationToken);
        runs.Add(riskRun);

        var masterRun = await workspaceRunner.RunMasterAsync(
            new TargetAiRequest(
                TargetWorkspace.Master,
                symbol,
                timeframe.Code(),
                analysisTime,
                workspaceCatalog.Get(TargetWorkspace.Master).PromptVersion,
                BuildMasterPayload(context, runs),
                context.Snapshot.EvidenceIds),
            cancellationToken);
        runs.Add(masterRun);

        var master = masterRun.MasterOutput;
        if (masterRun.Status != TargetWorkspaceExecutionStatus.Completed || master is null)
        {
            return await NoTargetAsync(
                id,
                masterRun.ErrorCode ?? "TARGET_MASTER_FAILED",
                "Target Master did not produce a valid structured result.",
                masterRun.ErrorMessage ?? "Target Master was unavailable or returned malformed output.",
                runs,
                cancellationToken);
        }

        if (!master.ValidTarget)
        {
            return await NoTargetAsync(
                id,
                master.NoTargetReason ?? "NO_VALID_TARGET",
                master.ReasoningSummary,
                master.Uncertainty,
                runs,
                cancellationToken);
        }

        var risk = riskRun.SpecialistOutput;
        if (risk is null)
        {
            return await NoTargetAsync(
                id,
                "RISK_WORKSPACE_FAILED",
                "The proposed target could not pass independent risk validation.",
                riskRun.ErrorMessage ?? "Risk AI did not produce a usable structured result.",
                runs,
                cancellationToken);
        }

        var validationFailure = resultValidator.Validate(
            master,
            risk,
            context.Snapshot,
            context.SelectedEvidence,
            context.RequestedTimeframeAtr,
            runs);
        if (validationFailure is not null)
        {
            return await NoTargetAsync(
                id,
                validationFailure,
                "The proposed target failed the deterministic target-validation gate.",
                master.Uncertainty,
                runs,
                cancellationToken);
        }

        var masterConfiguration = masterRun.Configuration;
        var result = await store.CompleteAsync(
            new TargetAnalysisCompletionWriteModel(
                id,
                master,
                runs,
                masterConfiguration.Provider,
                masterConfiguration.Model,
                masterConfiguration.PromptVersion,
                masterConfiguration.ConfigurationVersion,
                timeProvider.GetUtcNow().ToUniversalTime()),
            cancellationToken);
        logger.LogInformation(
            "Target analysis {AnalysisId} activated one target for {Symbol} {Timeframe}",
            id,
            symbol,
            timeframe);
        return result;
    }

    public async Task<TargetAnalysisResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await store.GetAsync(id, cancellationToken)
        ?? throw new TargetAnalysisException(
            TargetAnalysisErrorCodes.NotFound,
            "The requested target analysis was not found.");

    public async Task<IReadOnlyList<AiProviderAccountStatus>> GetProviderStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var requests = workspaceCatalog.Configurations
            .Where(configuration => configuration.Enabled)
            .GroupBy(configuration => new
            {
                configuration.Provider,
                configuration.BaseUrl,
                configuration.ApiKey
            })
            .Select(group => providerAccountStatus.GetStatusAsync(
                new AiProviderAccountRequest(group.Key.Provider, group.Key.BaseUrl, group.Key.ApiKey),
                cancellationToken));
        return await Task.WhenAll(requests);
    }

    private async Task EnsureProviderCanGenerateAsync(CancellationToken cancellationToken)
    {
        var blocked = (await GetProviderStatusesAsync(cancellationToken))
            .FirstOrDefault(status => !status.CanGenerate);
        if (blocked is null)
        {
            return;
        }

        throw new TargetAnalysisException(
            blocked.State == AiProviderAccountStates.QuotaExhausted
                ? TargetAnalysisErrorCodes.RateLimited
                : TargetAnalysisErrorCodes.AuthenticationFailed,
            blocked.Message);
    }

    public Task<IReadOnlyList<TargetAnalysisResult>> GetActiveAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var normalized = EvidenceNormalization.NormalizeInstrument(symbol)
            ?? throw Invalid("The target-analysis symbol is invalid.");
        return store.GetActiveAsync(normalized, cancellationToken);
    }

    public Task<PagedTargetAnalyses> GetHistoryAsync(
        TargetAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalized = EvidenceNormalization.NormalizeInstrument(query.Symbol)
            ?? throw Invalid("The target-analysis symbol is invalid.");
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > settings.MaximumPageSize)
        {
            throw Invalid($"Target-analysis paging requires page >= 1 and pageSize between 1 and {settings.MaximumPageSize}.");
        }

        return store.QueryAsync(query with { Symbol = normalized }, cancellationToken);
    }

    public async Task<IReadOnlyList<TargetLifecycleItem>> GetLifecycleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _ = await GetAsync(id, cancellationToken);
        return await store.GetLifecycleAsync(id, cancellationToken);
    }

    public async Task<TargetAnalysisResult> CancelAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(id, cancellationToken);
        if (current.Status != TargetAnalysisStatus.Active)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.InvalidState,
                "Only an active target can be cancelled.");
        }

        var cancelled = await store.TryTransitionAsync(
            new TargetLifecycleTransition(
                id,
                TargetAnalysisStatus.Active,
                TargetAnalysisStatus.Cancelled,
                "CANCELLED",
                "Cancelled by the user.",
                current.CurrentPrice,
                timeProvider.GetUtcNow().ToUniversalTime()),
            cancellationToken);
        if (!cancelled)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.InvalidState,
                "The target is no longer active and cannot be cancelled.");
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task MonitorActiveAsync(CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var active = await store.GetActiveAsync(settings.Symbol, cancellationToken);
        foreach (var target in active)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (target.ValidUntilUtc <= now)
            {
                await TransitionAsync(target, TargetAnalysisStatus.Expired, "EXPIRED", "The target validity window ended.", null, now, cancellationToken);
                continue;
            }

            var candles = await marketData.GetHistoryUpToAsync(
                target.Symbol,
                target.Timeframe,
                now,
                2,
                cancellationToken);
            var candle = candles.LastOrDefault(value => value.IsComplete && value.CloseTimeUtc <= now);
            if (candle is null || !target.TargetPrice.HasValue || !target.InvalidationPrice.HasValue || !target.CurrentPrice.HasValue)
            {
                continue;
            }

            var upward = target.TargetPrice > target.CurrentPrice;
            var invalidated = upward
                ? candle.Low <= target.InvalidationPrice
                : candle.High >= target.InvalidationPrice;
            var hit = upward
                ? candle.High >= target.TargetPrice
                : candle.Low <= target.TargetPrice;
            if (invalidated)
            {
                await TransitionAsync(target, TargetAnalysisStatus.Invalidated, "INVALIDATED", "Price reached the target invalidation level.", candle.Close, now, cancellationToken);
            }
            else if (hit)
            {
                await TransitionAsync(target, TargetAnalysisStatus.TargetHit, "TARGET_HIT", "Price reached the active target.", candle.Close, now, cancellationToken);
            }
        }
    }

    private async Task<TargetAnalysisContext> BuildContextAsync(
        string symbol,
        MarketTimeframe requestedTimeframe,
        DateTimeOffset analysisTime,
        CancellationToken cancellationToken)
    {
        var multi = await technicalAnalysis.AnalyzeMultiTimeframeAsync(symbol, analysisTime, cancellationToken);
        if (!multi.Analyses.TryGetValue(requestedTimeframe, out var requestedAnalysis))
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.MissingMarketData,
                "The requested timeframe was unavailable in the technical-analysis snapshot.");
        }

        var latest = await marketData.GetHistoryUpToAsync(
            symbol,
            requestedTimeframe,
            analysisTime,
            2,
            cancellationToken);
        var currentCandle = latest.LastOrDefault(candle => candle.IsComplete && candle.CloseTimeUtc <= analysisTime);
        if (currentCandle is null)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.MissingMarketData,
                "No completed current-price candle was available at the target-analysis time.");
        }

        if (analysisTime - currentCandle.CloseTimeUtc > requestedTimeframe.Duration() * settings.StaleAfterIntervals)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.MissingMarketData,
                "The current-price candle was stale at the target-analysis time.");
        }

        LocalSignalSnapshot? localSignal = null;
        try
        {
            localSignal = await localAnalyst.GetCurrentAsync(symbol, requestedTimeframe, cancellationToken);
        }
        catch (LocalAnalystException exception)
        {
            logger.LogWarning(
                "Target analysis could not load the independent Local Analyst context: {ErrorCode}",
                exception.Code);
        }

        var analyses = multi.Analyses
            .Where(pair => settings.Timeframes.Contains(pair.Key))
            .OrderByDescending(pair => TimeframeRank(pair.Key))
            .ToArray();
        if (analyses.Length < settings.MinimumMarketTimeframes)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.MissingMarketData,
                "Too few market timeframes were available for defensible target analysis.");
        }

        var selections = new Dictionary<TargetWorkspace, TargetEvidencePackage>();
        foreach (var workspace in SpecialistWorkspaces.Append(TargetWorkspace.Risk))
        {
            var query = new AiEvidenceSelectionQuery(
                symbol,
                ToAiSpecialist(workspace),
                AiInterpretationType.EvidenceSynthesis,
                null,
                analysisTime.AddHours(-settings.EvidenceLookbackHours),
                analysisTime,
                settings.MaximumEvidenceItemsPerWorkspace * 4);
            var candidates = await interpretationStore.LoadEvidenceAsync(query, cancellationToken);
            var selection = evidenceSelector.Select(
                candidates,
                query,
                settings.MaximumEvidenceItemsPerWorkspace);
            var compressed = evidenceCompressor.Compress(
                selection,
                query,
                settings.MaximumCompressedCharacters);
            selections[workspace] = new TargetEvidencePackage(selection, compressed);
        }

        var selectedEvidence = selections.Values
            .SelectMany(package => package.Selection.Items.Where(item =>
                package.Compressed.EvidenceIds.Contains(item.Id)))
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.First());
        if (selectedEvidence.Count == 0)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.NoEvidence,
                "No relevant evidence was available at the requested target-analysis time.");
        }

        var prior = await interpretationStore.QueryAsync(
            new AiInterpretationQuery(
                symbol,
                null,
                null,
                null,
                null,
                analysisTime.AddHours(-settings.EvidenceLookbackHours),
                analysisTime.AddTicks(1),
                true,
                1,
                settings.MaximumEvidenceItemsPerWorkspace),
            cancellationToken);
        var priorInterpretations = prior.Items
            .Where(item => item.Status == AiInterpretationExecutionStatus.Completed
                && item.AnalysisTimeUtc <= analysisTime)
            .Select(item => new PriorTargetInterpretation(
                item.Id,
                item.Specialist,
                item.InterpretationType,
                item.Direction,
                item.Confidence,
                item.Summary,
                item.EvidenceIds.Where(selectedEvidence.ContainsKey).ToArray(),
                item.AnalysisTimeUtc,
                item.PromptVersion))
            .Where(item => item.EvidenceIds.Count > 0)
            .ToArray();

        var latestCandles = new Dictionary<MarketTimeframe, StoredMarketCandle>
        {
            [requestedTimeframe] = currentCandle
        };
        foreach (var pair in analyses.Where(pair => pair.Key != requestedTimeframe))
        {
            var candles = await marketData.GetHistoryUpToAsync(
                symbol,
                pair.Key,
                analysisTime,
                2,
                cancellationToken);
            var candle = candles.LastOrDefault(value => value.IsComplete && value.CloseTimeUtc <= analysisTime);
            if (candle is not null)
            {
                latestCandles[pair.Key] = candle;
            }
        }

        var frames = analyses.Where(pair => latestCandles.ContainsKey(pair.Key)).Select(pair =>
        {
            var analysis = pair.Value;
            var candle = latestCandles[pair.Key];
            return new TargetMarketFrameSnapshot(
                pair.Key,
                candle.ProviderKey,
                candle.CloseTimeUtc,
                candle.Open,
                candle.High,
                candle.Low,
                candle.Close,
                candle.TickVolume,
                analysis.Trend.Direction.ToString(),
                $"{analysis.MarketStructure.Direction}:{analysis.MarketStructure.Structure}",
                $"RSI:{analysis.Momentum.Rsi.Momentum};MACD:{analysis.Momentum.Macd.Momentum}",
                analysis.Volatility.Regime.ToString(),
                analysis.Volatility.Atr.Value,
                [.. analysis.SupportResistance.Take(12).Select(level => level.Center)],
                [.. analysis.CandlestickPatterns.TakeLast(8).Select(pattern => $"{pattern.Pattern}:{pattern.Direction}")],
                [.. analysis.Conflicts],
                analysis.Diagnostics.CandlesUsed);
        }).ToArray();
        if (frames.Length < settings.MinimumMarketTimeframes)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.MissingMarketData,
                "Too few complete timeframe candles were available for the immutable target snapshot.");
        }

        var evidenceVersion = Hash(string.Join('|', selections.Values
            .Select(package => package.Selection.EvidenceVersion)
            .Order(StringComparer.Ordinal)));
        var providers = frames
            .GroupBy(frame => frame.Timeframe.Code())
            .ToDictionary(group => group.Key, group => group.First().Provider);
        var snapshot = new TargetAnalysisSnapshot(
            symbol,
            currentCandle.Close,
            analysisTime,
            requestedTimeframe,
            [.. frames.Select(frame => frame.Timeframe)],
            multi.TrendAlignment,
            string.Join(',', requestedAnalysis.PriceAction.Evidence),
            new Dictionary<string, string>
            {
                ["evidence"] = evidenceVersion,
                ["technicalAnalysis"] = "phase6-v1",
                ["localAnalyst"] = localSignal?.ConfigurationVersion ?? "unavailable",
                ["marketSnapshot"] = Hash(string.Join('|', frames.Select(frame => $"{frame.Timeframe}:{frame.LastCandleCloseTimeUtc:O}:{frame.Close}")))
            },
            [.. selectedEvidence.Keys.Order()],
            providers,
            Enum.GetValues<TargetWorkspace>().ToDictionary(
                workspace => workspace,
                workspace => workspaceCatalog.Get(workspace).ConfigurationVersion),
            Enum.GetValues<TargetWorkspace>().ToDictionary(
                workspace => workspace,
                workspace => workspaceCatalog.Get(workspace).PromptVersion),
            frames,
            [.. multi.Conflicts.Concat(selections.Values.SelectMany(package => package.Selection.Conflicts.Select(conflict => conflict.Reason))).Distinct()],
            localSignal);

        return new TargetAnalysisContext(
            snapshot,
            selections,
            selectedEvidence,
            priorInterpretations,
            requestedAnalysis.Volatility.Atr.Value);
    }

    private static string BuildSpecialistPayload(TargetWorkspace workspace, TargetAnalysisContext context)
    {
        var package = context.Evidence[workspace];
        return JsonSerializer.Serialize(new
        {
            snapshot = context.Snapshot,
            evidencePackage = JsonSerializer.Deserialize<JsonElement>(package.Compressed.Json),
            priorInterpretations = RelevantPrior(context, package.Compressed.EvidenceIds)
        }, SerializerOptions);
    }

    private string BuildRiskPayload(
        TargetAnalysisContext context,
        IReadOnlyList<TargetWorkspaceRunResult> runs) =>
        JsonSerializer.Serialize(new
        {
            snapshot = context.Snapshot,
            targetPolicy = new
            {
                settings.MinimumConfidence,
                settings.MinimumTargetDistanceAtr,
                settings.DefaultValidityMinutes,
                settings.MaximumValidityMinutes,
                requestedTimeframeAtr = context.RequestedTimeframeAtr
            },
            evidencePackage = JsonSerializer.Deserialize<JsonElement>(context.Evidence[TargetWorkspace.Risk].Compressed.Json),
            specialistResults = runs.Select(ForSynthesis),
            priorInterpretations = context.PriorInterpretations
        }, SerializerOptions);

    private string BuildMasterPayload(
        TargetAnalysisContext context,
        IReadOnlyList<TargetWorkspaceRunResult> runs) =>
        JsonSerializer.Serialize(new
        {
            snapshot = context.Snapshot,
            targetPolicy = new
            {
                settings.MinimumConfidence,
                settings.MinimumTargetDistanceAtr,
                settings.DefaultValidityMinutes,
                settings.MaximumValidityMinutes,
                requestedTimeframeAtr = context.RequestedTimeframeAtr
            },
            specialistResults = runs.Select(ForSynthesis),
            evidenceConflicts = context.Snapshot.Conflicts,
            priorInterpretations = context.PriorInterpretations
        }, SerializerOptions);

    private static object ForSynthesis(TargetWorkspaceRunResult run) => new
    {
        run.Workspace,
        run.Status,
        run.SpecialistOutput,
        run.ErrorCode,
        run.ErrorMessage,
        run.Configuration.PromptVersion,
        run.Configuration.ConfigurationVersion
    };

    private static IReadOnlyList<PriorTargetInterpretation> RelevantPrior(
        TargetAnalysisContext context,
        IReadOnlyList<Guid> allowedEvidenceIds)
    {
        var allowed = allowedEvidenceIds.ToHashSet();
        return [.. context.PriorInterpretations
            .Select(item => item with { EvidenceIds = [.. item.EvidenceIds.Where(allowed.Contains)] })
            .Where(item => item.EvidenceIds.Count > 0)];
    }

    private async Task<TargetAnalysisResult> NoTargetAsync(
        Guid id,
        string reason,
        string reasoningSummary,
        string uncertainty,
        IReadOnlyList<TargetWorkspaceRunResult> runs,
        CancellationToken cancellationToken)
    {
        var master = workspaceCatalog.Get(TargetWorkspace.Master);
        logger.LogInformation("Target analysis {AnalysisId} ended without a valid target: {Reason}", id, reason);
        return await store.CompleteNoValidTargetAsync(
            new TargetNoValidTargetWriteModel(
                id,
                Limit(reason, 500),
                Limit(reasoningSummary, 4_000),
                Limit(uncertainty, 4_000),
                runs,
                master.Provider,
                master.Model,
                master.PromptVersion,
                master.ConfigurationVersion,
                timeProvider.GetUtcNow().ToUniversalTime()),
            cancellationToken);
    }

    private Task<bool> TransitionAsync(
        TargetAnalysisResult target,
        TargetAnalysisStatus status,
        string eventType,
        string reason,
        decimal? price,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        store.TryTransitionAsync(
            new TargetLifecycleTransition(
                target.Id,
                TargetAnalysisStatus.Active,
                status,
                eventType,
                reason,
                price,
                occurredAt),
            cancellationToken);

    private void EnsureEnabled()
    {
        if (!settings.Enabled)
        {
            throw new TargetAnalysisException(
                TargetAnalysisErrorCodes.Disabled,
                "Target Analyst is disabled by configuration.");
        }
    }

    private static AiSpecialist ToAiSpecialist(TargetWorkspace workspace) => workspace switch
    {
        TargetWorkspace.Structure => AiSpecialist.Structure,
        TargetWorkspace.Liquidity => AiSpecialist.Liquidity,
        TargetWorkspace.Candle => AiSpecialist.Candle,
        TargetWorkspace.Flow => AiSpecialist.Flow,
        TargetWorkspace.Ktr => AiSpecialist.Ktr,
        TargetWorkspace.News => AiSpecialist.News,
        TargetWorkspace.Risk => AiSpecialist.Risk,
        TargetWorkspace.Master => AiSpecialist.Master,
        _ => throw new ArgumentOutOfRangeException(nameof(workspace), workspace, null)
    };

    private static int TimeframeRank(MarketTimeframe timeframe) => timeframe switch
    {
        MarketTimeframe.D1 => 7,
        MarketTimeframe.H4 => 6,
        MarketTimeframe.H1 => 5,
        MarketTimeframe.M30 => 4,
        MarketTimeframe.M15 => 3,
        MarketTimeframe.M5 => 2,
        MarketTimeframe.M1 => 1,
        _ => 0
    };

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string Limit(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static TargetAnalysisException Invalid(string message) => new(
        TargetAnalysisErrorCodes.InvalidRequest,
        message);

    private sealed record TargetEvidencePackage(
        SelectedAiEvidence Selection,
        CompressedAiEvidence Compressed);

    private sealed record TargetAnalysisContext(
        TargetAnalysisSnapshot Snapshot,
        IReadOnlyDictionary<TargetWorkspace, TargetEvidencePackage> Evidence,
        IReadOnlyDictionary<Guid, AiEvidenceCandidate> SelectedEvidence,
        IReadOnlyList<PriorTargetInterpretation> PriorInterpretations,
        decimal? RequestedTimeframeAtr);

    private sealed record PriorTargetInterpretation(
        Guid Id,
        AiSpecialist Specialist,
        AiInterpretationType InterpretationType,
        AiInterpretationDirection Direction,
        decimal? Confidence,
        string Summary,
        IReadOnlyList<Guid> EvidenceIds,
        DateTimeOffset AnalysisTimeUtc,
        string PromptVersion);
}
