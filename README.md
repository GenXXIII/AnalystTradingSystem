# XAUUSD AI Analyst Trading System

[![CI](https://github.com/GenXXIII/AnalystTradingSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/GenXXIII/AnalystTradingSystem/actions/workflows/ci.yml)

This repository is an independent analyst application for XAUUSD. The web
console, application workflows, normalized data contracts, persistence,
analysis rules, audit trail, and future signal validation belong to this
system.

AllTick, Twelve Data, MT5, NewsData.io, FRED, and OpenAI are external providers. They connect through
replaceable Infrastructure adapters; they do not define the architecture or
the user interface.

AllTick is the default market-data adapter. It runs inside the API container,
streams live `GOLD` ticks and bid/ask quotes over WebSocket, and uses official
REST candles for reconciliation. Twelve Data independently supplies historical
M1 reference candles, which the application normalizes and aggregates into M5,
M15, M30, H1, H4, and D1. MT5 remains an optional Windows-host fallback.

Phase 7 adds a provider-isolated NewsData.io collection pipeline with
normalization, deterministic XAUUSD relevance, duplicate-safe SQL persistence,
incremental state, bounded retry/paging, and application-owned news APIs. It
does not call AI, score sentiment, infer causality, emit BUY/SELL signals, place
trades, or claim profitability.

Phase 8 adds a provider-isolated FRED economic-data pipeline with focused
XAUUSD macro series, revision-aware SQL persistence, incremental state,
bounded retries/paging, and application-owned APIs. It stores facts only and
does not perform macro interpretation or signal generation.

## Architecture

```text
Analyst web console
        |
XauAi HTTP API
        |
Application contracts and use cases
        |
Domain model + SQL persistence
        ^
Infrastructure provider adapters
  |                          |          |           |          |
AllTick + Twelve Data/MT5   FRED      NewsData.io   OpenAI
market data                  Phase 8      Phase 7      future
```

- `Domain` is framework- and provider-independent.
- `Application` owns normalized contracts and use cases.
- `Infrastructure` implements SQL and external-provider adapters.
- `Api` exposes internal application contracts over HTTP.
- `frontend/xau-ai-web` is the owned operational interface.

See [system overview](docs/architecture/system-overview.md),
[provider boundaries](docs/architecture/provider-integrations.md),
[database foundation](docs/architecture/database-foundation.md), and
[configuration](docs/development/configuration.md). The completed flows are
documented in [market-data pipeline](docs/market-data-pipeline.md),
[technical analysis](docs/technical-analysis.md), and
[news collection](docs/news-collection.md). Phase 8 is documented in
[economic-data architecture](docs/architecture/economic-data.md) and the
[economic-data development guide](docs/development/economic-data.md).

## Local ports

| Service | Host | Container |
| --- | --- | --- |
| Analyst web console | `http://localhost:3001` | `3000` |
| API / Swagger | `http://localhost:5081` | `8080` |
| SQL Server | `localhost:14330` | `1433` |
| Redis | `localhost:6380` | `6379` |

## Prerequisites

- .NET SDK 10
- Node.js 24 or newer and npm
- Docker Desktop with Docker Compose
- Free AllTick and Twelve Data API keys for the dual-source Docker market-data flow
- Optional: MetaTrader 5 Desktop and Python on Windows for the MT5 fallback

Install the pinned Python dependency:

```powershell
py -3 -m pip install -r scripts/mt5/requirements.txt
```

## Configuration

The ignored `.env` contains real local values. `.env.example` contains guide
names only for host ports, URLs, and secrets. Never commit `.env`.

The current local values use:

```text
WEB_PORT=3001
API_PORT=5081
SQLSERVER_PORT=14330
REDIS_PORT=6380
NEXT_PUBLIC_API_URL=http://localhost:5081
CORS_ALLOWED_ORIGINS=http://localhost:3001
```

The database values must be one consistent set:

- `MSSQL_SA_PASSWORD` starts and administers the local SQL container.
- `DATABASE_APP_PASSWORD` belongs to the restricted `xauai_app` login.
- `DATABASE_CONNECTION_STRING` uses that restricted login.

For the default provider, set `ALLTICK_TOKEN` and keep
`MARKET_DATA_PROVIDER=AllTick`, `MARKET_DATA_PROVIDER_KEY=alltick`. The
application symbol `XAUUSD` maps to AllTick's provider code `GOLD`.

Set `TWELVE_DATA_API_KEY` and keep `TWELVE_DATA_ENABLED=true` to use Twelve
Data's separate free quota for history, backup/reference candles, and
cross-provider quality checks. Its provider symbol is `XAU/USD`. The two
providers are stored separately and are never silently blended.

The optional MT5 fallback requires `MARKET_DATA_PROVIDER=MT5`,
`MARKET_DATA_PROVIDER_KEY=mt5`, `MT5_LOGIN`, `MT5_PASSWORD`, `MT5_SERVER`,
`MT5_TERMINAL_PATH`, and the broker's `MT5_SYMBOL`.

## Run the dual-source market-data flow in Docker

Copy the example environment, replace the database password placeholders,
`ALLTICK_TOKEN`, and `TWELVE_DATA_API_KEY`, then start the stack:

```powershell
Copy-Item .env.example .env
docker compose up -d --build
```

AllTick needs no desktop terminal, Python bridge, or Windows host. The API keeps
one WebSocket connection with the required heartbeat, builds live provisional
candles, and reconciles them with provider candles before they become eligible
for completed-candle analysis. Twelve Data uses REST only in this design: it
backfills M1 in bounded chunks, refreshes the recent reference window every 15
minutes, and builds all higher timeframes locally.

Open <http://localhost:3001>. The root route opens directly into the analyst
terminal. The interactive chart supports zoom, pan, crosshair inspection,
fit/reset, jump-to-live, and fullscreen while the rest of the application owns
the analysis and news workflows. The provider stays behind the API and has no
UI or trade-execution role.

To use MT5 instead, configure the fallback variables and run
`./scripts/start-local.ps1`; a Linux container cannot operate a Windows MT5
terminal.

## Market-data endpoints

The dual-source quality endpoint is
`GET /api/market-data/{symbol}/source-comparison?timeframe=M1&limit=100`.

- `GET /api/market-data/provider/status` — safe selected-provider status

- `GET /health` — application and provider health
- `GET /api/system/status` — API connectivity contract
- `GET /api/mt5/status` — safe market-provider status (no password)
- `GET /api/market/xauusd/quote` — normalized current bid/ask
- `GET /api/market/xauusd/candles?timeframe=H1&from=...&to=...` — normalized UTC candles
- `POST /api/market/xauusd/candles/sync` — incremental, duplicate-safe persistence
- `GET /api/market-data/{symbol}/candles` — normalized stored UTC range
- `GET /api/market-data/{symbol}/latest` — bounded latest candles
- `GET /api/market-data/{symbol}/last-completed` — reliable last completed candle
- `GET /api/market-data/{symbol}/status` — availability and persistent sync state
- `GET /api/market-data/{symbol}/gaps` — market-aware candidate gaps
- `POST /api/market-data/{symbol}/sync` — validated, batched synchronization
- `GET /api/analysis/{symbol}/{timeframe}` — deterministic single-timeframe evidence
- `GET /api/analysis/{symbol}/multi-timeframe` — cross-timeframe agreement and conflicts
- `GET /api/news/` — paginated normalized news from SQL
- `GET /api/news/latest` — relevant articles from the last 24 hours
- `GET /api/news/relevant` — explicit minimum-relevance query
- `GET /api/news/{id}` — normalized article and source traceability
- `GET /api/news/status` — safe provider/collection status and stored count
- `POST /api/news/collect` — bounded incremental collection
- `GET /api/economic-data/series` — normalized configured series from SQL
- `GET /api/economic-data/series/{id}/observations` — bounded date-range observations
- `GET /api/economic-data/latest` — latest locally stored value per series
- `GET /api/economic-data/status` — safe provider and per-series sync status
- `POST /api/economic-data/synchronize` — bounded incremental FRED synchronization
- `/swagger` — interactive OpenAPI documentation in Development

All responses carry a correlation ID. Provider failures use stable, safe error
codes and never expose credentials or stack traces.

## Verification

```powershell
dotnet build XauAi.slnx --configuration Release
dotnet test tests/XauAi.UnitTests --configuration Release
dotnet test tests/XauAi.IntegrationTests --configuration Release
dotnet test tests/XauAi.ArchitectureTests --configuration Release

Set-Location frontend/xau-ai-web
npm run lint
npm run build
```

Database, live MT5, live NewsData, and live FRED tests are opt-in. Their setup is
documented in [provider boundaries](docs/architecture/provider-integrations.md)
and [news collection](docs/news-collection.md).

The live AllTick history check is also opt-in: set `ALLTICK_TOKEN` and
`XAUAI_RUN_ALLTICK_INTEGRATION=true`, then run the integration-test project.
The Twelve Data check is opt-in with `TWELVE_DATA_API_KEY` and
`XAUAI_RUN_TWELVE_DATA_INTEGRATION=true`.

## Continuous integration

The [GitHub Actions workflow](.github/workflows/ci.yml) validates the complete
repository on pushes to `main`, pull requests, and manual runs. It performs the
Release build, unit and architecture tests, formatting and EF migration checks,
SQL Server-backed integration tests, frontend lint/build, and a clean Docker
Compose smoke test for SQL Server, Redis, the API, and the web application.

Normal CI never calls paid or credentialed external providers. Manual workflow
runs can opt into the live FRED or NewsData.io integration tests after the
corresponding `FRED_API_KEY` or `NEWS_API_KEY` repository secret is configured.
The live MT5 test remains local because it requires the installed Windows MT5
terminal and broker session.
