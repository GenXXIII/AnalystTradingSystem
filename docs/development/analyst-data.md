# Analyst-data development guide

## Configure a permitted source

Collection is disabled by default. Verify that the source permits automated
RSS/Atom access and summary retention before enabling it. Do not configure a
page URL that requires scraping.

```text
ANALYSTS_ENABLED=true
ANALYST_PROVIDER=RssAtom
ANALYST_PROVIDER_KEY=analyst-rss
ANALYST_SOURCE_TYPE=RssFeed
ANALYST_BASE_URL=https://permitted.example/feed.xml
ANALYST_REQUIRES_API_KEY=false
```

If a legitimate private feed requires bearer authentication, set
`ANALYST_REQUIRES_API_KEY=true` and place the secret only in
`ANALYST_PROVIDER_API_KEY`. It must not be exposed through a public frontend
environment variable.

The remaining bounds are documented in
[configuration.md](configuration.md). Rate limiting is applied before HTTP
requests. Transient timeouts, HTTP 429 responses, and provider 5xx failures use
bounded exponential backoff. Logs contain provider, run, counts, duration,
status, safe error code, and trace ID but never credentials.

## Synchronization

Scheduled synchronization starts only when Analysts is enabled. Manual sync is:

```http
POST /api/analyst-predictions/synchronize
Content-Type: application/json

{
  "fromUtc": "2026-10-01T00:00:00Z",
  "toUtc": "2026-10-04T00:00:00Z"
}
```

Without an explicit range, the first run uses `InitialLookbackDays`. Later runs
start from the last successful sync minus `CollectionOverlapMinutes`; SQL
deduplication makes that overlap safe. RSS/Atom has no standard archive or
cursor pagination, so historical coverage is limited to entries the configured
feed currently retains. The application contract supports cursors and bounded
pages for future legitimate providers.

A failed provider request leaves all stored history intact, marks the run and
sync state failed, and is retried on the next scheduled cycle.

## APIs

- `GET /api/analyst-sources?page=1&pageSize=25`
- `GET /api/analyst-sources/{id}`
- `GET /api/analysts?sourceId={id}&page=1&pageSize=25`
- `GET /api/analysts/{id}`
- `GET /api/analyst-predictions?instrument=XAUUSD&direction=Bullish&from=...&to=...&asOf=...`
- `GET /api/analyst-predictions/{id}`
- `GET /api/analyst-predictions/latest?pageSize=25`
- `GET /api/analyst-predictions/status`
- `POST /api/analyst-predictions/synchronize`

The maximum page size is enforced. Responses use application-owned models and
never return provider XML or credentials.

## Copyright and source limitations

The RSS/Atom adapter stores title, summary, attribution, URL, publication time,
and deterministic structured claims. It deliberately does not fetch or copy the
full linked article. Operators remain responsible for the configured source's
license, terms, authentication, rate limits, and retention permissions.

## Validation

Run the checks from the repository root:

```powershell
dotnet build XauAi.slnx --configuration Release
dotnet test tests/XauAi.UnitTests/XauAi.UnitTests.csproj --configuration Release --no-build
dotnet test tests/XauAi.IntegrationTests/XauAi.IntegrationTests.csproj --configuration Release --no-build
dotnet test tests/XauAi.ArchitectureTests/XauAi.ArchitectureTests.csproj --configuration Release --no-build
dotnet ef migrations has-pending-model-changes --project backend/XauAi.Infrastructure --startup-project backend/XauAi.Infrastructure --configuration Release --no-build
git diff --check
```

SQL integration tests require `XAUAI_TEST_SQL_CONNECTION`. Live provider tests
are intentionally not part of normal CI because no external feed or credential
is committed.
