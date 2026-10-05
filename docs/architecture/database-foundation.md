# Database foundation

## Scope

Phase 3 established the SQL Server and Entity Framework Core persistence model.
Phase 5 maintains normalized provider candle evidence, Phase 7 persists relevant
news, and Phase 8 adds normalized macroeconomic series with audit-preserving
revisions. Phase 9 adds immutable analyst claims, and Phase 10 enriches the
shared evidence identity with normalization, relations, clusters, quarantine,
and availability-time history. The persistence layer still does not call AI models, evaluate
strategies, generate signals, or run backtests.

SQL Server is the authoritative persistent store. Redis remains optional cache
infrastructure and must not become the source of record.

## Model overview

The initial migration creates 22 application tables. Later source pipelines and
Phase 10 bring the current EF model to 38 mapped application tables:

| Area | Tables | Purpose |
| --- | --- | --- |
| Reference | `DataProviders`, `Instruments`, `Timeframes` | Normalized source, symbol, and timeframe identities |
| Evidence | `EvidenceRecords`, `EvidenceRelations`, `EvidenceClusters`, `EvidenceClusterMembers`, `EvidenceQuarantineRecords` | Normalized attributed evidence, deterministic relationships/grouping, and invalid-record traceability |
| Market | `MarketCandles`, `TechnicalObservations` | Fixed-point OHLCV data and extensible derived measurements |
| News | `NewsArticles`, `NewsArticleContents` | Article metadata separated from content whose storage is permitted |
| News pipeline | `NewsCollectionStates`, `NewsCollectionRuns` | Durable cursor/state and collection metrics |
| Macro | `EconomicEvents` | Scheduled events with nullable forecast, previous, and actual values |
| Economic pipeline | `EconomicSeries`, `EconomicObservations`, `EconomicObservationRevisions`, `EconomicSyncStates`, `EconomicSyncRuns` | Provider series, current values, revision audit, and durable synchronization |
| Analysts | `AnalystSources`, `Analysts`, `AnalystPublications`, `AnalystStatements`, `AnalystSyncStates`, `AnalystSyncRuns` | Attributed immutable claims, publication versions/replicas, and collection state; separate from facts and later interpretation |
| AI | `AiAnalyses`, `AiAnalysisEvidence` | Append-only model/prompt/version outputs and their evidence |
| Strategy | `Strategies`, `StrategyVersions`, `StrategyEvaluations`, `StrategyEvaluationEvidence` | Stable strategy identities, immutable versions, and evidence-backed evaluations |
| Signals | `TradingSignals`, `TradingSignalEvidence`, `SignalOutcomes` | Original signals, explanation links, and independently measured outcomes |
| Backtesting | `BacktestRuns`, `BacktestTrades` | Reproducible run settings, aggregates, and auditable trade detail |
| Statistics | `PerformanceStatistics` | Versioned measured results with sample size and dimensions |
| Market pipeline | `MarketDataSyncStates`, `MarketDataSyncRuns` | Durable recovery cursor/state and per-run metrics |

`EvidenceRecords` gives market candles, technical observations, news, economic
events, and analyst statements a common referentially constrained identity.
AI analyses, strategy evaluations, and signals link to those records through
explicit join tables. This avoids polymorphic IDs without foreign keys and lets
later phases explain exactly which evidence supported a result.

Phase 10 adds controlled evidence/source/direction/importance/category/unit and
quality fields, canonical plus original source values, identity/content hashes,
and separate observed, available, published, collected, and validity timestamps.
Every historical evidence read filters by `AvailableAtUtc` and the validity
interval. Relations preserve republications, contradictions, updates, and other
explicit links; clusters preserve event membership without merging the source
records. Invalid external records can be quarantined rather than discarded.

Economic observations also use `EvidenceRecords`. Their observation period is
stored separately as SQL `date`; evidence/system timestamps are UTC. Current
values use `decimal(28,8)`, and missing provider values remain SQL null.

Signal outcomes are one-to-one records separate from their original signals.
Changing an outcome cannot rewrite the signal's original prices, confidence,
strategy version, or timestamp. Model confidence lives on the signal; measured
accuracy and sample size live in `PerformanceStatistics`.

## Identity and historical versioning

- Internal keys are `uniqueidentifier`. SQL Server generates
  `NEWSEQUENTIALID()` where the entity owns its key, limiting random-index write
  amplification while keeping identifiers unsuitable for business meaning.
- Provider `ExternalId` values and normalized hashes are separate business keys.
- Internal IDs are not an API contract and should not be exposed unless a use
  case genuinely requires them.
- Strategies have a stable `Strategy` identity and immutable
  `StrategyVersion` records.
- AI rows retain provider, model, model version, prompt identifier/version,
  analysis version, and application version. New output creates a new row.
- Backtests retain engine version, parameter JSON/hash, date range, and every
  simulated trade needed to audit aggregates.

## Time and numeric rules

All persisted times have semantic `...AtUtc` or `...TimeUtc` names and use
`datetimeoffset(7)`. EF value converters normalize incoming offsets to UTC.
Database-generated creation timestamps use `SYSUTCDATETIME()`.

Prices and monetary values use `decimal(19,8)`. Volume uses `decimal(28,8)`,
generic technical values use `decimal(28,10)`, and normalized rates/confidence
use `decimal(9,6)`. No financial value uses SQL floating point. `Instruments`
also records `PriceScale`, so future broker-specific display precision is not
assumed from XAUUSD alone.

## Integrity and deduplication

The candle identity is enforced by the unique clustered index:

```text
InstrumentId + TimeframeId + OpenTimeUtc + DataProviderId
```

Repeated overlapping synchronization therefore cannot insert the same candle
twice. A provider-first companion index supports synchronization queries.

Other duplicate protection includes:

- unique normalized evidence identity hash, plus source/external ID and content-hash lookup indexes;
- provider plus external ID for news, economic events, and analyst statements;
- provider plus external series ID for economic series;
- economic series plus observation date for current economic observations;
- canonical URL hash for news;
- source URL hash for analyst statements;
- strategy plus version;
- backtest run plus trade sequence;
- one outcome per signal;
- technical observation identity including calculation version and parameter hash.

The original Phase 4 migration seeded a legacy provider row; it is retained as
inactive migration history. Phase 5 incremental synchronization
looks up the internal instrument, timeframe, and provider identities, inserts
new candles in one batch, skips already-complete rows, and updates an existing
incomplete candle when the provider returns newer values. The database unique
index remains the final concurrency safeguard.

Filtered indexes allow providers that do not supply an external ID. JSON check
constraints reject malformed parameter, result, detail, audit, and risk payloads.
Foreign keys use restrictive deletion so historical records are not silently
cascade-deleted.

## Query and growth strategy

Indexes target known access paths rather than every column:

- evidence by identity, source/external ID, content hash, instrument/time,
  evidence type/time, source type/time, and timeframe/time;
- candles by instrument, timeframe, provider, and UTC range;
- news by publication time, instrument, external ID, and hashes;
- events by currency and scheduled time;
- economic observations by series and observation-date range;
- analyst statements by instrument and publication time;
- AI analyses by type/status/time and model/prompt version;
- evaluations, signals, backtests, and statistics by strategy version and time.

The clustered candle key begins with instrument/timeframe/time, keeping each
series ordered for range scans. Do not partition prematurely. If candle volume
requires it, introduce SQL Server partitioning by `OpenTimeUtc` at a migration
boundary, preserve the unique identity columns in the partitioning design, and
move old partitions to an archival filegroup under an explicit retention policy.

## Runtime and migrations

`XauAiDbContext` and individual mapping classes live under
`XauAi.Infrastructure/Persistence`. Migrations are stored beside them. Restore
the repository-local EF tool and generate future migrations with:

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> `
  --project backend/XauAi.Infrastructure `
  --startup-project backend/XauAi.Infrastructure `
  --context XauAiDbContext `
  --output-dir Persistence/Migrations
```

Docker development uses three database identities/stages:

1. SQL Server starts with the local administrator credential.
2. `database-init` creates the database and restricted `xauai_app` login.
3. `database-migrator` applies EF migrations with administrator authority and exits.
4. The long-running API connects as `xauai_app`, which has only
   `db_datareader` and `db_datawriter` membership.

Production must run migrations as a separately authorized deployment action and
keep `Database:ApplyMigrationsOnStartup` false.

## Backup and recovery foundation

Development data is in the `sqlserver-data` named volume. Stopping Compose
without `--volumes` preserves it. A development backup can be created in a
mounted or copied backup directory using SQL Server `BACKUP DATABASE`; restore
it into a disposable database before trusting it.

Production needs an operator-owned policy covering encrypted full, differential,
and transaction-log backups, off-host storage, retention, restore testing,
recovery-point and recovery-time objectives, and monitoring. Before a production
migration: take and verify a recoverable backup, generate/review the SQL script,
test the migration against a restored copy, and have a forward-fix or restore
plan. EF migration success alone is not a backup strategy.
