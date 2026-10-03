# XAUUSD AI Analyst Trading System

[![CI](https://github.com/GenXXIII/AnalystTradingSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/GenXXIII/AnalystTradingSystem/actions/workflows/ci.yml)

This repository is an independent analyst application for XAUUSD. The web
console, application workflows, normalized data contracts, persistence,
analysis rules, audit trail, and future signal validation belong to this
system.

MT5, NewsData.io, FRED, and OpenAI are external providers. They connect through
replaceable Infrastructure adapters; they do not define the architecture or
the user interface.

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
  |          |           |          |
 MT5       FRED      NewsData.io   OpenAI
Phase 4   Phase 8      Phase 7      future
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
- MetaTrader 5 Desktop on Windows for live Phase 4 data
- Python with the official MetaTrader5 package

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

MT5 requires `MT5_LOGIN`, `MT5_PASSWORD`, `MT5_SERVER`,
`MT5_TERMINAL_PATH`, and the broker's `MT5_SYMBOL`. The application symbol
remains `XAUUSD` even when the provider symbol has a broker suffix.

## Run the Phase 7 terminal with live MT5 data

The official MT5 Python integration talks to the installed Windows terminal.
The standard startup command therefore keeps SQL Server, Redis, and the web
console in Docker while supervising the MT5-connected API on Windows:

```powershell
./scripts/start-local.ps1
```

This one command starts MT5 when it is closed, reopens it if its process exits,
keeps the Windows API running on port 5081, and starts the web console on port
3001. Runtime logs are written under the ignored `logs/` directory without
credentials. Stop the supervised API and web console with
`./scripts/stop-local.ps1`; SQL Server and Redis remain available.

Open <http://localhost:3001>. The root route opens directly into the analyst
terminal. The interactive chart supports zoom, pan, crosshair inspection,
fit/reset, jump-to-live, and fullscreen while the rest of the application owns
the analysis and news workflows. MT5 stays behind the API and has no UI role.

A Linux API container cannot operate a Windows MT5 terminal. The standard local
startup intentionally stops that container and hosts the live API on Windows.

## Market-data endpoints

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
