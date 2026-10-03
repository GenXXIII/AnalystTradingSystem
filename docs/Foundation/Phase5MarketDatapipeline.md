# PHASE 5 — MARKET DATA PIPELINE

Continue building the XAUUSD AI Trading Intelligence System from Phases 1–4.

Completed:

* Phase 1 — Project Foundation
* Phase 2 — Configuration & Secrets
* Phase 3 — Database Foundation
* Phase 4 — MT5 Integration

Now implement:

# PHASE 5 — MARKET DATA PIPELINE

The purpose of this phase is to build a reliable market-data pipeline on top of the MT5 integration.

Phase 4 established the ability to communicate with MT5 and retrieve XAUUSD data.

Phase 5 turns that capability into a reliable, persistent, recoverable market-data system.

---

# 1. CORE DATA FLOW

Implement this pipeline:

```text
                    MT5 Desktop
                         │
                         ↓
                MT5 Market Provider
                         │
                         ↓
                  Data Acquisition
                         │
                         ↓
                    Validation
                         │
                         ↓
                  Normalization
                         │
                         ↓
                  Deduplication
                         │
                         ↓
                Gap Detection
                         │
                         ↓
                  Persistence
                         │
                         ↓
                     SQL Server
                         │
                         ↓
                Market Data API
```

The pipeline must be designed so that the database becomes the reliable historical source for the application.

---

# 2. IMPORTANT RESPONSIBILITY BOUNDARY

Phase 4:

```text
"Can I communicate with MT5 and retrieve data?"
```

Phase 5:

```text
"Can I reliably maintain a complete and consistent market-data dataset?"
```

Do not move Phase 6 technical analysis into this phase.

---

# 3. SYMBOL

The initial instrument is:

```text
XAUUSD
```

Do not hardcode `XAUUSD` throughout the codebase.

Use the symbol configuration/mapping created in Phase 4.

The architecture should allow another symbol to be added later without rewriting the pipeline.

---

# 4. TIMEFRAMES

Support:

```text
M1
M5
M15
M30
H1
H4
D1
```

The pipeline must treat timeframe as part of the identity of market data.

For example:

```text
XAUUSD + M5 + 10:35
```

is different from:

```text
XAUUSD + H1 + 10:00
```

Do not mix timeframes.

---

# 5. INITIAL HISTORICAL SYNCHRONIZATION

Implement an initial synchronization process.

Example:

```text
Start
  ↓
Check database
  ↓
No data?
  ↓
Request configured historical range from MT5
  ↓
Validate
  ↓
Normalize
  ↓
Deduplicate
  ↓
Persist
  ↓
Verify
```

The historical range must be configurable.

Do NOT automatically request unlimited history.

For example, configuration may support:

```text
InitialHistoryDays
```

or an explicit date range.

Choose the approach that fits the existing architecture best.

---

# 6. INCREMENTAL SYNCHRONIZATION

Implement incremental synchronization.

Example:

```text
Database
   │
   └── Latest stored candle = 2026-10-01 10:00
                         ↓
                  Ask MT5 for newer data
                         ↓
                  Retrieve candles
                         ↓
                  Validate
                         ↓
                  Save only missing data
```

The pipeline should not repeatedly download the entire history.

This is important for:

* speed
* MT5 load
* database load
* reliability
* future API/resource limits

---

# 7. OVERLAPPING REQUESTS

Real systems often request overlapping ranges.

Example:

```text
Existing:
10:00
10:05
10:10
10:15

New request:
10:10
10:15
10:20
10:25
```

The pipeline must safely handle this.

Result:

```text
10:00
10:05
10:10
10:15
10:20
10:25
```

NOT:

```text
10:00
10:05
10:10
10:10   ← duplicate
10:15
10:15   ← duplicate
10:20
10:25
```

Use database constraints and application-level deduplication appropriately.

Do not rely on only one layer.

---

# 8. CANDLE VALIDATION

Before storing a candle, validate it.

At minimum check:

```text
Open > 0
High > 0
Low > 0
Close > 0
```

And logical OHLC relationships:

```text
High >= Open
High >= Close
High >= Low

Low <= Open
Low <= Close
Low <= High
```

Reject obviously invalid records.

Do not silently modify invalid market data.

Log rejected records safely.

---

# 9. TIMESTAMP VALIDATION

Validate:

* timestamp exists
* timestamp is UTC/normalized
* timestamp is within requested range
* timestamp is aligned with the requested timeframe where appropriate

For example, an H1 candle should not have an arbitrary minute offset if MT5 defines the candle boundaries differently.

Do not blindly "round" timestamps.

Understand the provider's candle semantics first.

---

# 10. CANDLE COMPLETENESS

Distinguish between:

```text
Completed candle
```

and:

```text
Currently forming candle
```

This distinction is extremely important for later technical analysis and backtesting.

The pipeline should provide a clear way for callers to request:

```text
Completed candles only
```

or, where appropriate:

```text
Include current/forming candle
```

Do not accidentally treat a currently forming candle as historical final data.

---

# 11. LAST COMPLETED CANDLE

Implement a reliable concept of:

```text
LastCompletedCandle
```

This will later be important for:

* indicators
* strategies
* signals
* backtesting
* AI analysis

Do not allow a partially formed candle to silently become the basis of historical analysis.

---

# 12. GAP DETECTION

Implement market-data gap detection.

Example:

```text
10:00
10:05
10:10
10:20
```

For M5:

```text
10:15
```

is missing.

The system should be able to detect that.

Do not assume every missing timestamp automatically represents a data error.

Markets have:

* weekends
* holidays
* broker-specific trading sessions
* market closures
* outages

Therefore, gap detection must understand expected market availability as much as practical.

Do not fill missing candles with fake values.

---

# 13. NO FAKE MARKET DATA

Never create fake candles such as:

```text
Open = previous close
High = previous close
Low = previous close
Close = previous close
```

just to remove a gap.

A missing candle is better than fabricated financial data.

The system should mark/report the gap and attempt recovery when appropriate.

---

# 14. RECOVERY

If a synchronization request fails:

```text
MT5 unavailable
Network failure
Timeout
Temporary error
```

the pipeline should:

* record the failure
* preserve the last known synchronization state
* retry when appropriate
* avoid duplicate data
* avoid corrupting the database
* provide useful logs

Do not retry forever.

Use a bounded retry policy with appropriate delay/backoff.

---

# 15. SYNCHRONIZATION STATE

Create a mechanism to track synchronization state.

For example:

```text
Symbol
Timeframe
LastSuccessfulSync
LastRequestedRange
LastStoredCandle
LastAttempt
Status
Error
```

This allows the system to know where it stopped.

Do not rely only on application logs.

Important synchronization state should be persisted when appropriate.

---

# 16. SYNC STATUS

Expose safe synchronization status.

For example:

```text
XAUUSD M5

Status: Healthy
Last candle: 2026-10-01 13:30 UTC
Last successful sync: 2026-10-01 13:31 UTC
Stored candles: ...
Detected gaps: 0
```

Do not expose internal credentials.

---

# 17. SCHEDULING

Implement a basic mechanism for regular synchronization.

The pipeline should be able to periodically check for new completed candles.

Do not create an overly complicated distributed scheduler.

For the current single-application architecture, a hosted/background service is acceptable if it fits the existing architecture.

The system should:

```text
Start
 ↓
Initialize
 ↓
Check latest data
 ↓
Request missing candles
 ↓
Validate
 ↓
Persist
 ↓
Wait
 ↓
Repeat
```

Use configurable intervals.

---

# 18. MULTIPLE TIMEFRAMES

Do not treat each timeframe as a completely separate system.

Use one reusable synchronization mechanism:

```text
MarketDataSyncService
```

with timeframe-specific configuration.

Conceptually:

```text
XAUUSD
 ├── M1
 ├── M5
 ├── M15
 ├── M30
 ├── H1
 ├── H4
 └── D1
```

Avoid copy-pasting seven separate synchronization implementations.

---

# 19. DATA RANGE REQUESTS

Create a reusable mechanism for:

```text
Get historical range
Get latest range
Get missing range
Get recent completed candles
```

Do not build separate implementations for every use case if the underlying logic is the same.

---

# 20. BATCHING

Use batch processing.

For large historical ranges:

```text
Requested range
      ↓
Batch 1
      ↓
Validate
      ↓
Persist
      ↓
Batch 2
      ↓
Validate
      ↓
Persist
      ↓
...
```

Do not load millions of candles into memory unnecessarily.

Choose a sensible batch size based on actual MT5/API/database behavior.

Make the batch size configurable if useful.

---

# 21. DATABASE WRITES

Avoid inefficient:

```text
INSERT
INSERT
INSERT
INSERT
...
```

for every candle when the infrastructure can efficiently process batches.

Use appropriate batch/bulk persistence.

However, do not introduce an unnecessary third-party bulk framework simply because it sounds faster.

Use the simplest approach that provides good performance and maintainability.

---

# 22. TRANSACTION SAFETY

A failed batch must not leave the database in an inconsistent state.

For example:

```text
Batch 1 → successful
Batch 2 → failed
Batch 3 → never started
```

The system should retain Batch 1 and be able to resume Batch 2.

Do not roll back the entire historical synchronization unnecessarily.

---

# 23. IDEMPOTENCY

Running the same synchronization twice must produce the same final dataset.

Example:

```text
Run 1:
1000 candles stored

Run 2:
same requested 1000 candles

Result:
still 1000 unique candles
```

Not:

```text
2000 candles
```

This is a critical requirement.

---

# 24. DATA QUALITY METADATA

Where useful, preserve metadata about ingestion.

For example:

```text
Source
Provider
FetchedAtUtc
SyncRunId
```

This can help later determine:

```text
Where did this data come from?
When did we retrieve it?
Which synchronization run created it?
```

Do not add unnecessary metadata to every record if it creates excessive storage overhead.

Choose a sensible design.

---

# 25. SYNC RUN

Create a concept of a synchronization run if useful.

For example:

```text
SyncRun
---------
Id
StartedAtUtc
CompletedAtUtc
Symbol
Timeframe
RequestedFrom
RequestedTo
RecordsReceived
RecordsInserted
RecordsSkipped
RecordsRejected
Status
Error
```

This provides operational observability.

Keep the design simple.

---

# 26. DATA QUERY SERVICE

Create a clean service for reading normalized market data.

It should support queries such as:

```text
Get latest candles
Get candles by time range
Get candles by timeframe
Get latest completed candle
Get data availability
Get gaps
```

This service will later become the input for Phase 6.

Do not calculate RSI/EMA/etc. inside it.

---

# 27. MARKET DATA API

Expose only useful read endpoints.

For example:

```text
GET /api/market-data/XAUUSD/candles
GET /api/market-data/XAUUSD/latest
GET /api/market-data/XAUUSD/status
```

Use the existing API conventions.

Validate:

```text symbol
timeframe
from
to
limit
```

Do not allow an unrestricted query such as:

```text
Give me every candle ever stored
```

without pagination or a controlled range.

---

# 28. PAGINATION / LIMITS

Market-data endpoints must have sensible limits.

For example:

```text
limit <= configured maximum
```

A request for:

```text
10,000,000 candles
```

must not cause the server to allocate an enormous amount of memory.

Use pagination or time-range constraints.

---

# 29. CACHING

Do NOT aggressively cache the entire market database in Redis.

SQL Server remains the source of truth.

Redis may later cache:

```text
latest quote
latest completed candles
frequently requested small ranges
```

Only introduce caching where measurement shows it helps.

Do not create cache complexity just for the sake of using Redis.

---

# 30. OBSERVABILITY

Add useful metrics/logging such as:

```text
Candles requested
Candles received
Candles inserted
Candles duplicated
Candles rejected
Sync duration
Last successful synchronization
Gap count
MT5 request failures
```

Do not log every individual candle.

The goal is to understand pipeline health.

---

# 31. CONCURRENCY

Prevent two synchronization processes from simultaneously trying to synchronize the exact same:

```text
Symbol + Timeframe + Range
```

unless the architecture explicitly supports safe concurrent synchronization.

Avoid race conditions such as:

```text
Sync A → sees missing candle
Sync B → sees same missing candle
Sync A → inserts
Sync B → inserts
```

Database uniqueness constraints remain the final protection.

---

# 32. MARKET HOURS

Do not build a simplistic assumption that XAUUSD trades continuously with no interruptions.

Account for:

* weekends
* market closures
* broker trading sessions
* holidays
* temporary data gaps

Do not fabricate data during closed periods.

Keep market-session logic extensible for later improvement.

---

# 33. DATA RETENTION

Do not automatically delete old market data.

Historical data will later be required for:

* backtesting
* statistics
* AI historical context
* strategy evaluation

If retention becomes necessary due to storage constraints, implement it deliberately in a future phase.

---

# 34. OFFLINE BEHAVIOR

If the PC or MT5 is offline:

```text
Application
    ↓
MT5 unavailable
```

the system should not corrupt existing data.

The database should preserve the last successful state.

Do not implement the complete Phase 17 offline catch-up system yet.

However, the pipeline must leave enough information for Phase 17 to determine:

```text
What data is missing?
What period needs to be recovered?
```

---

# 35. IMPORTANT: NO ANALYSIS YET

Do NOT calculate:

```text
RSI
EMA
SMA
MACD
ATR
ADX
Bollinger Bands
Stochastic
Candlestick patterns
Trend
Support/resistance
Market structure
```

Those belong to:

# PHASE 6 — TECHNICAL ANALYSIS ENGINE

Phase 5 only supplies clean market data.

---

# 36. IMPORTANT: NO AI YET

Do NOT call OpenAI.

Do not send market data to AI.

Do not generate:

* predictions
* reasoning
* confidence
* signals
* recommendations

AI begins in later phases.

---

# 37. IMPORTANT: NO TRADING

Do not implement:

```text
Buy
Sell
Order
Position
Trade execution
Stop-loss execution
Take-profit execution
```

The pipeline is strictly market-data infrastructure.

---

# 38. TESTING

Create comprehensive tests.

## Unit tests

Test:

```text
[ ] Candle validation
[ ] OHLC validation
[ ] Timeframe validation
[ ] Timestamp normalization
[ ] Symbol mapping
[ ] Completed-candle detection
[ ] Gap detection
[ ] Duplicate detection
[ ] Sync-state logic
[ ] Retry logic
[ ] Batch logic
```

## Integration tests

Test:

```text
[ ] MT5 → pipeline
[ ] Pipeline → SQL Server
[ ] Duplicate synchronization
[ ] Incremental synchronization
[ ] Failed synchronization
[ ] Resume after failure
[ ] Multiple timeframes
```

## API tests

Test:

```text
[ ] Latest candles
[ ] Historical range
[ ] Invalid timeframe
[ ] Invalid date range
[ ] Excessive limit
[ ] Empty result
[ ] Status endpoint
```

Do not use real credentials in ordinary automated tests.

---

# 39. PERFORMANCE TESTING

Test a realistic historical dataset.

Measure:

```text
Synchronization time
Database write time
Memory usage
Query time
CPU usage
```

Do not optimize purely based on assumptions.

Record the results.

If performance is already sufficient, do not add unnecessary complexity.

---

# 40. FAILURE TESTING

Actually test:

```text
MT5 unavailable
Wrong credentials
Wrong terminal path
Invalid symbol
Timeout
Database unavailable
Duplicate data
Interrupted synchronization
Application restart during synchronization
```

The pipeline should recover safely where possible.

---

# 41. RESTART TEST

This is especially important.

Test:

```text
Application starts
    ↓
Synchronization begins
    ↓
Application stops unexpectedly
    ↓
Application starts again
    ↓
Synchronization resumes
```

Verify:

* no duplicate candles
* no corrupted synchronization state
* missing data can be detected
* existing data remains intact

---

# 42. DOCUMENTATION

Create/update:

```text
docs/market-data-pipeline.md
```

Document:

* architecture
* synchronization flow
* initial history
* incremental sync
* gap detection
* retries
* synchronization state
* data validation
* database persistence
* API endpoints
* configuration
* troubleshooting

Also update the main README where appropriate.

---

# 43. CONFIGURATION

Use the Phase 2 configuration system.

Potential configuration:

```env
MARKET_DATA_SYNC_ENABLED=true
MARKET_DATA_SYMBOL=XAUUSD
MARKET_DATA_TIMEFRAMES=M1,M5,M15,M30,H1,H4,D1
MARKET_DATA_INITIAL_HISTORY_DAYS=30
MARKET_DATA_SYNC_INTERVAL_SECONDS=60
MARKET_DATA_BATCH_SIZE=1000
MARKET_DATA_MAX_API_LIMIT=5000
```

These are examples.

Use names that fit the existing configuration architecture.

Do not create configuration for features that are not implemented.

---

# 44. SECURITY

Continue protecting:

```text
MT5_LOGIN
MT5_PASSWORD
MT5_SERVER
OPENAI_API_KEY
FRED_API_KEY
NEWSDATA_API_KEY
```

No secrets in:

* logs
* API responses
* database records unless explicitly required
* frontend code
* Git
* documentation
* screenshots

---

# 45. DEFINITION OF DONE

Phase 5 is complete only when:

* [ ] Initial historical synchronization works
* [ ] Incremental synchronization works
* [ ] Multiple supported timeframes work
* [ ] XAUUSD data is normalized
* [ ] Candle validation works
* [ ] Duplicate protection works
* [ ] Gap detection works
* [ ] Missing data is not fabricated
* [ ] Synchronization state is tracked
* [ ] Failed synchronization can recover
* [ ] Retry behavior is bounded
* [ ] Batch processing works
* [ ] Database writes are efficient
* [ ] Restart recovery works
* [ ] Market-data queries work
* [ ] API endpoints work
* [ ] Query limits exist
* [ ] Observability exists
* [ ] Unit tests pass
* [ ] Integration tests pass
* [ ] Failure tests pass
* [ ] Performance testing was performed
* [ ] Documentation is updated
* [ ] No technical-analysis logic was added
* [ ] No AI calls were added
* [ ] No trading execution was added

---

# 46. FINAL REPORT

After implementation, provide:

1. Files created
2. Files modified
3. Market-data architecture
4. Synchronization architecture
5. Database changes
6. Supported timeframes
7. Initial synchronization result
8. Incremental synchronization result
9. Gap-detection result
10. Duplicate-handling result
11. Restart/recovery test result
12. Performance measurements
13. Tests executed
14. Test results
15. Any issue requiring my action
16. Confirmation that Phase 5 is complete
17. Short explanation of what Phase 6 will build

Do not claim a test passed unless it was actually executed.

Do not implement Phase 6 early.

# END OF PHASE 5
