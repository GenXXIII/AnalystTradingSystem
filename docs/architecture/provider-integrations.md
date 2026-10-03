# Provider integration boundaries

## Application ownership

The XAUUSD AI Analyst Trading System is the product and the architecture. It
owns:

- the operational web interface and analyst workflow;
- normalized domain and application models;
- evidence persistence and deduplication;
- deterministic calculations, backtests, and performance statistics;
- evidence traceability, risk rules, and future signal validation.

AllTick, MT5, NewsData.io, FRED, and OpenAI are providers at the Infrastructure edge.
Provider SDK objects, field names, credentials, and UI concepts must not leak
into Domain or Application.

| Provider | Supplied capability | Internal boundary | Status |
| --- | --- | --- | --- |
| AllTick | Live quotes/ticks and candles | `IMarketDataProvider` | Default Docker-native market adapter |
| MT5 | Quotes and candles | `IMarketDataProvider` | Optional Windows-host fallback |
| NewsData.io | News evidence | `INewsProvider` | Phase 7 adapter and pipeline active |
| FRED | Macroeconomic observations | `IEconomicDataProvider` | Phase 8 adapter and pipeline active |
| OpenAI | Evidence-grounded reasoning | Future AI provider contract | Future phase |

Replacing a provider should require a new Infrastructure adapter, configuration,
and mapping tests—not a rewrite of the web console or core use cases.

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
AllTickMarketDataProvider       EF candle store
        |                         |
WebSocket + REST              SQL Server :14330
```

The synchronization service validates, batches, deduplicates, detects candidate
gaps, records durable run/state information, and persists normalized candles.
The default adapter maps internal `XAUUSD` to AllTick `GOLD`. WebSocket ticks
keep provisional candles live; official REST candles reconcile and complete
them before analysis consumes them. No adapter exposes order-send,
position-modification, or close-trade operations.

## Data rules

- All request ranges and persisted timestamps are UTC.
- Supported timeframes are M1, M5, M15, M30, H1, H4, and D1.
- Bid and ask are current quote fields; spread is derived from provider data.
- Tick volume and real volume remain separate nullable values.
- A candle is uniquely identified by instrument, timeframe, open time, and provider.
- A repeated overlapping sync skips complete duplicates and may update an incomplete candle.
- Provider credentials are server-only and never returned, logged, or stored in market records.

## Optional MT5 Windows runtime

The Python MetaTrader5 package communicates with MetaTrader 5 Desktop. The live
adapter therefore runs on Windows where `terminal64.exe` is installed. A Linux
Docker API cannot access that Windows terminal directly.

Install the pinned bridge dependency:

```powershell
py -3 -m pip install -r scripts/mt5/requirements.txt
```

With SQL Server running in Docker and the ignored `.env` configured:

```powershell
docker compose stop api
./scripts/start-mt5-api.ps1
```

The startup probe reports a safe provider state. Expected failure states include
disabled, terminal missing, invalid configuration, authentication failure, and
connection failure. None includes the configured password.

## Opt-in live verification

Set the MT5 environment values and a SQL administrator test connection in the
current process, then run:

```powershell
$env:XAUAI_RUN_MT5_INTEGRATION = "true"
$env:XAUAI_TEST_SQLSERVER_CONNECTION_STRING = "YOUR_SQL_TEST_CONNECTION"
dotnet test tests/XauAi.IntegrationTests `
  --configuration Release `
  --filter FullyQualifiedName~Mt5LiveIntegrationTests
```

The test creates a temporary database, connects to the live terminal, retrieves
a quote, synchronizes H1 and M15 candles, repeats and incrementally extends H1,
verifies UTC storage, state health, and idempotence, then deletes the database.

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
