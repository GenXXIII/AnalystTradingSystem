using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using XauAi.Application.LocalAnalysis;
using XauAi.Application.MarketData;
using XauAi.Domain.Signals;
using XauAi.Domain.Strategies;
using XauAi.Infrastructure.Persistence;

namespace XauAi.Infrastructure.LocalAnalysis.Persistence;

internal sealed class EfLocalSignalStore(
    XauAiDbContext dbContext,
    LocalAnalystSettings settings) : ILocalSignalStore
{
    private static readonly Guid StrategyVersionId = Guid.Parse("41000000-0000-0000-0000-000000000001");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public async Task<LocalAnalystCheckpoint?> GetCheckpointAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        var references = await ResolveAsync(symbol, timeframe, cancellationToken);
        var state = await dbContext.LocalAnalystProcessingStates
            .AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.InstrumentId == references.InstrumentId
                && value.TimeframeId == references.TimeframeId,
                cancellationToken);
        return state is null ? null : ToCheckpoint(symbol, timeframe, state);
    }

    public async Task<LocalSignalSnapshot?> GetActiveAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        var references = await ResolveAsync(symbol, timeframe, cancellationToken);
        var signal = await ActiveQuery(references)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        return signal is null ? null : ToSnapshot(signal, timeframe, LocalSignalStateFrom(signal.Direction), false);
    }

    public async Task<LocalSignalSnapshot> SaveAsync(
        LocalSignalPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var references = await ResolveAsync(request.Decision.Symbol, request.Decision.Timeframe, cancellationToken);
        var evaluation = CreateEvaluation(references, request);
        dbContext.StrategyEvaluations.Add(evaluation);

        TradingSignal? signal = null;
        var mutation = request.Mutation;
        if (mutation == LocalSignalMutation.Open)
        {
            signal = await ActiveQuery(references).SingleOrDefaultAsync(cancellationToken);
            if (signal is null)
            {
                signal = CreateSignal(references, request, evaluation.Id);
                dbContext.TradingSignals.Add(signal);
                AddEvent(signal, evaluation.Id, "OPENED", null, "ACTIVE", request);
            }
            else
            {
                mutation = LocalSignalMutation.Update;
                UpdateSignal(signal, request, evaluation.Id);
                AddEvent(signal, evaluation.Id, "UPDATED", "ACTIVE", "ACTIVE", request);
            }
        }
        else if (mutation is LocalSignalMutation.Update or LocalSignalMutation.Stop)
        {
            signal = await RequireSignalAsync(request.ExistingSignalId, references, cancellationToken);
            var previousStatus = signal.Status;
            UpdateSignal(signal, request, evaluation.Id);
            if (mutation == LocalSignalMutation.Stop)
            {
                signal.Status = "STOPPED";
                signal.InvalidationReason = request.Decision.Reason;
                signal.EndedAtUtc = request.EvaluatedAtUtc;
            }

            AddEvent(
                signal,
                evaluation.Id,
                mutation == LocalSignalMutation.Stop ? "STOPPED" : "UPDATED",
                previousStatus,
                signal.Status,
                request);
        }

        var snapshot = signal is null
            ? ToNothingSnapshot(request.Decision, request.EvaluatedAtUtc)
            : ToSnapshot(
                signal,
                request.Decision.Timeframe,
                mutation == LocalSignalMutation.Stop ? LocalSignalState.Stop : LocalSignalStateFrom(signal.Direction),
                false,
                request.Decision.Conditions,
                request.Decision.Reason,
                request.EvaluatedAtUtc);
        var processingState = await dbContext.LocalAnalystProcessingStates
            .SingleOrDefaultAsync(value =>
                value.InstrumentId == references.InstrumentId
                && value.TimeframeId == references.TimeframeId,
                cancellationToken);
        if (processingState is null)
        {
            processingState = new LocalAnalystProcessingState
            {
                InstrumentId = references.InstrumentId,
                TimeframeId = references.TimeframeId
            };
            dbContext.LocalAnalystProcessingStates.Add(processingState);
        }

        processingState.LastProcessedCandleTimeUtc = request.Decision.SignalCandleTimeUtc;
        processingState.LastStrategyEvaluationId = evaluation.Id;
        processingState.CurrentTradingSignalId = mutation == LocalSignalMutation.Stop
            ? null
            : signal?.Id ?? request.ExistingSignalId;
        processingState.LastResult = request.Decision.State.ToString().ToUpperInvariant();
        processingState.LastReason = request.Decision.Reason;
        processingState.LastSnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        processingState.UpdatedAtUtc = request.EvaluatedAtUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
        return snapshot;
    }

    public async Task<IReadOnlyList<LocalSignalSnapshot>> GetHistoryAsync(
        string symbol,
        MarketTimeframe? timeframe,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var instrumentId = await dbContext.Instruments
            .Where(value => value.Symbol == symbol)
            .Select(value => value.Id)
            .SingleAsync(cancellationToken);
        Guid? timeframeId = null;
        if (timeframe.HasValue)
        {
            timeframeId = await dbContext.Timeframes
                .Where(value => value.Code == timeframe.Value.Code())
                .Select(value => (Guid?)value.Id)
                .SingleAsync(cancellationToken);
        }

        var query = dbContext.TradingSignals.AsNoTracking()
            .Where(value => value.InstrumentId == instrumentId && value.Source == "LocalAnalyst");
        if (timeframeId.HasValue)
        {
            query = query.Where(value => value.TimeframeId == timeframeId.Value);
        }

        var rows = await query
            .OrderByDescending(value => value.SignalAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
        var timeframeCodes = await dbContext.Timeframes.AsNoTracking()
            .ToDictionaryAsync(value => value.Id, value => value.Code, cancellationToken);
        return [.. rows.Select(row =>
        {
            var rowTimeframe = row.TimeframeId.HasValue
                && timeframeCodes.TryGetValue(row.TimeframeId.Value, out var code)
                && MarketTimeframes.TryParse(code, out var parsed)
                    ? parsed
                    : timeframe ?? MarketTimeframe.M1;
            var state = row.Status == "STOPPED" ? LocalSignalState.Stop : LocalSignalStateFrom(row.Direction);
            return ToSnapshot(row, rowTimeframe, state, false);
        })];
    }

    public async Task<IReadOnlyList<LocalSignalLifecycleItem>> GetLifecycleAsync(
        string signalId,
        CancellationToken cancellationToken = default)
    {
        var signal = await dbContext.TradingSignals.AsNoTracking()
            .SingleOrDefaultAsync(value => value.SignalKey == signalId && value.Source == "LocalAnalyst", cancellationToken);
        if (signal is null)
        {
            throw new LocalAnalystException(
                LocalAnalystErrorCodes.SignalNotFound,
                "The requested local signal was not found.");
        }

        return await dbContext.TradingSignalLifecycleEvents.AsNoTracking()
            .Where(value => value.TradingSignalId == signal.Id)
            .OrderBy(value => value.OccurredAtUtc)
            .Select(value => new LocalSignalLifecycleItem(
                signal.SignalKey!,
                value.EventType,
                value.PreviousStatus,
                value.Status,
                value.Direction == "BUY" ? LocalSignalState.Buy : LocalSignalState.Sell,
                value.Reason,
                value.CandleTimeUtc,
                value.Price,
                value.Score,
                value.Confidence,
                value.OccurredAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocalSignalChartMarker>> GetChartMarkersAsync(
        string symbol,
        MarketTimeframe timeframe,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var references = await ResolveAsync(symbol, timeframe, cancellationToken);
        var signals = await dbContext.TradingSignals.AsNoTracking()
            .Where(value => value.InstrumentId == references.InstrumentId
                && value.TimeframeId == references.TimeframeId
                && value.Source == "LocalAnalyst")
            .OrderByDescending(value => value.SignalAtUtc)
            .Take(limit)
            .Select(value => new
            {
                value.Id,
                value.SignalKey,
                value.Direction,
                value.SignalCandleTimeUtc,
                value.EntryPrice
            })
            .ToListAsync(cancellationToken);

        var markers = new List<LocalSignalChartMarker>(signals.Count * 2);
        foreach (var signal in signals)
        {
            if (string.IsNullOrWhiteSpace(signal.SignalKey) || !signal.SignalCandleTimeUtc.HasValue)
            {
                continue;
            }

            markers.Add(new LocalSignalChartMarker(
                signal.SignalKey,
                LocalSignalStateFrom(signal.Direction),
                signal.SignalCandleTimeUtc.Value,
                signal.EntryPrice,
                null));
        }

        var signalIds = signals.Select(value => value.Id).ToArray();
        if (signalIds.Length == 0)
        {
            return [];
        }

        var signalKeys = signals
            .Where(value => !string.IsNullOrWhiteSpace(value.SignalKey))
            .ToDictionary(value => value.Id, value => value.SignalKey!, EqualityComparer<Guid>.Default);
        var signalOpenTimes = signals
            .Where(value => value.SignalCandleTimeUtc.HasValue)
            .ToDictionary(value => value.Id, value => value.SignalCandleTimeUtc!.Value, EqualityComparer<Guid>.Default);
        var stopEvents = await dbContext.TradingSignalLifecycleEvents.AsNoTracking()
            .Where(value => signalIds.Contains(value.TradingSignalId) && value.EventType == "STOPPED")
            .OrderBy(value => value.CandleTimeUtc)
            .Select(value => new
            {
                value.TradingSignalId,
                value.CandleTimeUtc,
                value.Price,
                value.Reason
            })
            .ToListAsync(cancellationToken);
        foreach (var stopEvent in stopEvents)
        {
            if (!signalKeys.TryGetValue(stopEvent.TradingSignalId, out var signalKey))
            {
                continue;
            }

            if (signalOpenTimes.TryGetValue(stopEvent.TradingSignalId, out var signalOpenTime)
                && stopEvent.CandleTimeUtc <= signalOpenTime)
            {
                continue;
            }

            markers.Add(new LocalSignalChartMarker(
                signalKey,
                LocalSignalState.Stop,
                stopEvent.CandleTimeUtc,
                stopEvent.Price,
                stopEvent.Reason));
        }

        return [.. markers
            .OrderBy(value => value.CandleTimeUtc)
            .ThenBy(value => value.State == LocalSignalState.Stop ? 1 : 0)
            .ThenBy(value => value.SignalId, StringComparer.Ordinal)];
    }

    public async Task<IReadOnlyList<LocalAnalystCheckpoint>> GetCheckpointsAsync(
        string symbol,
        IReadOnlyList<MarketTimeframe> timeframes,
        CancellationToken cancellationToken = default)
    {
        var instrumentId = await dbContext.Instruments
            .Where(value => value.Symbol == symbol)
            .Select(value => value.Id)
            .SingleAsync(cancellationToken);
        var definitions = await dbContext.Timeframes.AsNoTracking()
            .Where(value => timeframes.Select(timeframe => timeframe.Code()).Contains(value.Code))
            .ToDictionaryAsync(value => value.Id, value => value.Code, cancellationToken);
        var states = await dbContext.LocalAnalystProcessingStates.AsNoTracking()
            .Where(value => value.InstrumentId == instrumentId && definitions.Keys.Contains(value.TimeframeId))
            .ToListAsync(cancellationToken);
        var byTimeframe = states.ToDictionary(value => value.TimeframeId);
        var result = new List<LocalAnalystCheckpoint>();
        foreach (var timeframe in timeframes)
        {
            var definition = definitions.Single(pair => pair.Value == timeframe.Code());
            if (byTimeframe.TryGetValue(definition.Key, out var state))
            {
                result.Add(ToCheckpoint(symbol, timeframe, state));
            }
            else
            {
                result.Add(new LocalAnalystCheckpoint(
                    symbol,
                    timeframe,
                    null,
                    LocalSignalState.Nothing,
                    "NOT_EVALUATED",
                    DateTimeOffset.MinValue,
                    null));
            }
        }

        return result;
    }

    private StrategyEvaluation CreateEvaluation(
        SignalReferences references,
        LocalSignalPersistenceRequest request) =>
        new()
        {
            Id = Guid.NewGuid(),
            StrategyVersionId = StrategyVersionId,
            InstrumentId = references.InstrumentId,
            TimeframeId = references.TimeframeId,
            EvaluatedAtUtc = request.EvaluatedAtUtc,
            Result = request.Decision.State.ToString().ToUpperInvariant(),
            Score = request.Decision.Confidence,
            DetailsJson = JsonSerializer.Serialize(request.Decision, JsonOptions),
            CreatedAtUtc = request.EvaluatedAtUtc
        };

    private TradingSignal CreateSignal(
        SignalReferences references,
        LocalSignalPersistenceRequest request,
        Guid evaluationId)
    {
        var direction = request.Decision.State.ToString().ToUpperInvariant();
        return new TradingSignal
        {
            Id = Guid.NewGuid(),
            InstrumentId = references.InstrumentId,
            TimeframeId = references.TimeframeId,
            StrategyVersionId = StrategyVersionId,
            StrategyEvaluationId = evaluationId,
            SignalKey = request.NewSignalId,
            Source = "LocalAnalyst",
            SignalCandleId = request.Decision.SignalCandleId,
            SignalCandleTimeUtc = request.Decision.SignalCandleTimeUtc,
            Direction = direction,
            EntryPrice = request.Decision.SignalPrice,
            StopLossPrice = request.Decision.InvalidationPrice,
            TakeProfitPrice = request.Decision.TargetPrice,
            TimeHorizonSeconds = SecondsBetween(request.Decision.CandleCloseTimeUtc, request.Decision.ValidUntilUtc),
            ModelConfidence = request.Decision.Confidence,
            Score = request.Decision.Score,
            MaxScore = request.Decision.MaxScore,
            StructureState = request.Decision.StructureState,
            LiquidityState = request.Decision.LiquidityState,
            CandleState = request.Decision.CandleState,
            MomentumState = request.Decision.MomentumState,
            KtrState = request.Decision.KtrState,
            VolatilityState = request.Decision.VolatilityState,
            ExplanationJson = JsonSerializer.Serialize(request.Decision.Conditions, JsonOptions),
            RiskConditionsJson = JsonSerializer.Serialize(new
            {
                invalidationPrice = request.Decision.InvalidationPrice,
                targetPrice = request.Decision.TargetPrice,
                validUntilUtc = request.Decision.ValidUntilUtc
            }, JsonOptions),
            ValidUntilUtc = request.Decision.ValidUntilUtc,
            LastEvaluatedCandleTimeUtc = request.Decision.SignalCandleTimeUtc,
            ConfigurationVersion = settings.ConfigurationVersion,
            Status = "ACTIVE",
            SignalAtUtc = request.Decision.SignalCandleTimeUtc,
            CreatedAtUtc = request.EvaluatedAtUtc,
            UpdatedAtUtc = request.EvaluatedAtUtc
        };
    }

    private static void UpdateSignal(
        TradingSignal signal,
        LocalSignalPersistenceRequest request,
        Guid evaluationId)
    {
        signal.StrategyEvaluationId = evaluationId;
        signal.ModelConfidence = request.Decision.Confidence;
        signal.Score = request.Decision.Score;
        signal.MaxScore = request.Decision.MaxScore;
        signal.StructureState = request.Decision.StructureState;
        signal.LiquidityState = request.Decision.LiquidityState;
        signal.CandleState = request.Decision.CandleState;
        signal.MomentumState = request.Decision.MomentumState;
        signal.KtrState = request.Decision.KtrState;
        signal.VolatilityState = request.Decision.VolatilityState;
        signal.ExplanationJson = JsonSerializer.Serialize(request.Decision.Conditions, JsonOptions);
        signal.LastEvaluatedCandleTimeUtc = request.Decision.SignalCandleTimeUtc;
        signal.InvalidationReason = request.Decision.Reason;
        signal.UpdatedAtUtc = request.EvaluatedAtUtc;
    }

    private void AddEvent(
        TradingSignal signal,
        Guid evaluationId,
        string eventType,
        string? previousStatus,
        string status,
        LocalSignalPersistenceRequest request) =>
        dbContext.TradingSignalLifecycleEvents.Add(new TradingSignalLifecycleEvent
        {
            Id = Guid.NewGuid(),
            TradingSignalId = signal.Id,
            StrategyEvaluationId = evaluationId,
            EventType = eventType,
            PreviousStatus = previousStatus,
            Status = status,
            Direction = signal.Direction,
            Reason = request.Decision.Reason,
            CandleTimeUtc = request.Decision.SignalCandleTimeUtc,
            Price = request.Decision.SignalPrice,
            Score = request.Decision.Score,
            Confidence = request.Decision.Confidence,
            DetailsJson = JsonSerializer.Serialize(request.Decision.Conditions, JsonOptions),
            OccurredAtUtc = request.EvaluatedAtUtc,
            CreatedAtUtc = request.EvaluatedAtUtc
        });

    private async Task<TradingSignal> RequireSignalAsync(
        Guid? signalId,
        SignalReferences references,
        CancellationToken cancellationToken)
    {
        if (!signalId.HasValue)
        {
            throw new InvalidOperationException("An existing signal identifier is required for this lifecycle mutation.");
        }

        return await dbContext.TradingSignals.SingleAsync(value =>
            value.Id == signalId.Value
            && value.InstrumentId == references.InstrumentId
            && value.TimeframeId == references.TimeframeId
            && value.Source == "LocalAnalyst"
            && value.Status == "ACTIVE",
            cancellationToken);
    }

    private IQueryable<TradingSignal> ActiveQuery(SignalReferences references) =>
        dbContext.TradingSignals.Where(value =>
            value.InstrumentId == references.InstrumentId
            && value.TimeframeId == references.TimeframeId
            && value.Source == "LocalAnalyst"
            && value.Status == "ACTIVE");

    private async Task<SignalReferences> ResolveAsync(
        string symbol,
        MarketTimeframe timeframe,
        CancellationToken cancellationToken)
    {
        var instrumentId = await dbContext.Instruments
            .Where(value => value.Symbol == symbol)
            .Select(value => value.Id)
            .SingleAsync(cancellationToken);
        var timeframeId = await dbContext.Timeframes
            .Where(value => value.Code == timeframe.Code())
            .Select(value => value.Id)
            .SingleAsync(cancellationToken);
        return new SignalReferences(instrumentId, timeframeId);
    }

    private LocalAnalystCheckpoint ToCheckpoint(
        string symbol,
        MarketTimeframe timeframe,
        LocalAnalystProcessingState state)
    {
        LocalSignalSnapshot? snapshot = null;
        if (!string.IsNullOrWhiteSpace(state.LastSnapshotJson))
        {
            snapshot = JsonSerializer.Deserialize<LocalSignalSnapshot>(state.LastSnapshotJson, JsonOptions);
        }

        return new LocalAnalystCheckpoint(
            symbol,
            timeframe,
            state.LastProcessedCandleTimeUtc,
            ParseState(state.LastResult),
            state.LastReason,
            state.UpdatedAtUtc,
            snapshot);
    }

    private LocalSignalSnapshot ToSnapshot(
        TradingSignal signal,
        MarketTimeframe timeframe,
        LocalSignalState state,
        bool cached,
        IReadOnlyList<LocalSignalCondition>? conditions = null,
        string? reason = null,
        DateTimeOffset? evaluatedAtUtc = null)
    {
        conditions ??= DeserializeConditions(signal.ExplanationJson);
        var origin = LocalSignalStateFrom(signal.Direction);
        return new LocalSignalSnapshot(
            signal.SignalKey,
            settings.Symbol,
            timeframe,
            signal.SignalCandleId,
            signal.SignalCandleTimeUtc,
            state,
            origin,
            signal.EntryPrice,
            signal.Score ?? 0m,
            signal.MaxScore ?? settings.MaximumScore,
            signal.ModelConfidence ?? 0m,
            signal.StructureState ?? "Unavailable",
            signal.LiquidityState ?? "Unavailable",
            signal.CandleState ?? "Unavailable",
            signal.MomentumState ?? "Unavailable",
            signal.KtrState ?? "Unavailable",
            signal.VolatilityState ?? "Unavailable",
            signal.StopLossPrice,
            signal.TakeProfitPrice,
            reason ?? signal.InvalidationReason,
            signal.ValidUntilUtc,
            signal.Status,
            signal.ConfigurationVersion ?? settings.ConfigurationVersion,
            conditions,
            evaluatedAtUtc ?? signal.UpdatedAtUtc,
            signal.CreatedAtUtc,
            signal.UpdatedAtUtc,
            signal.EndedAtUtc,
            cached,
            signal.Id);
    }

    private LocalSignalSnapshot ToNothingSnapshot(LocalSignalDecision decision, DateTimeOffset evaluatedAtUtc) =>
        new(
            null,
            decision.Symbol,
            decision.Timeframe,
            decision.SignalCandleId,
            decision.SignalCandleTimeUtc,
            LocalSignalState.Nothing,
            null,
            decision.SignalPrice,
            decision.Score,
            decision.MaxScore,
            decision.Confidence,
            decision.StructureState,
            decision.LiquidityState,
            decision.CandleState,
            decision.MomentumState,
            decision.KtrState,
            decision.VolatilityState,
            decision.InvalidationPrice,
            decision.TargetPrice,
            decision.Reason,
            decision.ValidUntilUtc,
            "NONE",
            settings.ConfigurationVersion,
            decision.Conditions,
            evaluatedAtUtc,
            null,
            null,
            null,
            false);

    private static IReadOnlyList<LocalSignalCondition> DeserializeConditions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<LocalSignalCondition[]>(json, JsonOptions) ?? [];
    }

    private static int SecondsBetween(DateTimeOffset start, DateTimeOffset end) =>
        Math.Max(1, checked((int)Math.Min((end - start).TotalSeconds, int.MaxValue)));

    private static LocalSignalState LocalSignalStateFrom(string direction) =>
        direction == "BUY" ? LocalSignalState.Buy : LocalSignalState.Sell;

    private static LocalSignalState ParseState(string value) =>
        Enum.TryParse<LocalSignalState>(value, true, out var parsed) ? parsed : LocalSignalState.Nothing;

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record SignalReferences(Guid InstrumentId, Guid TimeframeId);
}
