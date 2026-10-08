using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using XauAi.Application.AI;
using XauAi.Application.Evidence;
using XauAi.Application.MarketData;
using XauAi.Application.TechnicalAnalysis;

namespace XauAi.Application.FullAnalysis;

internal sealed class FullAnalystService(
    IFullAnalysisStore store,
    IAiInterpretationStore interpretationStore,
    IAiEvidenceSelector evidenceSelector,
    IAiEvidenceCompressor evidenceCompressor,
    IMarketDataQueryStore marketData,
    ITechnicalAnalysisService technicalAnalysis,
    IFullWorkspaceRunner workspaceRunner,
    IFullResultValidator resultValidator,
    IAiProviderAccountStatusService providerAccountStatus,
    FullWorkspaceCatalog workspaceCatalog,
    FullAnalystSettings settings,
    TimeProvider timeProvider,
    ILogger<FullAnalystService> logger) : IFullAnalystService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly FullWorkspace[] SpecialistWorkspaces =
    [
        FullWorkspace.Structure,
        FullWorkspace.Liquidity,
        FullWorkspace.Candle,
        FullWorkspace.Flow,
        FullWorkspace.Ktr,
        FullWorkspace.News
    ];

    public async Task<FullAnalysisResult> AnalyzeAsync(
        CreateFullAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var symbol = EvidenceNormalization.NormalizeInstrument(request.Symbol)
            ?? throw Invalid("The Full Analyst symbol must be a supported XAUUSD alias.");
        if (!string.Equals(symbol, settings.Symbol, StringComparison.Ordinal))
        {
            throw Invalid("The requested symbol is not configured for Full Analyst.");
        }

        if (!MarketTimeframes.TryParse(request.Timeframe, out var timeframe)
            || !settings.Timeframes.Contains(timeframe))
        {
            throw Invalid("The Full Analyst timeframe is not enabled.");
        }

        if ((await store.GetActiveAsync(symbol, cancellationToken)).Count > 0)
        {
            throw new FullAnalysisException(
                FullAnalysisErrorCodes.InvalidState,
                "Cancel or wait for the active Future Analyst result to finish before generating another outlook.");
        }

        await EnsureProviderCanGenerateAsync(cancellationToken);

        var analysisTime = (request.AnalysisTimeUtc ?? now).ToUniversalTime();
        if (analysisTime > now)
        {
            throw Invalid("The Full Analyst time cannot be in the future.");
        }

        var id = Guid.NewGuid();
        await store.CreateJobAsync(
            new FullAnalysisJobWriteModel(
                id,
                symbol,
                timeframe,
                analysisTime,
                workspaceCatalog.Get(FullWorkspace.Master).PromptVersion,
                settings.ConfigurationVersion,
                now),
            cancellationToken);

        FullAnalysisContext context;
        try
        {
            context = await BuildContextAsync(symbol, timeframe, analysisTime, cancellationToken);
            await store.UpdateSnapshotAsync(
                new FullSnapshotWriteModel(
                    id,
                    context.Snapshot.CurrentPrice,
                    context.StateHash,
                    JsonSerializer.Serialize(context.Snapshot, SerializerOptions),
                    timeProvider.GetUtcNow().ToUniversalTime()),
                cancellationToken);
        }
        catch (TechnicalAnalysisException exception)
        {
            logger.LogWarning(exception, "Full analysis {AnalysisId} could not create a valid market snapshot", id);
            return await WaitAsync(
                id,
                "Insufficient or invalid market data.",
                "The multi-timeframe technical snapshot was unavailable or incomplete.",
                [],
                cancellationToken);
        }
        catch (FullAnalysisException exception) when (exception.Code is
                   FullAnalysisErrorCodes.MissingMarketData or FullAnalysisErrorCodes.NoEvidence)
        {
            return await WaitAsync(id, exception.SafeMessage, "The evidence-quality gate required WAIT.", [], cancellationToken);
        }

        var specialistRuns = await Task.WhenAll(SpecialistWorkspaces.Select(workspace =>
            workspaceRunner.RunSpecialistAsync(
                new FullAiRequest(
                    workspace,
                    symbol,
                    timeframe.Code(),
                    analysisTime,
                    workspaceCatalog.Get(workspace).PromptVersion,
                    context.StateHash,
                    BuildSpecialistPayload(workspace, context),
                    context.Evidence[workspace].Compressed.EvidenceIds),
                cancellationToken)));
        var runs = specialistRuns.ToList();
        if (runs.Any(run => run.Status is FullWorkspaceExecutionStatus.Failed or FullWorkspaceExecutionStatus.Disabled))
        {
            return await WaitAsync(
                id,
                "One or more Full AI specialists were unavailable.",
                "A directional state was not forced from an incomplete specialist set.",
                runs,
                cancellationToken);
        }

        var riskRun = await workspaceRunner.RunSpecialistAsync(
            new FullAiRequest(
                FullWorkspace.Risk,
                symbol,
                timeframe.Code(),
                analysisTime,
                workspaceCatalog.Get(FullWorkspace.Risk).PromptVersion,
                context.StateHash,
                BuildRiskPayload(context, runs),
                context.Snapshot.EvidenceIds),
            cancellationToken);
        runs.Add(riskRun);
        if (riskRun.Status is FullWorkspaceExecutionStatus.Failed or FullWorkspaceExecutionStatus.Disabled
            || riskRun.SpecialistOutput is null)
        {
            return await WaitAsync(
                id,
                "Full Risk AI could not validate the current setup.",
                riskRun.ErrorMessage ?? "Risk evidence was insufficient.",
                runs,
                cancellationToken);
        }

        var masterRun = await workspaceRunner.RunMasterAsync(
            new FullAiRequest(
                FullWorkspace.Master,
                symbol,
                timeframe.Code(),
                analysisTime,
                workspaceCatalog.Get(FullWorkspace.Master).PromptVersion,
                context.StateHash,
                BuildMasterPayload(context, runs),
                context.Snapshot.EvidenceIds),
            cancellationToken);
        runs.Add(masterRun);
        var master = masterRun.MasterOutput;
        if (masterRun.Status is FullWorkspaceExecutionStatus.Failed or FullWorkspaceExecutionStatus.Disabled
            || master is null)
        {
            return await WaitAsync(
                id,
                "Full Master AI did not produce a valid structured result.",
                masterRun.ErrorMessage ?? "Master synthesis was unavailable.",
                runs,
                cancellationToken);
        }

        var validationFailure = resultValidator.Validate(
            master,
            riskRun.SpecialistOutput,
            context.Snapshot,
            runs);
        if (validationFailure is not null)
        {
            return await WaitAsync(
                id,
                "The master direction failed the deterministic safety gate.",
                validationFailure,
                runs,
                cancellationToken);
        }

        var masterConfiguration = masterRun.Configuration;
        return await store.CompleteAsync(
            new FullAnalysisCompletionWriteModel(
                id,
                master.Decision,
                master.Confidence,
                master.Agreement,
                master.Conflicts,
                master.KeyEvidenceIds,
                master.Reasoning,
                master.Invalidation,
                master.Uncertainty,
                master.Decision == FullDecision.Wait ? null : master.ValidUntilUtc,
                runs,
                masterRun.Id,
                masterConfiguration.Provider,
                masterConfiguration.Model,
                masterConfiguration.PromptVersion,
                masterConfiguration.ConfigurationVersion,
                timeProvider.GetUtcNow().ToUniversalTime()),
            cancellationToken);
    }

    public async Task<FullAnalysisResult> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await store.GetAsync(id, cancellationToken)
        ?? throw new FullAnalysisException(FullAnalysisErrorCodes.NotFound, "The requested Full Analysis was not found.");

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

        throw new FullAnalysisException(
            blocked.State == AiProviderAccountStates.QuotaExhausted
                ? FullAnalysisErrorCodes.RateLimited
                : FullAnalysisErrorCodes.AuthenticationFailed,
            blocked.Message);
    }

    public Task<IReadOnlyList<FullAnalysisResult>> GetActiveAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var normalized = EvidenceNormalization.NormalizeInstrument(symbol)
            ?? throw Invalid("The Full Analyst symbol is invalid.");
        return store.GetActiveAsync(normalized, cancellationToken);
    }

    public Task<PagedFullAnalyses> GetHistoryAsync(
        FullAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalized = EvidenceNormalization.NormalizeInstrument(query.Symbol)
            ?? throw Invalid("The Full Analyst symbol is invalid.");
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > settings.MaximumPageSize)
        {
            throw Invalid($"Full Analyst paging requires page >= 1 and pageSize between 1 and {settings.MaximumPageSize}.");
        }

        return store.QueryAsync(query with { Symbol = normalized }, cancellationToken);
    }

    public async Task<IReadOnlyList<FullLifecycleItem>> GetLifecycleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _ = await GetAsync(id, cancellationToken);
        return await store.GetLifecycleAsync(id, cancellationToken);
    }

    public async Task<FullAnalysisResult> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync(id, cancellationToken);
        if (current.Status != FullAnalysisStatus.Active)
        {
            throw new FullAnalysisException(
                FullAnalysisErrorCodes.InvalidState,
                "Only an active Full Analyst result can be cancelled.");
        }

        var changed = await store.TryTransitionAsync(
            new FullLifecycleTransition(
                id,
                FullAnalysisStatus.Active,
                FullAnalysisStatus.Cancelled,
                "CANCELLED",
                "Cancelled by the user.",
                current.CurrentPrice,
                timeProvider.GetUtcNow().ToUniversalTime()),
            cancellationToken);
        if (!changed)
        {
            throw new FullAnalysisException(
                FullAnalysisErrorCodes.InvalidState,
                "The Full Analyst result is no longer active and cannot be cancelled.");
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
        foreach (var analysis in await store.GetActiveAsync(settings.Symbol, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (analysis.ValidUntilUtc <= now)
            {
                await TransitionAsync(analysis, FullAnalysisStatus.Expired, "EXPIRED", "The Full Analyst validity window ended.", null, now, cancellationToken);
                continue;
            }

            var invalidation = analysis.Invalidation;
            if (invalidation?.Price is not > 0m)
            {
                continue;
            }

            var candles = await marketData.GetHistoryUpToAsync(analysis.Symbol, analysis.Timeframe, now, 2, cancellationToken);
            var candle = candles.LastOrDefault(value => value.IsComplete && value.CloseTimeUtc <= now);
            if (candle is null)
            {
                continue;
            }

            var invalidated = invalidation.Condition switch
            {
                FullInvalidationCondition.AtOrBelow => candle.Low <= invalidation.Price,
                FullInvalidationCondition.AtOrAbove => candle.High >= invalidation.Price,
                _ => false
            };
            if (invalidated)
            {
                await TransitionAsync(analysis, FullAnalysisStatus.Invalidated, "INVALIDATED", invalidation.Summary, candle.Close, now, cancellationToken);
            }
        }
    }

    private async Task<FullAnalysisContext> BuildContextAsync(
        string symbol,
        MarketTimeframe requestedTimeframe,
        DateTimeOffset analysisTime,
        CancellationToken cancellationToken)
    {
        var multi = await technicalAnalysis.AnalyzeMultiTimeframeAsync(symbol, analysisTime, cancellationToken);
        if (!multi.Analyses.TryGetValue(requestedTimeframe, out var requestedAnalysis))
        {
            throw Missing("The requested timeframe was unavailable in the Full Analyst snapshot.");
        }

        var latest = await marketData.GetHistoryUpToAsync(symbol, requestedTimeframe, analysisTime, 2, cancellationToken);
        var currentCandle = latest.LastOrDefault(candle => candle.IsComplete && candle.CloseTimeUtc <= analysisTime);
        if (currentCandle is null
            || analysisTime - currentCandle.CloseTimeUtc > requestedTimeframe.Duration() * settings.StaleAfterIntervals)
        {
            throw Missing("A fresh completed current-price candle was unavailable.");
        }

        var analyses = multi.Analyses
            .Where(pair => settings.Timeframes.Contains(pair.Key))
            .OrderByDescending(pair => TimeframeRank(pair.Key))
            .ToArray();
        if (analyses.Length < settings.MinimumMarketTimeframes)
        {
            throw Missing("Too few market timeframes were available for Full Analyst.");
        }

        var selections = new Dictionary<FullWorkspace, FullEvidencePackage>();
        foreach (var workspace in SpecialistWorkspaces.Append(FullWorkspace.Risk))
        {
            var query = new AiEvidenceSelectionQuery(
                symbol,
                ToAiSpecialist(workspace),
                AiInterpretationType.EvidenceSynthesis,
                requestedTimeframe.Code(),
                analysisTime.AddHours(-settings.EvidenceLookbackHours),
                analysisTime,
                settings.MaximumEvidenceItemsPerWorkspace * 4);
            var candidates = await interpretationStore.LoadEvidenceAsync(query, cancellationToken);
            var selection = evidenceSelector.Select(candidates, query, settings.MaximumEvidenceItemsPerWorkspace);
            var compressed = evidenceCompressor.Compress(selection, query, settings.MaximumCompressedCharacters);
            selections[workspace] = new FullEvidencePackage(selection, compressed);
        }

        var evidenceIds = selections.Values
            .SelectMany(package => package.Compressed.EvidenceIds)
            .Distinct()
            .Order()
            .ToArray();
        if (evidenceIds.Length == 0)
        {
            throw new FullAnalysisException(FullAnalysisErrorCodes.NoEvidence, "No relevant evidence was available at the requested analysis time.");
        }

        var latestCandles = new Dictionary<MarketTimeframe, StoredMarketCandle>
        {
            [requestedTimeframe] = currentCandle
        };
        foreach (var pair in analyses.Where(pair => pair.Key != requestedTimeframe))
        {
            var candles = await marketData.GetHistoryUpToAsync(symbol, pair.Key, analysisTime, 2, cancellationToken);
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
            return new FullMarketFrameSnapshot(
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
            throw Missing("Too few completed timeframe candles were available for the immutable snapshot.");
        }

        var evidenceVersion = Hash(string.Join('|', selections.Values
            .Select(package => package.Selection.EvidenceVersion)
            .Order(StringComparer.Ordinal)));
        var marketVersion = Hash(string.Join('|', frames.Select(frame =>
            $"{frame.Timeframe}:{frame.LastCandleCloseTimeUtc:O}:{frame.Open}:{frame.High}:{frame.Low}:{frame.Close}:{frame.TickVolume}")));
        var configurationVersion = Hash(string.Join('|', Enum.GetValues<FullWorkspace>().Select(workspace =>
            $"{workspace}:{workspaceCatalog.Get(workspace).ConfigurationVersion}:{workspaceCatalog.Get(workspace).PromptVersion}")));
        var stateHash = Hash($"{symbol}|{requestedTimeframe}|{marketVersion}|{evidenceVersion}|{configurationVersion}");
        var snapshot = new FullAnalysisSnapshot(
            symbol,
            currentCandle.Close,
            analysisTime,
            requestedTimeframe,
            [.. frames.Select(frame => frame.Timeframe)],
            currentCandle.IsComplete ? "Closed" : "Forming",
            multi.TrendAlignment,
            new Dictionary<string, string>
            {
                ["evidence"] = evidenceVersion,
                ["technicalAnalysis"] = "phase6-v1",
                ["marketSnapshot"] = marketVersion,
                ["state"] = stateHash
            },
            evidenceIds,
            frames.GroupBy(frame => frame.Timeframe.Code()).ToDictionary(group => group.Key, group => group.First().Provider),
            Enum.GetValues<FullWorkspace>().ToDictionary(workspace => workspace, workspace => workspaceCatalog.Get(workspace).ConfigurationVersion),
            Enum.GetValues<FullWorkspace>().ToDictionary(workspace => workspace, workspace => workspaceCatalog.Get(workspace).PromptVersion),
            frames,
            [.. multi.Conflicts.Concat(selections.Values.SelectMany(package => package.Selection.Conflicts.Select(conflict => conflict.Reason))).Distinct()]);

        return new FullAnalysisContext(snapshot, selections, requestedAnalysis.Volatility.Atr.Value, stateHash);
    }

    private static string BuildSpecialistPayload(FullWorkspace workspace, FullAnalysisContext context)
    {
        var package = context.Evidence[workspace];
        return JsonSerializer.Serialize(new
        {
            snapshot = context.Snapshot,
            evidencePackage = JsonSerializer.Deserialize<JsonElement>(package.Compressed.Json),
            workspaceScope = Scope(workspace)
        }, SerializerOptions);
    }

    private string BuildRiskPayload(FullAnalysisContext context, IReadOnlyList<FullWorkspaceRunResult> runs) =>
        JsonSerializer.Serialize(new
        {
            snapshot = context.Snapshot,
            policy = new
            {
                settings.MinimumConfidence,
                settings.DefaultValidityMinutes,
                settings.MaximumValidityMinutes,
                requestedTimeframeAtr = context.RequestedTimeframeAtr
            },
            evidencePackage = JsonSerializer.Deserialize<JsonElement>(context.Evidence[FullWorkspace.Risk].Compressed.Json),
            specialistResults = runs.Select(ForSynthesis)
        }, SerializerOptions);

    private string BuildMasterPayload(FullAnalysisContext context, IReadOnlyList<FullWorkspaceRunResult> runs) =>
        JsonSerializer.Serialize(new
        {
            snapshot = context.Snapshot,
            policy = new
            {
                settings.MinimumConfidence,
                settings.DefaultValidityMinutes,
                settings.MaximumValidityMinutes,
                requestedTimeframeAtr = context.RequestedTimeframeAtr,
                waitWhenEvidenceIsWeakConflictingStaleIncompleteOrInvalid = true,
                majorityVotingForbidden = true
            },
            specialistResults = runs.Select(ForSynthesis),
            evidenceConflicts = context.Snapshot.Conflicts
        }, SerializerOptions);

    private static object ForSynthesis(FullWorkspaceRunResult run) => new
    {
        run.Workspace,
        run.Status,
        run.SpecialistOutput,
        run.ErrorCode,
        run.ErrorMessage,
        run.Configuration.PromptVersion,
        run.Configuration.ConfigurationVersion
    };

    private async Task<FullAnalysisResult> WaitAsync(
        Guid id,
        string reasoning,
        string uncertainty,
        IReadOnlyList<FullWorkspaceRunResult> runs,
        CancellationToken cancellationToken)
    {
        var master = workspaceCatalog.Get(FullWorkspace.Master);
        logger.LogInformation("Full analysis {AnalysisId} ended with WAIT: {Reason}", id, reasoning);
        return await store.CompleteAsync(
            new FullAnalysisCompletionWriteModel(
                id,
                FullDecision.Wait,
                0m,
                0m,
                [Limit(reasoning, 1_000)],
                [],
                Limit(reasoning, 4_000),
                new FullInvalidation("No active direction to invalidate.", null, FullInvalidationCondition.None),
                Limit(uncertainty, 4_000),
                null,
                runs,
                runs.LastOrDefault(run => run.Workspace == FullWorkspace.Master)?.Id,
                master.Provider,
                master.Model,
                master.PromptVersion,
                master.ConfigurationVersion,
                timeProvider.GetUtcNow().ToUniversalTime()),
            cancellationToken);
    }

    private Task<bool> TransitionAsync(
        FullAnalysisResult analysis,
        FullAnalysisStatus status,
        string eventType,
        string reason,
        decimal? price,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) =>
        store.TryTransitionAsync(
            new FullLifecycleTransition(
                analysis.Id,
                FullAnalysisStatus.Active,
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
            throw new FullAnalysisException(FullAnalysisErrorCodes.Disabled, "Full Analyst is disabled by configuration.");
        }
    }

    private static string Scope(FullWorkspace workspace) => workspace switch
    {
        FullWorkspace.Structure => "HH, HL, LH, LL, BOS, CHoCH, trend, range, transition, strength, and structural invalidation.",
        FullWorkspace.Liquidity => "Price-derived highs, lows, equal levels, swing pools, sweeps, rejection, attraction, and conflicts; never order-book claims.",
        FullWorkspace.Candle => "Contextual rejection, engulfing, momentum, breakout, failure, continuation, exhaustion, and reversal.",
        FullWorkspace.Flow => "Price momentum, directional pressure, acceleration, impulse, pullback, tick volume, volatility, and exhaustion; never fabricated order flow.",
        FullWorkspace.Ktr => "Only supplied KTR, important price and session levels, breakouts, retests, volatility-adjusted levels, and reaction zones.",
        FullWorkspace.News => "Economic, USD, macro, central-bank, news, and attributed analyst evidence with facts separated from interpretation.",
        FullWorkspace.Risk => "Volatility, regime, news risk, conflicts, invalidation, data quality, uncertainty, and abnormal conditions.",
        FullWorkspace.Master => "Evidence-weighted synthesis without majority voting.",
        _ => throw new ArgumentOutOfRangeException(nameof(workspace), workspace, null)
    };

    private static AiSpecialist ToAiSpecialist(FullWorkspace workspace) => workspace switch
    {
        FullWorkspace.Structure => AiSpecialist.Structure,
        FullWorkspace.Liquidity => AiSpecialist.Liquidity,
        FullWorkspace.Candle => AiSpecialist.Candle,
        FullWorkspace.Flow => AiSpecialist.Flow,
        FullWorkspace.Ktr => AiSpecialist.Ktr,
        FullWorkspace.News => AiSpecialist.News,
        FullWorkspace.Risk => AiSpecialist.Risk,
        FullWorkspace.Master => AiSpecialist.Master,
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

    private static FullAnalysisException Invalid(string message) => new(FullAnalysisErrorCodes.InvalidRequest, message);
    private static FullAnalysisException Missing(string message) => new(FullAnalysisErrorCodes.MissingMarketData, message);

    private sealed record FullEvidencePackage(SelectedAiEvidence Selection, CompressedAiEvidence Compressed);

    private sealed record FullAnalysisContext(
        FullAnalysisSnapshot Snapshot,
        IReadOnlyDictionary<FullWorkspace, FullEvidencePackage> Evidence,
        decimal? RequestedTimeframeAtr,
        string StateHash);
}
