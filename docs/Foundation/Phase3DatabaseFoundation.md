# PHASE 3 — DATABASE FOUNDATION

Continue building the XAUUSD AI Trading Intelligence System from Phase 1 and Phase 2.

Phase 1 established the project foundation and architecture.

Phase 2 established configuration and secrets management.

Now implement:

# PHASE 3 — DATABASE FOUNDATION

The objective is to create a professional, scalable SQL Server database foundation that can support the entire future XAUUSD AI analysis system.

Do not implement MT5 integration, news collection, economic-data collection, analyst collection, AI analysis, strategy execution, signal generation, or backtesting yet.

This phase establishes the database architecture those future phases will use.

---

# 1. Final System Context

The eventual system will analyze XAUUSD using multiple evidence sources:

* MT5 market data
* Candlestick patterns
* Technical indicators
* Trend analysis
* Support/resistance
* Market structure
* Momentum
* Volatility
* Multi-timeframe analysis
* Financial news
* Economic events
* Federal Reserve/macroeconomic information
* Analyst opinions
* Historical market behavior
* Multiple trading strategies
* AI analysis
* Backtesting
* Statistical performance

The database must eventually be able to preserve the evidence used to produce an analysis.

The architecture should therefore support:

```text
RAW DATA
   ↓
NORMALIZED DATA
   ↓
ANALYZED DATA
   ↓
AI INTERPRETATION
   ↓
STRATEGY ANALYSIS
   ↓
SIGNAL
   ↓
OUTCOME
   ↓
STATISTICS
```

Do not collapse all of these concepts into one table.

---

# 2. Database Technology

Use:

```text
SQL Server
Entity Framework Core
Code-first migrations
```

Use SQL Server as the primary persistent database.

Redis remains a cache and should NOT become the source of truth.

The database is the authoritative persistent store.

---

# 3. Database Design Principles

Follow these principles:

* Strong primary keys
* Foreign-key constraints
* Appropriate indexes
* Appropriate unique constraints
* UTC timestamps
* Explicit relationships
* Appropriate decimal precision
* No unnecessary duplication
* No giant table containing unrelated data
* No business logic inside SQL unless genuinely necessary
* Preserve historical information
* Avoid destructive updates to historical records
* Design for large time-series datasets
* Design for efficient querying
* Design for future partitioning if needed
* Design for idempotent ingestion
* Design for duplicate detection

The database must be designed for **data integrity first and performance second**, while keeping future scale in mind.

---

# 4. Database Naming

Use consistent naming.

Prefer clear names such as:

```text
MarketCandles
NewsArticles
EconomicEvents
AnalystStatements
AiAnalyses
TradingSignals
BacktestRuns
SignalOutcomes
```

Avoid meaningless names such as:

```text
Data1
Info
Temp
Stuff
Records
```

Use consistent singular/plural conventions throughout the database.

Choose one convention and apply it consistently.

---

# 5. Primary Key Strategy

Use a consistent internal database identifier strategy.

Prefer:

```text
Guid
```

or another technically appropriate non-sequential identifier for public-facing entities.

Do not expose internal database IDs unnecessarily through the UI.

The application may also need provider-specific identifiers and natural/business keys.

For example:

```text
InternalId
ProviderId
ExternalId
```

should not be confused with each other.

Document the decision.

---

# 6. Timestamps

All persisted timestamps must have an explicit meaning.

Prefer UTC internally.

Examples:

```text
CreatedAtUtc
UpdatedAtUtc
PublishedAtUtc
OccurredAtUtc
StartedAtUtc
CompletedAtUtc
```

Never rely on ambiguous timestamps such as:

```text
Date
Time
Timestamp
```

when the meaning is unclear.

For market data, preserve enough information to reconstruct the original market-time context when required.

---

# 7. Market Data Foundation

Prepare the database architecture for future MT5 market data.

The final system will need to store information such as:

```text
Symbol
Timeframe
Open
High
Low
Close
Volume
Spread where available
Open time
Close time if appropriate
Provider/source
```

However, do not build a huge collection system in Phase 3.

Create the **foundation/entities needed for persistent market candles**.

The design must support:

```text
XAUUSD
M1
M5
M15
M30
H1
H4
D1
```

and potentially additional timeframes later.

Do not hardcode the database so that only one timeframe is possible.

---

# 8. Candle Data Precision

Prices must use appropriate SQL decimal precision.

Do NOT store financial prices as floating-point database values unless there is a demonstrated reason.

Use an appropriate decimal type.

The implementation should consider:

* price precision
* volume precision
* broker symbol precision

Do not assume XAUUSD always has one fixed decimal format.

---

# 9. Market Data Uniqueness

Design a uniqueness rule preventing duplicate candles.

A candle should not be inserted twice for the same:

```text
Symbol
Timeframe
OpenTime
Provider
```

or the equivalent normalized identity.

The exact unique-index design should be chosen carefully.

This is extremely important because later MT5 synchronization may repeatedly request overlapping historical ranges.

The ingestion process must be idempotent.

---

# 10. Raw Data vs Processed Data

The system must distinguish raw source data from derived analysis.

For example:

```text
Market Candle
     ↓
Technical Indicator
     ↓
Pattern Detection
     ↓
Strategy Evaluation
     ↓
AI Analysis
```

Do not store everything as if it were raw market data.

A calculated RSI is not the same thing as an MT5 candle.

A detected engulfing pattern is not the same thing as the original candle.

A model interpretation is not the same thing as a technical indicator.

Design the database accordingly.

---

# 11. Technical Analysis Foundation

Prepare the schema for future deterministic technical calculations.

The eventual system may calculate:

```text
RSI
EMA
SMA
MACD
ATR
Bollinger Bands
ADX
Stochastic
Support
Resistance
Trend
Volatility
Market Structure
Candlestick Patterns
```

Do NOT implement the complete technical-analysis engine in Phase 3.

However, the database architecture must allow these derived observations to be stored when Phase 6 is implemented.

Avoid creating 100 unnecessary columns such as:

```text
RSI
EMA9
EMA20
EMA50
EMA100
EMA200
...
```

unless the final architecture has a strong reason for that design.

Prefer a structure that can evolve when new indicators are added.

Document the trade-off.

---

# 12. News Database Foundation

Prepare the database for future financial news.

A future news record may contain:

```text
Source
ExternalId
Title
Description
Content where permitted
URL
PublishedAtUtc
FetchedAtUtc
Author where available
Language
Source category
Symbol relevance
```

Important:

Do not assume every provider gives permission to store full article content.

The schema must allow us to store metadata and permitted content separately.

News implementation itself belongs to Phase 7.

---

# 13. News Deduplication Foundation

The database must eventually support duplicate detection.

Possible duplicate identifiers include:

```text
Provider + ExternalId
Canonical URL
Content hash
Title/source/time combination
```

Do not implement the complete deduplication engine yet.

But design unique constraints/indexes that help make duplicate insertion difficult.

Phase 10 will handle complete normalization and deduplication.

---

# 14. Economic Event Foundation

Prepare for future economic events.

The database should eventually support:

```text
Event
Country
Currency
Category
Importance
ScheduledAtUtc
Previous
Forecast
Actual
Source
ExternalId
```

Do not assume every provider supplies all values.

For example:

```text
Actual = NULL
```

is valid before an event occurs.

Economic-data implementation is Phase 8.

---

# 15. Analyst Data Foundation

Prepare for analyst information.

A future analyst record may contain:

```text
Source
Analyst
PublishedAtUtc
Title
Content/summary where permitted
Instrument
Direction
Target
Time horizon
Source URL
External identifier
```

Do not assume an analyst statement is automatically correct.

Store the source information separately from our interpretation.

For example:

```text
Original Analyst Statement
        ↓
Structured Extraction
        ↓
AI Interpretation
        ↓
Later Outcome
```

Analyst collection belongs to Phase 9.

---

# 16. AI Analysis Foundation

The final system will use multiple AI analysis stages.

The database should eventually support:

```text
News AI Analysis
Market AI Analysis
Macro AI Analysis
Strategy AI Analysis
Historical Context Analysis
Master AI Analysis
```

Do not implement AI analysis in Phase 3.

However, create a database foundation capable of recording:

```text
Model
Provider
Prompt/version identifier
Input reference
Output
CreatedAtUtc
Token usage if available
Cost if available
Latency
Status
Error information where appropriate
```

The exact schema should avoid storing unnecessary duplicated data.

---

# 17. AI Versioning

This is important.

The same market situation may be analyzed by different:

```text
AI model
Prompt version
Strategy version
Application version
```

Therefore, an AI result should be traceable to the configuration used to create it.

For example:

```text
ModelVersion
PromptVersion
AnalysisVersion
CreatedAtUtc
```

Do not overwrite old AI analyses simply because a new model produces a different result.

Historical analyses are valuable for later evaluation.

---

# 18. Strategy Foundation

The final system will analyze multiple strategies, including categories such as:

```text
Candlestick
Trend Following
Breakout
Pullback
Reversal
Momentum
Support/Resistance
Multi-Timeframe
News Driven
Macro Driven
Combined/Multi-Factor
```

Do NOT implement these strategies in Phase 3.

But the database should be designed so strategy definitions and strategy evaluations can eventually be represented.

Do not hardcode strategy names throughout unrelated tables.

A strategy should eventually have a stable identity/version.

---

# 19. Signal Foundation

The final system may produce structured signals.

Do NOT generate signals in Phase 3.

But design for future records containing concepts such as:

```text
Symbol
Direction
Entry
Stop Loss
Take Profit
Time Horizon
Strategy
Signal Time
Analysis Reference
Risk Conditions
Confidence
Status
```

Important:

Do not treat:

```text
AI confidence
```

as equivalent to:

```text
historical accuracy
```

The schema must keep these concepts separate.

---

# 20. Signal Evidence

A final signal should eventually be explainable.

For example:

```text
Signal
  ↓
Technical Evidence
  ↓
Candlestick Evidence
  ↓
News Evidence
  ↓
Macro Evidence
  ↓
Strategy Evidence
  ↓
Historical Evidence
  ↓
AI Reasoning
```

The database should allow the application to trace why a signal existed.

Do not build the full evidence engine yet.

Design the foundation so it can be added without a database rewrite.

---

# 21. Backtesting Foundation

The final system will backtest strategies and signals.

Do not implement backtesting in Phase 3.

But database design should eventually support:

```text
BacktestRun
StrategyVersion
DateRange
Symbol
Timeframe
Parameters
StartingCapital
Results
TradeCount
WinningTrades
LosingTrades
Profit
Loss
Drawdown
WinRate
Expectancy
```

Do not store only a final percentage.

Backtesting must eventually preserve enough information to audit how the result was calculated.

---

# 22. Signal Outcome Foundation

Eventually we need to know what happened after a signal.

For example:

```text
Signal
   ↓
Market develops
   ↓
Outcome
```

Store the actual outcome separately from the original signal.

Do not modify the original signal to make it appear correct later.

This is essential for honest performance measurement.

---

# 23. Statistics Foundation

The final system should eventually calculate:

```text
Win rate
Loss rate
Expectancy
Profit factor
Maximum drawdown
Average R
Average favorable excursion
Average adverse excursion
Sample size
Performance by strategy
Performance by timeframe
Performance by market regime
```

Do not implement these calculations in Phase 3.

Database design should preserve the underlying data needed to calculate them later.

---

# 24. Historical Integrity

Never rewrite historical results merely because the current strategy has changed.

For example:

```text
Strategy V1
Strategy V2
Strategy V3
```

must be distinguishable.

Similarly:

```text
AI Prompt V1
AI Prompt V2
AI Prompt V3
```

must be distinguishable.

This allows us to determine whether improvements actually improved performance.

---

# 25. Entity Framework Core

Create the EF Core foundation.

Establish:

* DbContext
* Entity configurations
* Migration structure
* Connection configuration
* Dependency injection
* Development database startup process

Prefer separate configuration classes rather than placing every mapping into one enormous `OnModelCreating`.

For example:

```text
Infrastructure/
└── Persistence/
    ├── XauAiDbContext.cs
    ├── Configurations/
    └── Migrations/
```

Keep entity configuration maintainable.

---

# 26. Persistence Structure

Use a structure similar to:

```text
XauAi.Infrastructure/
│
└── Persistence/
    │
    ├── XauAiDbContext.cs
    │
    ├── Configurations/
    │
    ├── Migrations/
    │
    └── Extensions/
```

If a better structure fits the existing architecture, use it.

Do not create unnecessary abstraction layers merely for the appearance of Clean Architecture.

---

# 27. Repository Pattern

Do NOT automatically create a generic:

```text
GenericRepository<T>
```

for everything.

Only create repository abstractions when they provide real value.

Entity Framework Core already provides powerful persistence/query capabilities.

Prefer feature-specific abstractions where needed.

For example, later:

```text
IMarketDataRepository
INewsRepository
ISignalRepository
```

if the application actually benefits from them.

---

# 28. Index Strategy

Design indexes based on expected queries.

Future common queries will include:

```text
XAUUSD candles by timeframe and time range

News by published time

Economic events by scheduled time

Analyses by market timestamp

Signals by strategy/time

Backtests by strategy/version

Outcomes by signal
```

Create appropriate indexes.

Do not create indexes on every column.

Every index has a write/storage cost.

Document important index decisions.

---

# 29. Large Time-Series Data

XAUUSD candle data may eventually become large.

Design with future growth in mind.

Consider:

* clustered indexes
* composite indexes
* time-range queries
* archival strategy
* partitioning possibility
* retention policy
* storage growth

Do not prematurely implement complicated partitioning unless the current database scale requires it.

But document how partitioning could be introduced later.

---

# 30. Database Migrations

Create the initial migration.

The migration must be reproducible.

Test:

```text
Empty database
    ↓
Apply migrations
    ↓
Database created
    ↓
Application starts
```

Also verify that the migration can be applied inside the intended development environment.

Do not manually create database tables that are not represented in migrations.

---

# 31. Seed Data

Only create minimal development seed data if genuinely useful.

Do NOT insert fake trading results that could later be mistaken for real historical performance.

If sample data is required, clearly mark it as:

```text
Development/Test Data
```

Never present sample results as actual trading performance.

---

# 32. Database Security

Use least privilege where practical.

Do not use the database administrator account for normal application runtime if the environment allows a restricted application user.

Do not expose database credentials to the frontend.

Do not return database connection strings through API endpoints.

Do not log database passwords.

---

# 33. Database Backup Foundation

Document the future backup requirement.

The application should eventually have a database backup strategy.

Do not build a complete production backup infrastructure in Phase 3 unless required by the current environment.

Create documentation explaining:

* development backup
* production backup
* recovery
* retention
* migration safety

---

# 34. What Phase 3 Must NOT Do

Do NOT implement:

```text
❌ MT5 connection
❌ Live XAUUSD ingestion
❌ News API integration
❌ Economic API integration
❌ Analyst API/scraping
❌ AI API calls
❌ Technical indicator calculations
❌ Candlestick detection
❌ Strategy execution
❌ Signal generation
❌ Backtesting
❌ Performance scoring
❌ Trading execution
❌ Full dashboard
```

Only create the database foundation required for these later phases.

---

# 35. Phase 3 Testing

Run actual tests.

Verify:

```text
[ ] Database can be created
[ ] Initial migration works
[ ] Application connects to SQL Server
[ ] EF Core configuration works
[ ] Foreign keys work
[ ] Unique constraints work
[ ] Important indexes exist
[ ] UTC timestamps behave correctly
[ ] Decimal precision is correct
[ ] Duplicate candle protection works
[ ] Configuration works through environment variables
[ ] Integration tests pass
[ ] Architecture tests pass
[ ] Application starts successfully
```

If tests fail, fix them before declaring the phase complete.

---

# 36. Database Performance Verification

Perform basic query/performance verification for the foundation.

At minimum verify that future-style queries can efficiently filter by:

```text
Symbol
Timeframe
Time range
Provider
External ID
Published time
Scheduled time
```

Do not optimize based on imaginary workloads.

Document assumptions.

---

# 37. Phase 3 Definition of Done

Phase 3 is complete only when:

```text
✅ SQL Server foundation exists
✅ EF Core is configured
✅ DbContext exists
✅ Entity configuration structure exists
✅ Initial migration exists
✅ Database can be created from migration
✅ Market-data foundation exists
✅ News-data foundation exists
✅ Economic-data foundation exists
✅ Analyst-data foundation exists
✅ AI-analysis foundation exists
✅ Strategy foundation exists
✅ Signal foundation exists
✅ Backtest foundation exists
✅ Outcome foundation exists
✅ Statistics foundation is supported
✅ Historical versioning is supported
✅ Important indexes exist
✅ Duplicate protection exists
✅ UTC timestamps are consistent
✅ Decimal precision is appropriate
✅ Database secrets are protected
✅ Tests pass
✅ Documentation exists
```

Do not interpret this as permission to implement all later business logic.

The foundation should exist; the actual processing belongs to later phases.

---

# 38. Final Verification Report

After implementation, provide:

1. Database architecture
2. Tables/entities created
3. Relationships
4. Important indexes
5. Unique constraints
6. Migration information
7. EF Core structure
8. Security decisions
9. Performance decisions
10. Tests executed
11. Test results
12. Any unresolved issue
13. Confirmation that Phase 3 is complete
14. What Phase 4 will implement

Do not claim something was tested if it was not actually tested.

Do not claim the database is production-ready merely because the migration succeeds.

The goal is a strong foundation that can support the remaining 17 phases without requiring a major database redesign.

# END OF PHASE 3
