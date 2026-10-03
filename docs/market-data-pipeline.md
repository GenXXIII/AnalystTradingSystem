# Market data pipeline

## Purpose and boundary

Phase 5 turns the read-only MT5 adapter into a persistent and recoverable
market-data system. SQL Server is the historical source of truth. MT5 remains a
replaceable provider at the Infrastructure edge.

This phase does not calculate indicators, call AI models, create signals, or
place trades. The web terminal is an owned analyst interface; it is not an MT5
screen and exposes no order controls.

## Architecture

```text
MT5 Desktop
    |
Mt5MarketDataProvider
    |
MarketDataSynchronizationService
    |-- bounded acquisition batches
    |-- UTC and OHLC validation
    |-- forming/completed classification
    |-- in-batch deduplication
    |-- bounded retry and per-series lock
    |-- candidate-gap detection
    |
EF Core stores
    |-- MarketCandles
    |-- MarketDataSyncStates
    |-- MarketDataSyncRuns
    |
SQL Server
    |
MarketDataQueryService -> HTTP API -> analyst web terminal
```

Application owns provider-neutral contracts and orchestration. Infrastructure
owns MT5 and EF Core implementations. Domain owns the persisted entities. The
API and frontend consume normalized records only.

## Synchronization flow

One reusable synchronization service handles M1, M5, M15, M30, H1, H4, and D1.
Symbol and timeframe form part of every series identity.

For an empty series, the service aligns the configured initial-history start to
the timeframe and requests at most `InitialHistoryDays`. For an existing series,
it starts after the latest completed candle. If the latest stored candle is
forming, that same open time is requested again so it can become completed.

Each range is divided into bounded batches. A completed batch commits before
the next batch starts. If a later batch fails, earlier commits remain valid and
the next run resumes from the latest stored cursor. The exact final candle on a
batch boundary is requested with a one-second provider envelope so it is not
omitted while preserving the logical requested range.

Repeated and overlapping requests are safe at two levels:

- Application deduplicates each provider batch by open time.
- SQL Server enforces one row per instrument, timeframe, provider, and open time.

An existing completed candle is immutable to ingestion. An existing forming
candle may be updated by a later provider observation.

## Validation and normalization

Before persistence, every candle must have:

- the configured application symbol and requested timeframe;
- a non-empty provider symbol;
- an open time inside the requested range;
- a close time exactly one timeframe after its open;
- an open time aligned to the timeframe boundary;
- positive OHLC prices and valid high/low relationships;
- non-negative volume and spread values.

Offsets are normalized to UTC. The pipeline derives completeness from
`CloseTimeUtc <= ObservedAtUtc`; it does not trust a provider completeness flag
as historical truth. Invalid records are rejected without price or timestamp
repair. Logs aggregate rejection codes per batch and never log every candle.

## Completed candles and gaps

Read APIs accept `completedOnly`. `/last-completed` always queries completed
records and never substitutes the current forming candle.

Gap detection walks the expected timeframe boundaries and compares them with
stored open times. The current XAUUSD calendar excludes the observed broker
maintenance window from 20:58 through 22:01 UTC and the weekend closure from
Friday 20:58 through Sunday 22:01 UTC. Holiday and broker-session calendars
remain extensible because those hours can change. A reported gap is classified
`UnverifiedMissing`; it is a review/recovery candidate, not proof that the
provider is wrong. No candle is fabricated to hide a gap.

## Failure, retry, and restart recovery

Transient provider errors—timeout, connection, initialization, data request,
or temporary unavailability—use configurable exponential backoff and stop
after `MaxRetries`. Authentication, invalid symbol, and invalid request errors
are not repeatedly retried.

Every attempt creates a `MarketDataSyncRun`. The persistent state stores the
last attempt, successful time, requested range, last stored candle, consecutive
failure count, candidate-gap count, and safe error. Starting a new run marks
any previous `Running` run for that series as `Interrupted`. Existing candles
remain intact, and the latest stored cursor determines the recovery point.

An in-process semaphore serializes the same symbol/timeframe series. The SQL
unique key remains the final race-condition safeguard. The current
single-application architecture intentionally does not introduce a distributed
scheduler.

## Scheduling

`MarketDataSynchronizationWorker` runs only when
`MARKET_DATA_SYNC_ENABLED=true`. It synchronizes every configured timeframe,
then waits `MARKET_DATA_SYNC_INTERVAL_SECONDS`. A failure in one timeframe is
logged safely and does not prevent later timeframes from running.

## Database persistence

Phase 5 adds:

- `MarketDataSyncStates`: one current state per instrument/timeframe/provider.
- `MarketDataSyncRuns`: durable per-run metrics and outcome history.

`MarketCandles` remains the normalized data table. Each batch uses one EF Core
`SaveChanges` transaction, not one transaction for the entire historical run.
There is no automatic retention or market-data deletion.

## Read and control API

All endpoints use the existing success/error and correlation-ID contracts.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `GET` | `/api/market-data/{symbol}/candles` | Controlled UTC range; requires timeframe/from/to and bounded limit |
| `GET` | `/api/market-data/{symbol}/latest` | Latest bounded records; supports `completedOnly` |
| `GET` | `/api/market-data/{symbol}/last-completed` | Latest completed candle only |
| `GET` | `/api/market-data/{symbol}/status` | Availability and persistent synchronization state |
| `GET` | `/api/market-data/{symbol}/gaps` | Candidate gaps for a controlled range |
| `POST` | `/api/market-data/{symbol}/sync` | Manual bounded synchronization |

Limits are enforced by `MaxApiLimit` and `MaxQueryRangeDays`. Unsupported
symbols/timeframes, reversed ranges, and excessive requests return safe errors.

## Configuration

| Environment variable | Default | Purpose |
| --- | ---: | --- |
| `MARKET_DATA_SYNC_ENABLED` | `false` in committed app settings | Enable the background worker |
| `MARKET_DATA_SYMBOL` | `XAUUSD` | Provider-neutral application symbol |
| `MARKET_DATA_TIMEFRAMES` | `M1,M5,M15,M30,H1,H4,D1` | Enabled series |
| `MARKET_DATA_INITIAL_HISTORY_DAYS` | `7` | Bounded first synchronization |
| `MARKET_DATA_SYNC_INTERVAL_SECONDS` | `10` | Delay between full worker cycles |
| `MARKET_DATA_BATCH_SIZE` | `1000` | Maximum logical candles per batch |
| `MARKET_DATA_MAX_API_LIMIT` | `5000` | Maximum records returned by one read |
| `MARKET_DATA_MAX_QUERY_RANGE_DAYS` | `366` | Maximum API/sync range |
| `MARKET_DATA_MAX_RETRIES` | `2` | Retries after the first transient attempt |
| `MARKET_DATA_RETRY_BASE_DELAY_SECONDS` | `2` | Exponential backoff base |
| `MARKET_DATA_MAX_GAP_RESULTS` | `1000` | Candidate-gap result cap |
| `MARKET_DATA_INCLUDE_FORMING_CANDLE` | `true` | Persist the labeled forming candle for live chart updates |

The ignored local `.env` enables the Phase 5 worker. Credentials remain only in
`.env` and are not exposed through these options.

## Measured performance

The SQL Server integration performance test used 10,000 deterministic M1
candles, ten 1,000-candle batches, the real EF stores, and a temporary SQL Server
database. On the local development machine on 2026-10-02:

| Measurement | Result |
| --- | ---: |
| Synchronization and writes | 6,289 ms |
| Query of 10,000 completed candles | 121 ms |
| Process CPU during measured work | 5,531 ms |
| Working-set increase | 52.6 MB |
| Duplicate rows | 0 |

These results are development evidence, not a production service-level
objective. The current simple EF batching is sufficient for this phase.

## Troubleshooting

- `MT5_TERMINAL_NOT_FOUND`: confirm the absolute Windows `terminal64.exe` path.
- `MT5_AUTHENTICATION_FAILED`: verify login, password, and broker server in the
  ignored `.env`; do not paste them into logs or screenshots.
- `MT5_TIMEOUT` or connection failure: keep stored data, check terminal/network,
  and allow the bounded next retry/cycle.
- `DATABASE_DISABLED`: enable SQL persistence and supply the restricted app
  connection string.
- `MARKET_DATA_LIMIT_EXCEEDED`: lower `limit` to the configured maximum.
- `MARKET_DATA_RANGE_TOO_LARGE`: split the request into controlled ranges.
- Candidate gaps: check broker session/holiday availability before treating a
  gap as an outage. Never insert a synthetic candle.

For live MT5, run the API on Windows using `scripts/start-mt5-api.ps1`; the
Linux API container cannot communicate with the installed Windows terminal.
