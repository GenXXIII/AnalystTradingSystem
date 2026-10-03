# Economic-data development guide

## Configure FRED

Committed defaults keep economic collection disabled. Put the real key only in
the ignored `.env` or a server-side secret store:

```text
ECONOMIC_DATA_ENABLED=true
ECONOMIC_DATA_PROVIDER=FRED
ECONOMIC_DATA_PROVIDER_KEY=fred
FRED_API_KEY=YOUR_SERVER_SIDE_FRED_KEY
ECONOMIC_DATA_BASE_URL=https://api.stlouisfed.org/fred/
```

Never use a `NEXT_PUBLIC_` variable for the key. The health/status responses do
not return it, and the provider health check validates configuration without
spending a FRED request.

`ECONOMIC_DATA_TRACKED_SERIES` is a comma-separated list of
`SERIES_ID:Category` values. IDs must be unique and contain only letters,
digits, or underscore. Provider metadata supplies the names, units, frequencies,
and seasonal adjustments.

Important bounds:

- `ECONOMIC_DATA_INITIAL_HISTORY_YEARS` limits initial backfill.
- `ECONOMIC_DATA_REVISION_LOOKBACK_DAYS` overlaps incremental runs to discover revisions.
- `ECONOMIC_DATA_SYNC_INTERVAL_MINUTES` is at least 15 minutes; the default is six hours.
- `ECONOMIC_DATA_PROVIDER_PAGE_SIZE` and `ECONOMIC_DATA_MAXIMUM_PAGES_PER_SERIES` cap provider work.
- `ECONOMIC_DATA_MAXIMUM_PAGE_SIZE` caps API reads.
- `ECONOMIC_DATA_RATE_LIMIT_PER_MINUTE` defaults to 100, below FRED's documented 120-request limit.
- `ECONOMIC_DATA_MAX_RETRIES` and `ECONOMIC_DATA_RETRY_BASE_DELAY_SECONDS` bound retry work.

## Synchronization and API

The hosted worker synchronizes all configured series on its bounded interval.
A development/operator-triggered run is also available:

```http
POST /api/economic-data/synchronize
Content-Type: application/json

{ "externalSeriesId": "CPIAUCSL", "from": "2025-01-01", "to": "2026-10-03" }
```

Omit `externalSeriesId` to process all configured series. Omit dates for normal
incremental behavior. The remaining read endpoints never call FRED:

- `GET /api/economic-data/series?category=Inflation&frequency=Monthly`
- `GET /api/economic-data/series/{id}`
- `GET /api/economic-data/series/{id}/observations?page=1&pageSize=100&from=2025-01-01&to=2026-10-03`
- `GET /api/economic-data/latest`
- `GET /api/economic-data/status`

All endpoints use the standard success/error envelope and correlation ID.
Manual synchronization follows the project's current unauthenticated operator
endpoint convention; add authorization when the platform introduces its shared
authentication boundary rather than inventing a Phase 8-only scheme.

## Migrations and Docker

The `ImplementEconomicDataPipeline` migration creates the series, observation,
revision, state, and run tables and seeds the `fred` data provider. Compose
injects `FRED_API_KEY` only into the API container. Apply migrations through the
existing `database-migrator`; do not delete the SQL volume to adopt Phase 8.

## Tests

Normal tests use clearly named `TEST_ONLY` fixtures and make no FRED request:

```powershell
dotnet test tests/XauAi.UnitTests --configuration Release
dotnet test tests/XauAi.IntegrationTests --configuration Release
dotnet test tests/XauAi.ArchitectureTests --configuration Release
```

SQL integration and performance checks require a disposable-database-capable
administrator connection:

```powershell
$env:XAUAI_TEST_SQLSERVER_CONNECTION_STRING = "YOUR_SQL_ADMIN_TEST_CONNECTION"
dotnet test tests/XauAi.IntegrationTests --configuration Release `
  --filter FullyQualifiedName~EconomicDataSqlTests
```

The real FRED test is deliberately opt-in. It synchronizes one configured
series with two provider requests (metadata plus one observation page), verifies
normalization and SQL persistence, and deletes its isolated test database:

```powershell
$env:XAUAI_RUN_FRED_INTEGRATION = "true"
$env:FRED_API_KEY = "YOUR_FRED_API_KEY"
$env:XAUAI_TEST_SQLSERVER_CONNECTION_STRING = "YOUR_SQL_ADMIN_TEST_CONNECTION"
dotnet test tests/XauAi.IntegrationTests --configuration Release `
  --filter FullyQualifiedName~FredLiveIntegrationTests
```

## Troubleshooting

- `ECONOMIC_DATA_DISABLED`: enable collection and restart after configuring the key.
- `ECONOMIC_DATA_PROVIDER_AUTHENTICATION_FAILED`: replace the server-side FRED key.
- `ECONOMIC_DATA_PROVIDER_RATE_LIMITED`: wait for reset or lower the configured local rate.
- `ECONOMIC_DATA_PROVIDER_TIMEOUT`: check network access and the timeout value; retries remain bounded.
- `ECONOMIC_DATA_PROVIDER_RESPONSE_INVALID`: inspect safe structured logs and verify the configured series ID.
- `ECONOMIC_DATA_DATABASE_DISABLED`: enable SQL persistence before synchronizing or querying stored values.

Provider outages mark the affected run/state failed while preserving all prior
series, observations, and revisions.
