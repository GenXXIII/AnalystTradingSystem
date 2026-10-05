# Provider integration boundaries

## Application ownership

The XAUUSD AI Analyst Trading System is the product and the architecture. It
owns:

- the operational web interface and analyst workflow;
- normalized domain and application models;
- evidence persistence and deduplication;
- deterministic calculations, backtests, and performance statistics;
- evidence traceability, risk rules, and future signal validation.

AllTick, Twelve Data, NewsData.io, FRED, configured analyst RSS/Atom sources,
and OpenAI are providers at the Infrastructure edge.
Provider SDK objects, field names, credentials, and UI concepts must not leak
into Domain or Application.

| Provider | Supplied capability | Internal boundary | Status |
| --- | --- | --- | --- |
| AllTick | Live quotes/ticks and candles | `IMarketDataProvider` | Default Docker-native market adapter |
| Twelve Data | Historical backup/reference candles | `IReferenceMarketDataProvider` | Independent REST reference adapter |
| NewsData.io | News evidence | `INewsProvider` | Phase 7 adapter and pipeline active |
| FRED | Macroeconomic observations | `IEconomicDataProvider` | Phase 8 adapter and pipeline active |
| Permitted RSS/Atom source | Analyst publications and claims | `IAnalystDataProvider` | Phase 9 adapter; disabled until a legitimate feed is configured |
| OpenAI | Evidence-grounded reasoning | Future AI provider contract | Future phase |

Replacing a provider should require a new Infrastructure adapter, configuration,
and mapping tests—not a rewrite of the web console or core use cases.

## Phase 9 analyst-data flow

```text
Permitted RSS/Atom feed
        |
IAnalystDataProvider adapter
        |
relevance -> conservative claim extraction -> identity/version deduplication
        |
SQL source/analyst/publication/prediction + durable run/state
        |
/api/analyst-* application contracts
```

Reads come from SQL and never refetch a provider feed. Exact republications are
linked and do not become duplicate independent opinions. No AI interpretation,
consensus, accuracy score, or signal is produced. See
[analyst-data architecture](analyst-data.md) and the
[development guide](../development/analyst-data.md).

## Phase 8 economic-data flow

```text
FRED series/observations API
        |
IEconomicDataProvider adapter
        |
bounded backfill/incremental sync -> missing-value normalization -> revision audit
        |
SQL series/observation/revision + durable run/state
        |
/api/economic-data application contracts
```

Reads come from SQL and do not spend FRED requests. See
[economic-data architecture](economic-data.md) and the
[development guide](../development/economic-data.md).

## Phase 7 news flow

```text
NewsData.io latest/archive API
        |
INewsProvider adapter
        |
collection -> normalization -> deterministic relevance -> deduplication
        |
SQL article/evidence/content + durable run/state
        |
/api/news application contracts
```

Provider IDs, canonical URLs, and same-source title/date fingerprints protect
against duplicates while independent sources remain distinct evidence. Reads
come from SQL and do not consume provider requests. Collection is paginated,
rate-aware, incremental, and safely retry-bounded. See
[news collection](../news-collection.md) for configuration, exact rules, API,
cost behavior, and opt-in live verification.

## Phase 5 market-data flow

```text
Analyst console (port 3001)
        |
normalized HTTP JSON
        |
API container (port 5081)
        |
IMarketDataProvider / IMarketDataSynchronizationService
        |
AllTickMarketDataProvider    TwelveDataMarketDataProvider
        |                         |
WebSocket + REST          REST M1 -> local aggregation
        |_________________________|
                    |
             EF candle store
                    |
            SQL Server :14330
```

The synchronization service validates, batches, deduplicates, detects candidate
gaps, records durable run/state information, and persists normalized candles.
The primary adapter maps internal `XAUUSD` to AllTick `GOLD`. WebSocket ticks
keep provisional candles live; official REST candles reconcile and complete
them before analysis consumes them. Twelve Data maps `XAUUSD` to `XAU/USD`,
backfills M1 candles, and locally aggregates the higher timeframes. No adapter exposes order-send,
position-modification, or close-trade operations.

## Data rules

- All request ranges and persisted timestamps are UTC.
- Supported timeframes are M1, M5, M15, M30, H1, H4, and D1.
- Bid and ask are current quote fields; spread is derived from provider data.
- Tick volume and real volume remain separate nullable values.
- A candle is uniquely identified by instrument, timeframe, open time, and provider.
- A repeated overlapping sync skips complete duplicates and may update an incomplete candle.
- Provider credentials are server-only and never returned, logged, or stored in market records.

## Opt-in live verification

NewsData live verification is separately opt-in because it consumes external
provider quota:

```powershell
$env:XAUAI_RUN_NEWSDATA_INTEGRATION = "true"
$env:NEWS_API_KEY = "YOUR_NEWSDATA_API_KEY"
$env:XAUAI_TEST_SQLSERVER_CONNECTION_STRING = "YOUR_SQL_TEST_CONNECTION"
dotnet test tests/XauAi.IntegrationTests `
  --configuration Release `
  --filter FullyQualifiedName~NewsDataLiveIntegrationTests
```

The normal unit/integration suite uses deterministic responses instead.

FRED live verification is separately opt-in:

```powershell
$env:XAUAI_RUN_FRED_INTEGRATION = "true"
$env:FRED_API_KEY = "YOUR_FRED_API_KEY"
$env:XAUAI_TEST_SQLSERVER_CONNECTION_STRING = "YOUR_SQL_TEST_CONNECTION"
dotnet test tests/XauAi.IntegrationTests `
  --configuration Release `
  --filter FullyQualifiedName~FredLiveIntegrationTests
```
