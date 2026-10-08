# XAUUSD AI Analyst Trading System

[![CI](https://github.com/GenXXIII/AnalystTradingSystem/actions/workflows/ci.yml/badge.svg)](https://github.com/GenXXIII/AnalystTradingSystem/actions/workflows/ci.yml)

This repository is an independent analyst application for XAUUSD. The web
console, application workflows, normalized data contracts, persistence,
analysis rules, audit trail, and future signal validation belong to this
system.

AllTick, Twelve Data, NewsData.io, FRED, and configured AI providers are external providers. They connect through
replaceable Infrastructure adapters; they do not define the architecture or
the user interface.

AllTick is the default market-data adapter. It runs inside the API container,
streams live `GOLD` ticks and bid/ask quotes over WebSocket, and uses official
REST candles for reconciliation. Twelve Data independently supplies historical
M1 reference candles, which the application normalizes and aggregates into M5,
M15, M30, H1, H4, and D1. No desktop trading terminal is required.

Phase 7 adds a provider-isolated NewsData.io collection pipeline with
normalization, deterministic XAUUSD relevance, duplicate-safe SQL persistence,
incremental state, bounded retry/paging, and application-owned news APIs. It
does not call AI, score sentiment, infer causality, emit BUY/SELL signals, place
trades, or claim profitability.

Phase 8 adds a provider-isolated FRED economic-data pipeline with focused
XAUUSD macro series, revision-aware SQL persistence, incremental state,
bounded retries/paging, and application-owned APIs. It stores facts only and
does not perform macro interpretation or signal generation.

Phase 9 adds an application-owned analyst-data pipeline with a configurable
permitted RSS/Atom adapter, deterministic XAUUSD relevance and claim extraction,
separate source/analyst identity, immutable publication versions, exact
republication links, incremental SQL state, look-ahead-safe queries, and bounded
APIs. No feed is enabled by default. It does not calculate consensus or analyst
accuracy, call AI, or generate trading signals.

Phase 10 unifies market, technical, news, economic, and analyst records as
normalized evidence with explicit availability time, original attribution,
duplicate-safe identity, deterministic relations/clusters/conflicts, quality
metadata, quarantine, and bounded multi-timeframe evidence packs. It adds no AI
reasoning, confidence/probability calculation, or BUY/SELL engine.

Phase 11 selects only evidence available at the analysis timestamp, removes
duplicate/reposted context, compresses it, and sends it through independently
configured News, Candle, Structure, Liquidity, Flow, KTR, Risk, or Master AI
specialists. Responses are schema-validated, evidence-linked, cached, versioned,
and persisted with current/stale/superseded/invalid lifecycle state. This layer
interprets evidence only; it cannot create BUY/SELL/WAIT, Entry, Stop Loss, Take
Profit, or an active trade setup.

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
AllTick + Twelve Data       FRED      NewsData.io   Analyst RSS/Atom   AI provider adapters
market data                  Phase 8      Phase 7       Phase 9         Phase 11
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
[economic-data development guide](docs/development/economic-data.md). Phase 9
is documented in [analyst-data architecture](docs/architecture/analyst-data.md)
and the [analyst-data development guide](docs/development/analyst-data.md).
Phase 10 is documented in the [evidence-layer architecture](docs/architecture/evidence-layer.md),
[normalization guide](docs/development/normalization.md), and
[availability-time decision](docs/decisions/0006-availability-time-governs-evidence-visibility.md).
Phase 11 is documented in the [AI interpretation architecture](docs/architecture/ai-interpretation.md)
and [AI specialist configuration guide](docs/development/ai-interpretation.md).

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

Set `ALLTICK_TOKEN`. The application symbol `XAUUSD` maps to AllTick's
provider code `GOLD`; Compose fixes the primary provider identity to AllTick.

Set `TWELVE_DATA_API_KEY` and keep `TWELVE_DATA_ENABLED=true` to use Twelve
Data's separate free quota for history, backup/reference candles, and
cross-provider quality checks. Its provider symbol is `XAU/USD`. The two
providers are stored separately and are never silently blended.

## Run the dual-source market-data flow in Docker

Copy the example environment, replace the database password placeholders,
`ALLTICK_TOKEN`, and `TWELVE_DATA_API_KEY`, then start the stack:

```powershell
Copy-Item .env.example .env
docker compose up -d --build
```

If an existing SQL Server volume was created with a different `.env` password,
reset both persisted logins without deleting data, then start normally:

```powershell
.\scripts\reset-database-passwords.ps1
.\scripts\start-local.ps1
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

## Market-data endpoints

The dual-source quality endpoint is
`GET /api/market-data/{symbol}/source-comparison?timeframe=M1&limit=100`.

- `GET /api/market-data/provider/status` — safe selected-provider status

- `GET /health` — application and provider health
- `GET /api/system/status` — API connectivity contract
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
- `GET /api/evidence` — filtered and paginated normalized evidence
- `GET /api/evidence/{id}` — one evidence record with relations and clusters
- `GET /api/evidence/pack` — look-ahead-safe multi-source evidence pack
- `GET /api/evidence/conflicts` — deterministic directional conflicts without resolution
- `GET /api/evidence/sources` — paginated evidence-source coverage
- `/swagger` — interactive OpenAPI documentation in Development

- `POST /api/ai-interpretations` - run one configured specialist over bounded, look-ahead-safe evidence
- `GET /api/ai-interpretations` - query current or historical structured interpretations
- `GET /api/ai-interpretations/latest` - latest current interpretation for a context
- `GET /api/ai-interpretations/{id}` - interpretation with structured output and evidence trace
- `GET /api/ai-interpretations/specialists` - safe non-secret specialist configuration
- `GET /api/evidence/{id}/interpretations` - interpretations that used one evidence record

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

Database, live NewsData, and live FRED tests are opt-in. Their setup is
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
