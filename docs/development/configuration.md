# Configuration and secrets

## Configuration flow

```text
appsettings.json
        +
appsettings.{Environment}.json
        +
.NET user secrets (Development only)
        +
environment variables / Docker injection
        ↓
provider-neutral typed options
        ↓
conditional startup validation
        ↓
EF Core persistence and future Infrastructure clients
```

The API binds configuration once in Infrastructure. Application code must not
call `Environment.GetEnvironmentVariable` directly. The central environment
adapter maps the friendly variable names below to typed configuration paths;
native .NET names such as `AI__Enabled` also continue to work.

All integrations are disabled by default. Enabling one turns its configuration
requirements into startup requirements. Placeholders such as
`USER_PROVIDED_LATER`, `MODEL_DEFINED_LATER`, and `REPLACE_WITH...` are treated
as missing values by validation.

## Environments

| Environment | File | Behavior |
| --- | --- | --- |
| Development | `appsettings.Development.json` | Local CORS origin, Swagger enabled, providers disabled |
| Testing | `appsettings.Testing.json` | Isolated test CORS origin, Swagger disabled, providers disabled |
| Production | `appsettings.Production.json` | Swagger disabled and CORS origin must be supplied externally |

Committed settings files contain only non-secret defaults. Production secrets
should come from the deployment platform's secret manager and be exposed to the
process as environment variables. Do not copy production secrets into `.env`.

## Variables

### Application, HTTP, and containers

| Variable | Required | Secret | Used by | Phase |
| --- | --- | --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Yes | No | Backend | 1 |
| `APP_NAME` | Yes; default exists | No | Backend | 2 |
| `APP_VERSION` | Yes; default exists | No | Backend | 2 |
| `LOG_LEVEL_DEFAULT` | No | No | Backend | 2 |
| `API_PORT` | Docker host mapping only | No | Docker | 1 |
| `WEB_PORT` | Docker host mapping only | No | Docker | 1 |
| `NEXT_PUBLIC_API_URL` | Yes for web | No; browser-visible | Frontend | 1 |
| `CORS_ALLOWED_ORIGINS` | Yes; comma-separated | No | Backend | 2 |
| `API_ENABLE_SWAGGER` | No | No | Backend | 2 |
| `SQLSERVER_PORT` | Docker host mapping only | No | Docker | 1 |
| `REDIS_PORT` | Docker host mapping only | No | Docker | 1 |

`NEXT_PUBLIC_API_URL` is the only browser-exposed application variable. Never
prefix a credential, connection string, or private provider setting with
`NEXT_PUBLIC_`.

### Database and Redis

| Variable | Required | Secret | Used by | Phase |
| --- | --- | --- | --- | --- |
| `MSSQL_SA_PASSWORD` | Local SQL container startup | Yes | SQL Server container | 1/3 |
| `DATABASE_APP_PASSWORD` | Docker database initialization/runtime | Yes | Database init and backend | 3 |
| `DATABASE_ENABLED` | No; committed default `false`, Compose default `true` | No | Backend | 2/3 |
| `DATABASE_CONNECTION_STRING` | When database is enabled | Yes | Backend | 3 |
| `DATABASE_COMMAND_TIMEOUT_SECONDS` | No; default exists | No | Backend | 3 |
| `DATABASE_APPLY_MIGRATIONS_ON_STARTUP` | No; true only for an authorized development/deployment migrator | No | Backend | 3 |
| `REDIS_ENABLED` | No; defaults `false` | No | Backend | 2/3 |
| `REDIS_CONNECTION_STRING` | When Redis is enabled | Depends on authentication | Backend | 3 |
| `REDIS_INSTANCE_NAME` | Yes; default exists | No | Backend | 3 |
| `REDIS_DEFAULT_EXPIRATION_SECONDS` | Yes; default exists | No | Backend | 3 |

### AI and cost controls

| Variable | Required | Secret | Used by | Phase |
| --- | --- | --- | --- | --- |
| `<SPECIALIST>_AI_ENABLED` | No; defaults `false` | No | One AI specialist | 11 |
| `<SPECIALIST>_AI_PROVIDER` | When that specialist is enabled | No | Attribution/model metadata | 11 |
| `<SPECIALIST>_AI_ADAPTER` | When enabled; defaults `OpenAiCompatible` | No | Provider transport | 11 |
| `<SPECIALIST>_AI_REQUIRES_API_KEY` | Provider-dependent | No | One AI specialist | 11 |
| `<SPECIALIST>_AI_API_KEY` | When required | Yes | One AI specialist | 11 |
| `<SPECIALIST>_AI_BASE_URL` | When enabled | No/limited | One AI specialist | 11 |
| `<SPECIALIST>_AI_MODEL` | When enabled | No | One AI specialist | 11 |
| `<SPECIALIST>_AI_TEMPERATURE` | No; validated 0-2 | No | One AI specialist | 11 |
| `<SPECIALIST>_AI_TIMEOUT_SECONDS` | No; default exists | No | One AI specialist | 11 |
| `<SPECIALIST>_AI_MAX_RETRIES` | No; default exists | No | One AI specialist | 11 |
| `<SPECIALIST>_AI_MAX_OUTPUT_TOKENS` | No; bounded default exists | No | One AI specialist | 11 |
| `<SPECIALIST>_AI_REQUESTS_PER_MINUTE` | No; bounded default exists | No | One AI specialist | 11 |

`<SPECIALIST>` is `NEWS`, `CANDLE`, `STRUCTURE`, `LIQUIDITY`, `FLOW`, `KTR`,
`RISK`, or `MASTER`. Each configuration is validated independently. Phase 11
enforces evidence hashing, persisted interpretation reuse, bounded compression,
token accounting, retry limits, and per-specialist request pacing.

### AllTick (default market-data provider)

| Variable | Required | Secret | Used by | Phase |
| --- | --- | --- | --- | --- |
| `ALLTICK_ENABLED` | No; defaults `false` | No | Backend AllTick | Market data |
| `ALLTICK_TOKEN` | When AllTick is enabled | Yes | Backend AllTick | Market data |
| `ALLTICK_APPLICATION_SYMBOL` | Yes; defaults `XAUUSD` | No | Backend AllTick | Market data |
| `ALLTICK_SYMBOL` | Yes; defaults `GOLD` | No | Backend AllTick | Market data |
| `ALLTICK_HTTP_BASE_URL`, `ALLTICK_WEBSOCKET_URL` | Validated defaults exist | No | Backend AllTick | Market data |
| `ALLTICK_REQUEST_TIMEOUT_SECONDS`, `ALLTICK_RECONNECT_DELAY_SECONDS`, `ALLTICK_HEARTBEAT_INTERVAL_SECONDS` | Bounded defaults exist | No | Backend AllTick | Market data |
| `ALLTICK_QUOTE_MAX_AGE_SECONDS`, `ALLTICK_REALTIME_PERSIST_INTERVAL_SECONDS` | Bounded defaults exist | No | Backend AllTick | Market data |
| `ALLTICK_MAX_BARS_PER_REQUEST`, `ALLTICK_MINIMUM_HTTP_REQUEST_INTERVAL_SECONDS` | Bounded free-tier defaults exist | No | Backend AllTick | Market data |

`ALLTICK_TOKEN` is server-only. AllTick calls XAUUSD gold `GOLD`; the adapter
maps it to the application's stable `XAUUSD` identity.

### Market data pipeline

| Variable | Required | Secret | Used by | Phase |
| --- | --- | --- | --- | --- |
| `MARKET_DATA_SYNC_ENABLED` | No; defaults `false` | No | Background sync | 5 |
| `MARKET_DATA_SYMBOL` | Yes; defaults `XAUUSD` | No | Pipeline | 5 |
| `MARKET_DATA_TIMEFRAMES` | Yes; supported comma-separated values | No | Pipeline | 5 |
| `MARKET_DATA_INITIAL_HISTORY_DAYS` | Yes; bounded default exists | No | Initial sync | 5 |
| `MARKET_DATA_SYNC_INTERVAL_SECONDS` | Yes; bounded default exists | No | Scheduler | 5 |
| `MARKET_DATA_BATCH_SIZE` | Yes; bounded default exists | No | Acquisition/persistence | 5 |
| `MARKET_DATA_MAX_API_LIMIT` | Yes; bounded default exists | No | Query API | 5 |
| `MARKET_DATA_MAX_QUERY_RANGE_DAYS` | Yes; bounded default exists | No | Query/sync API | 5 |
| `MARKET_DATA_MAX_RETRIES` | Yes; bounded default exists | No | Recovery | 5 |
| `MARKET_DATA_RETRY_BASE_DELAY_SECONDS` | Yes; bounded default exists | No | Recovery | 5 |
| `MARKET_DATA_MAX_GAP_RESULTS` | Yes; bounded default exists | No | Gap query | 5 |
| `MARKET_DATA_INCLUDE_FORMING_CANDLE` | No; defaults `true` | No | Scheduler | 5 |

The scheduler is deliberately local to the API process. SQL Server remains the
source of truth. See [market-data pipeline](../market-data-pipeline.md) for
behavior and limits.

### Technical analysis engine

| Variable group | Required | Secret | Used by | Phase |
| --- | --- | --- | --- | --- |
| `TECHNICAL_ANALYSIS_ENABLED`, `TECHNICAL_ANALYSIS_SYMBOL`, `TECHNICAL_ANALYSIS_TIMEFRAMES`, `TECHNICAL_ANALYSIS_HISTORY_LIMIT` | No; validated defaults exist | No | Analysis service | 6 |
| `SMA_PERIODS`, `EMA_PERIODS`, `RSI_PERIOD` | No; validated defaults exist | No | Trend/momentum | 6 |
| `MACD_FAST_PERIOD`, `MACD_SLOW_PERIOD`, `MACD_SIGNAL_PERIOD` | No; validated defaults exist | No | Momentum | 6 |
| `ATR_PERIOD`, `ADX_PERIOD`, `BOLLINGER_PERIOD`, `BOLLINGER_STANDARD_DEVIATION` | No; validated defaults exist | No | Trend/volatility | 6 |
| `STOCHASTIC_K_PERIOD`, `STOCHASTIC_D_PERIOD` | No; validated defaults exist | No | Momentum | 6 |
| `SWING_WINDOW`, `LEVEL_TOLERANCE_PERCENT`, `MINIMUM_LEVEL_TOUCHES`, `MAXIMUM_LEVEL_ZONES` | No; validated defaults exist | No | Structure/levels | 6 |
| `PRICE_ACTION_LOOKBACK`, `VOLATILITY_LOOKBACK`, `VERY_LOW_VOLATILITY_RATIO`, `LOW_VOLATILITY_RATIO`, `HIGH_VOLATILITY_RATIO`, `VERY_HIGH_VOLATILITY_RATIO` | No; validated defaults exist | No | Price action/volatility | 6 |

These values affect analytical meaning and are kept in the established typed
configuration path. They are not credentials. The committed application default
keeps analysis disabled, while the local ignored `.env` may enable it. The API
uses only completed normalized SQL candles and never calls a provider while calculating
analysis. See [technical analysis](../technical-analysis.md) for formulas,
warm-up behavior, time safety, and limitations.

### News, economic data, and analyst sources

| Variable | Required | Secret | Used by | Phase |
| --- | --- | --- | --- | --- |
| `NEWS_ENABLED` | No; defaults `false` | No | Backend News | 2/7 |
| `NEWS_PROVIDER` | When News is enabled | No | Backend News | 7 |
| `NEWS_REQUIRES_API_KEY` | Provider-dependent | No | Backend News | 7 |
| `NEWS_API_KEY` | Only when the provider requires it | Yes | Backend News | 7 |
| `NEWS_BASE_URL` | When News is enabled | No | Backend News | 7 |
| `NEWS_TIMEOUT_SECONDS` | No; default exists | No | Backend News | 7 |
| `NEWS_MAX_RETRIES` | No; default exists | No | Backend News | 7 |
| `NEWS_RATE_LIMIT_PER_MINUTE` | Provider-dependent | No | Backend News | 7 |
| `NEWS_USE_TIMEFRAME_PARAMETER` | No; enable only when the provider plan supports it | No | Provider request | 7 |
| `NEWS_PROVIDER_KEY` | Yes; defaults `newsdata` | No | News persistence | 7 |
| `NEWS_SYMBOL` | Yes; defaults `XAUUSD` | No | News association | 7 |
| `NEWS_LANGUAGE` | Yes; defaults `en` | No | Provider request | 7 |
| `NEWS_PROVIDER_QUERY` | Yes; maximum 100 characters | No | Provider request | 7 |
| `NEWS_INITIAL_LOOKBACK_HOURS` | Yes; bounded 1-48 | No | Initial collection | 7 |
| `NEWS_COLLECTION_OVERLAP_MINUTES` | Yes; bounded 0-120 | No | Incremental collection | 7 |
| `NEWS_COLLECTION_INTERVAL_SECONDS` | Yes; bounded 60-86400 | No | Scheduler | 7 |
| `NEWS_PAGE_SIZE` | Yes; bounded 1-50 | No | Provider paging | 7 |
| `NEWS_MAXIMUM_PAGES_PER_COLLECTION` | Yes; bounded 1-20 | No | Request/cost limit | 7 |
| `NEWS_MAXIMUM_PAGE_SIZE` | Yes; bounded 1-200 | No | Query API | 7 |
| `NEWS_MAXIMUM_COLLECTION_RANGE_DAYS` | Yes; bounded 1-366 | No | Manual collection | 7 |
| `NEWS_RETRY_BASE_DELAY_SECONDS` | Yes; bounded 1-60 | No | Retry policy | 7 |
| `NEWS_ARCHIVE_ENABLED` | No; requires provider-plan support | No | Archive endpoint | 7 |
| `NEWS_MINIMUM_RELEVANCE` | Yes; defaults `Medium` | No | Collection/latest query | 7 |
| `NEWS_GOLD_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_USD_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_FED_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_INFLATION_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_EMPLOYMENT_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_RATES_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_ECONOMY_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_CENTRAL_BANK_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_GEOPOLITICS_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `NEWS_COMMODITY_KEYWORDS` | Yes; comma-separated defaults | No | Relevance classifier | 7 |
| `ECONOMIC_DATA_ENABLED` | No; defaults `false` | No | Backend Economic Data | 2/8 |
| `ECONOMIC_DATA_PROVIDER` | When Economic Data is enabled | No | Backend Economic Data | 8 |
| `ECONOMIC_DATA_REQUIRES_API_KEY` | Provider-dependent | No | Backend Economic Data | 8 |
| `FRED_API_KEY` | When FRED is enabled | Yes | Backend Economic Data | 8 |
| `ECONOMIC_DATA_BASE_URL` | When Economic Data is enabled | No | Backend Economic Data | 8 |
| `ECONOMIC_DATA_TIMEOUT_SECONDS` | No; default exists | No | Backend Economic Data | 8 |
| `ECONOMIC_DATA_MAX_RETRIES` | No; default exists | No | Backend Economic Data | 8 |
| `ECONOMIC_DATA_RATE_LIMIT_PER_MINUTE` | Provider-dependent | No | Backend Economic Data | 8 |
| `ECONOMIC_DATA_PROVIDER_KEY` | Yes; defaults `fred` | No | Economic persistence | 8 |
| `ECONOMIC_DATA_INITIAL_HISTORY_YEARS` | Yes; bounded 1-100 | No | Initial backfill | 8 |
| `ECONOMIC_DATA_REVISION_LOOKBACK_DAYS` | Yes; bounded 0-3650 | No | Revision-aware incremental sync | 8 |
| `ECONOMIC_DATA_SYNC_INTERVAL_MINUTES` | Yes; bounded 15-10080 | No | Scheduler | 8 |
| `ECONOMIC_DATA_PROVIDER_PAGE_SIZE` | Yes; bounded 1-100000 | No | Provider paging | 8 |
| `ECONOMIC_DATA_MAXIMUM_PAGES_PER_SERIES` | Yes; bounded 1-1000 | No | Provider request limit | 8 |
| `ECONOMIC_DATA_MAXIMUM_PAGE_SIZE` | Yes; bounded 1-1000 | No | Query API | 8 |
| `ECONOMIC_DATA_MAXIMUM_QUERY_RANGE_YEARS` | Yes; bounded 1-200 | No | Query/sync range | 8 |
| `ECONOMIC_DATA_RETRY_BASE_DELAY_SECONDS` | Yes; bounded 1-60 | No | Retry policy | 8 |
| `ECONOMIC_DATA_TRACKED_SERIES` | Yes; `SERIES_ID:Category` list | No | Series selection | 8 |
| `ANALYSTS_ENABLED` | No; defaults `false` | No | Backend Analysts | 2/9 |
| `ANALYST_PROVIDER` | Yes; Phase 9 adapter is `RssAtom` | No | Backend Analysts | 9 |
| `ANALYST_PROVIDER_KEY` | Yes; defaults `analyst-rss` | No | Analyst persistence | 9 |
| `ANALYST_SOURCE_TYPE` | Yes; Phase 9 adapter requires `RssFeed` | No | Backend Analysts | 9 |
| `ANALYST_REQUIRES_API_KEY` | Provider-dependent | No | Backend Analysts | 9 |
| `ANALYST_PROVIDER_API_KEY` | Only when the provider requires it | Yes | Backend Analysts | 9 |
| `ANALYST_BASE_URL` | Required when enabled; permitted RSS/Atom URL | No | Backend Analysts | 9 |
| `ANALYST_TIMEOUT_SECONDS` | No; default exists | No | Backend Analysts | 9 |
| `ANALYST_MAX_RETRIES` | No; default exists | No | Backend Analysts | 9 |
| `ANALYST_RATE_LIMIT_PER_MINUTE` | Provider-dependent | No | Backend Analysts | 9 |
| `ANALYST_SYMBOL` | Yes; defaults `XAUUSD` | No | Analyst normalization | 9 |
| `ANALYST_INITIAL_LOOKBACK_DAYS` | Yes; bounded 1-3650 | No | Initial feed collection | 9 |
| `ANALYST_COLLECTION_OVERLAP_MINUTES` | Yes; bounded 0-1440 | No | Incremental sync | 9 |
| `ANALYST_SYNC_INTERVAL_MINUTES` | Yes; bounded 1-10080 | No | Scheduler | 9 |
| `ANALYST_PROVIDER_PAGE_SIZE` | Yes; bounded 1-500 | No | Provider paging | 9 |
| `ANALYST_MAXIMUM_PAGES_PER_SYNC` | Yes; bounded 1-100 | No | Provider safety limit | 9 |
| `ANALYST_MAXIMUM_PAGE_SIZE` | Yes; bounded 1-500 | No | Query API | 9 |
| `ANALYST_MAXIMUM_COLLECTION_RANGE_DAYS` | Yes; bounded 1-36500 | No | Sync range | 9 |
| `ANALYST_RETRY_BASE_DELAY_SECONDS` | Yes; bounded 1-300 | No | Retry policy | 9 |
| `ANALYST_RELEVANCE_KEYWORDS` | Yes; comma-separated defaults | No | Deterministic relevance | 9 |
| `EVIDENCE_MAXIMUM_PAGE_SIZE` | Yes; bounded 1-1000 | No | Evidence query API | 10 |
| `EVIDENCE_MAXIMUM_QUERY_RANGE_DAYS` | Yes; bounded 1-3650 | No | Evidence query API | 10 |
| `EVIDENCE_DEFAULT_PACK_LOOKBACK_DAYS` | Yes; bounded 1-3650 | No | Evidence packs | 10 |
| `EVIDENCE_MAXIMUM_PACK_LOOKBACK_DAYS` | Yes; bounded 1-3650 | No | Evidence packs | 10 |
| `EVIDENCE_MAXIMUM_PACK_ITEMS_PER_TYPE` | Yes; bounded 1-1000 | No | Evidence packs | 10 |
| `EVIDENCE_INGESTION_BATCH_SIZE` | Yes; bounded 1-5000 | No | Evidence ingestion | 10 |
| `EVIDENCE_CONFLICT_WINDOW_HOURS` | Yes; bounded 1-720 | No | Conflict detection | 10 |
| `EVIDENCE_MAXIMUM_CONFLICTS` | Yes; bounded 1-5000 | No | Conflict API/pack | 10 |
| `AI_INTERPRETATION_PROMPT_VERSION` | Yes; defaults `phase11-v2` | No | AI cache/prompt version | 11 |
| `AI_INTERPRETATION_DEFAULT_LOOKBACK_HOURS` | Yes; bounded by maximum | No | Evidence selection | 11 |
| `AI_INTERPRETATION_MAXIMUM_LOOKBACK_HOURS` | Yes; bounded 1-87600 | No | Evidence selection | 11 |
| `AI_INTERPRETATION_MAXIMUM_EVIDENCE_ITEMS` | Yes; bounded 1-500 | No | Evidence selection | 11 |
| `AI_INTERPRETATION_MAXIMUM_COMPRESSED_CHARACTERS` | Yes; bounded 2000-1000000 | No | Provider evidence package | 11 |
| `AI_INTERPRETATION_CURRENT_CONTEXT_CACHE_MINUTES` | Yes; bounded 1-60 | No | Current-time cache context | 11 |
| `AI_INTERPRETATION_MAXIMUM_PAGE_SIZE` | Yes; bounded 1-1000 | No | Interpretation query API | 11 |
| `<SPECIALIST>_AI_ENABLED` | No; defaults `false` | No | One Phase 11 specialist | 11 |
| `<SPECIALIST>_AI_API_KEY` | When that enabled provider requires it | Yes | One Phase 11 specialist | 11 |
| `<SPECIALIST>_AI_PROVIDER/ADAPTER/MODEL/BASE_URL` | Required when enabled | No | One Phase 11 specialist | 11 |

Phase 9 implements the `RssAtom` adapter with `RssFeed` source type. A
configured feed is not permission to collect data: terms, licensing, access,
rate limits, and summary-retention rights must be assessed before enabling it.
See [analyst data](analyst-data.md).

Phase 7 supports `NewsData` as its provider. The committed configuration and
`.env.example` keep collection disabled. Enabling it requires a real
`NEWS_API_KEY` and valid `NEWS_BASE_URL`. Collection uses a single composite
query per bounded page, persists incremental state in SQL Server, and applies
the keyword groups locally. Set `NEWS_ARCHIVE_ENABLED=true` only when the
NewsData plan grants archive access. See [news collection](../news-collection.md)
for the rules, endpoints, cost behavior, and live-test opt-in.

Phase 8 supports `FRED`. Collection remains disabled in committed defaults and
requires a server-side `FRED_API_KEY` when enabled. The configured series list,
backfill, revision overlap, request/page bounds, retry behavior, APIs, and
opt-in live verification are documented in the
[economic-data development guide](economic-data.md).

Phase 10 evidence settings are non-secret safety bounds. The default evidence
pack lookback cannot exceed the configured maximum; startup validation rejects
invalid values. See the [normalization guide](normalization.md) for the evidence
contract, API, normalization, deduplication, and historical timing rules.

## Local development

The API can still run without an external provider or database by using the
committed disabled defaults:

```powershell
dotnet run --project backend/XauAi.Api
```

To run persistence outside Compose, enable the database and put its connection
string in .NET user secrets from the repository root. The
project already has a non-secret `UserSecretsId`:

```powershell
dotnet user-secrets set "Database:Enabled" "true" --project backend/XauAi.Api
dotnet user-secrets set "Database:ConnectionString" "YOUR_LOCAL_CONNECTION_STRING" --project backend/XauAi.Api
dotnet user-secrets set "Database:ApplyMigrationsOnStartup" "true" --project backend/XauAi.Api
```

User secrets are stored outside the repository and are loaded only in Development.

For Docker, copy `.env.example` to the Git-ignored `.env`, set different strong
values for `MSSQL_SA_PASSWORD` and `DATABASE_APP_PASSWORD`, and run Compose.
`database-init` provisions the restricted login, `database-migrator` applies
the migration and exits, and the API uses only the restricted login. Docker
Compose reads `.env` and explicitly injects backend variables. Dockerfiles
contain no credentials.

## Production

- Use the deployment platform's encrypted secret store.
- Inject secrets as runtime environment variables, not image build arguments.
- Supply `CORS_ALLOWED_ORIGINS` explicitly; Production intentionally has none.
- Keep Swagger disabled unless there is a controlled operational need.
- Keep `DATABASE_APPLY_MIGRATIONS_ON_STARTUP=false` for the long-running API;
  run reviewed migrations as an authorized deployment step.
- Give the runtime database principal only the permissions its use cases need.
- Never log option objects or configuration providers.
- Rotate a credential immediately if it is committed or printed to a shared log.

## Validation behavior

Invalid enabled configuration fails during application startup with option keys
and corrective requirements. Validation messages never contain configured
secret values. Disabled providers may retain empty strings or documented
placeholders without preventing startup.
