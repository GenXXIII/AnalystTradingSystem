# XAUUSD-AI — PHASE 8

## Economic Data Pipeline

You are continuing development of the **XAUUSD-AI** project.

You must implement **Phase 8 only**.

Do not skip ahead to Phase 9 or later.

---

# 1. PROJECT CONTEXT

This project is an AI-powered XAUUSD market intelligence and signal-analysis system.

The application architecture is:

```text
XAUUSD-AI
│
├── frontend/
│   └── xau-ai-web/
│
├── backend/
│   ├── XauAi.Api/
│   ├── XauAi.Application/
│   ├── XauAi.Domain/
│   └── XauAi.Infrastructure/
│
├── tests/
│   ├── XauAi.UnitTests/
│   ├── XauAi.IntegrationTests/
│   └── XauAi.ArchitectureTests/
│
├── docs/
├── scripts/
├── docker-compose.yml
└── .env.example
```

Architecture:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application
 ↓
Domain
```

External providers must remain behind application-owned interfaces.

For economic data:

```text
Application
    │
    ▼
IEconomicDataProvider
    │
    ▼
FredEconomicDataProvider
    │
    ▼
FRED API
```

The rest of the application must NOT depend directly on FRED SDK/API models.

---

# 2. PHASE 8 GOAL

Build a reliable economic-data collection and normalization pipeline.

The system must be able to:

* connect to FRED
* retrieve configured economic series
* retrieve historical observations
* retrieve incremental updates
* normalize external data into our own domain model
* store observations in SQL Server
* prevent duplicates
* track synchronization state
* handle rate limits and transient failures
* preserve historical values
* expose economic data through our own API
* provide data suitable for later macro analysis

This phase is about **data collection and infrastructure**.

It is NOT about AI interpretation.

---

# 3. VERY IMPORTANT — DO NOT IMPLEMENT AI ANALYSIS YET

Do NOT implement:

* OpenAI analysis
* macro AI reasoning
* BUY/SELL signals
* final trade decisions
* strategy scoring
* confluence scoring
* signal confidence
* automated trading
* analyst reasoning
* news interpretation

Phase 8 only provides reliable economic data for future phases.

The future flow will be:

```text
FRED
 ↓
Economic Data Pipeline
 ↓
Normalized Economic Data
 ↓
Database
 ↓
Future Macro Analysis
 ↓
Future AI Analyst
```

---

# 4. WHY ECONOMIC DATA MATTERS FOR XAUUSD

The future XAUUSD analyst needs macroeconomic context.

Important relationships include:

```text
Inflation
    ↓
Fed expectations
    ↓
Interest-rate expectations
    ↓
Treasury yields
    ↓
USD strength
    ↓
Gold environment
```

The application must collect the underlying data first.

Do NOT hard-code causal conclusions such as:

```text
CPI ↑ = GOLD SELL
```

That is too simplistic.

The future analyst must evaluate the broader context.

Phase 8 only stores the facts.

---

# 5. FIRST PROVIDER

Implement:

```text
FredEconomicDataProvider
```

behind:

```text
IEconomicDataProvider
```

Do not make the entire application depend on FRED.

The architecture must allow another provider to be added later.

Example:

```text
IEconomicDataProvider
        │
        ├── FredEconomicDataProvider
        │
        └── FutureEconomicDataProvider
```

---

# 6. ECONOMIC DATA DOMAIN MODEL

Use the existing database foundation from Phase 3.

Do not duplicate economic entities if they already exist.

Inspect the current project first.

Reuse existing models where appropriate.

The economic-data domain should conceptually support:

```text
EconomicSeries
EconomicObservation
EconomicRelease/Event
EconomicDataSource
EconomicSyncState
```

Do not blindly create all of these if the existing Phase 3 model already covers them.

---

# 7. ECONOMIC SERIES

An economic series represents a measurable macroeconomic dataset.

Examples:

```text
CPI
Core CPI
PCE
Core PCE
Federal Funds Rate
Unemployment Rate
Nonfarm Payrolls
GDP
Real GDP
Treasury yields
Industrial Production
Retail Sales
PMI-related data where available
Consumer sentiment
```

Do not assume every desired dataset exists in FRED.

The system should represent the actual provider series.

A series should contain information such as:

```text
Id
Source
ExternalSeriesId
Name
Description
Units
Frequency
SeasonalAdjustment
Country
Currency
Category
Provider
IsActive
CreatedAt
UpdatedAt
```

Use appropriate types.

Do not use floating-point types for financial/economic values where precision matters.

---

# 8. ECONOMIC OBSERVATION

Each observation represents a value at a specific period/date.

Conceptually:

```text
EconomicObservation
-------------------
Id
EconomicSeriesId
ObservationDate
Value
OriginalValue
Status
Source
CreatedAt
```

The exact model should follow the existing project architecture.

Important:

* store UTC timestamps for system timestamps
* preserve the provider's observation date/period
* support missing values
* do not convert missing observations into zero
* do not fabricate values
* preserve original provider values when useful

Example:

```text
Series:
Federal Funds Rate

Date:
2026-09-01

Value:
4.25
```

---

# 9. RELEASE DATA

If the existing economic-event model from Phase 3 supports it, use it.

Economic releases should support fields such as:

```text
Event
Country
Currency
Category
Importance
ScheduledTime
Previous
Forecast
Actual
Source
ExternalId
```

Examples:

```text
CPI
NFP
FOMC
GDP
PCE
Unemployment
Retail Sales
```

Do not invent forecast or actual values.

Only store values actually returned by a legitimate provider/source.

If FRED does not provide a specific field, leave it unavailable rather than fabricating it.

---

# 10. IMPORTANT DISTINCTION

Do not confuse:

```text
Economic Series
```

with:

```text
Economic Release/Event
```

For example:

```text
CPI Series
    ↓
Historical CPI observations
```

is different from:

```text
CPI Release
    ↓
Scheduled release
    ↓
Forecast
    ↓
Actual
```

The database should preserve this distinction where practical.

---

# 11. FRED SERIES CONFIGURATION

Do not hard-code hundreds of FRED series throughout the application.

Create configuration for the series the system wants to track.

For example:

```text
EconomicSeriesDefinition
```

or an equivalent configuration mechanism.

Possible configured categories:

```text
Inflation
Employment
Interest Rates
GDP
Consumer
Production
Treasury
Monetary Policy
```

The exact FRED series IDs must be verified against FRED.

Do not invent series IDs.

---

# 12. INITIAL MACRO DATA TARGETS

The initial configuration should focus on data relevant to XAUUSD.

Prioritize categories such as:

### Inflation

```text
CPI
Core CPI
PCE
Core PCE
```

### Employment

```text
Unemployment
Nonfarm Payrolls
```

### Interest Rates

```text
Federal Funds Rate
```

### Treasury / Rates

Where reliable FRED series are available:

```text
2Y Treasury Yield
5Y Treasury Yield
10Y Treasury Yield
30Y Treasury Yield
```

### Economic Growth

```text
GDP
Real GDP
```

### Other useful macro context

```text
Retail Sales
Industrial Production
Consumer-related indicators
```

Do not add dozens of unnecessary series simply because they exist.

Start with a focused XAUUSD macro dataset.

---

# 13. PROVIDER INTERFACE

Create an application-owned interface similar to:

```text
IEconomicDataProvider
```

It should support operations conceptually such as:

```text
GetSeriesMetadataAsync(...)
GetObservationsAsync(...)
GetLatestObservationAsync(...)
GetObservationsSinceAsync(...)
```

Use the project's existing conventions.

Do not expose FRED-specific response models through the interface.

For example, avoid:

```text
Application → FredResponse
```

Prefer:

```text
Application → EconomicObservationDto
```

or domain/application-owned models.

---

# 14. PROVIDER ADAPTER

Implement:

```text
FredEconomicDataProvider
```

The provider adapter is responsible for:

```text
Application request
      ↓
FRED request
      ↓
FRED response
      ↓
Normalize
      ↓
Application/domain model
```

FRED-specific details must stay inside Infrastructure.

---

# 15. API KEY SECURITY

Use the configuration architecture from Phase 2.

Example:

```env
FRED_API_KEY=
```

Never:

* hard-code the key
* commit the key
* log the key
* return the key through an API
* expose it through frontend configuration
* use `NEXT_PUBLIC_FRED_API_KEY`

The key must remain server-side.

---

# 16. CONFIGURATION

Use the existing typed configuration system.

For example:

```text
EconomicDataOptions
```

should contain appropriate settings such as:

```text
Enabled
Provider
ApiKey
BaseUrl
Timeout
RetryCount
Backoff
```

Do not create scattered environment-variable access.

Avoid:

```csharp
Environment.GetEnvironmentVariable(...)
```

throughout the codebase.

Use typed options.

---

# 17. HISTORICAL BACKFILL

The pipeline must support historical economic data.

Example:

```text
Last stored observation
        ↓
Request newer observations
        ↓
Normalize
        ↓
Deduplicate
        ↓
Store
```

For initial setup:

```text
FRED
 ↓
Historical range
 ↓
Database
```

The amount of historical data should be configurable.

Do not download unlimited data by default.

---

# 18. INCREMENTAL SYNCHRONIZATION

The system must not download the entire history every time.

Track synchronization state.

Conceptually:

```text
EconomicSyncState
-----------------
Provider
Series
LastSuccessfulSync
LastObservationDate
LastAttempt
Status
Error
```

Example:

```text
Last observation:
2026-09-30

Next sync:
request data after 2026-09-30
```

The exact implementation should fit the existing persistence architecture.

---

# 19. IDEMPOTENCY

Running the same synchronization twice must not create duplicate observations.

Example:

```text
Sync #1
CPI 2026-09-01 → inserted

Sync #2
CPI 2026-09-01 → ignored
```

Use a database uniqueness constraint appropriate to the provider and series.

Possible logical uniqueness:

```text
Provider
+
Series
+
ObservationDate
```

Do not rely only on application-side duplicate checks.

The database should protect integrity.

---

# 20. DATA REVISIONS

Economic data can sometimes be revised.

Do not assume historical observations are permanently immutable.

Design the model so revisions can be handled without destroying auditability.

If a provider returns a changed value:

```text
Old observation
       ↓
Provider revision
       ↓
Updated/current value
```

The exact revision strategy should be documented.

Do not silently overwrite data if doing so would destroy information required for historical analysis.

If revision tracking is not implemented in this phase, document it as a future requirement.

---

# 21. MISSING VALUES

Never convert:

```text
missing
```

into:

```text
0
```

Example:

```text
Value = .
```

must remain unavailable/null.

This is extremely important for economic analysis.

---

# 22. FREQUENCY

Economic series can have different frequencies:

```text
Daily
Weekly
Monthly
Quarterly
Annual
```

Store the provider frequency.

Do not force all economic data into daily candles.

The future macro-analysis layer can later align different frequencies with market timestamps.

---

# 23. TIME HANDLING

Economic observations often have an observation period/date.

System timestamps should remain UTC.

Use explicit semantics.

For example:

```text
ObservationDate
```

means the economic period/date.

While:

```text
FetchedAt
CreatedAt
UpdatedAt
```

are system timestamps.

Do not mix them.

---

# 24. DATA NORMALIZATION

Create a clear normalization boundary.

Example:

```text
FRED format
     ↓
Provider adapter
     ↓
Normalized economic model
     ↓
Application
     ↓
Database
```

Normalize:

* identifiers
* names
* dates
* units
* frequency
* values
* source
* status

Do not allow provider-specific naming to spread throughout the application.

---

# 25. RAW VS NORMALIZED DATA

Follow the architecture established in Phase 3.

If raw provider payload storage already exists, use it appropriately.

Conceptually:

```text
RAW FRED RESPONSE
       ↓
NORMALIZATION
       ↓
ECONOMIC OBSERVATION
```

Do not store huge raw payloads unnecessarily.

If raw storage is not implemented yet, do not introduce a giant generic raw-data system just for this phase.

Keep it maintainable.

---

# 26. APPLICATION SERVICES

Create focused application services/use cases.

Examples:

```text
EconomicData/
├── GetSeries/
├── GetObservations/
├── GetLatest/
├── Synchronize/
└── Status/
```

Follow the existing project conventions.

Do not create one giant:

```text
EconomicService.cs
```

containing every operation.

---

# 27. API ENDPOINTS

Expose our own API.

Possible endpoints:

```text
GET /api/economic-data/series
GET /api/economic-data/series/{id}
GET /api/economic-data/series/{id}/observations
GET /api/economic-data/latest
GET /api/economic-data/status
```

If synchronization is exposed through the API, protect it appropriately according to the project's current authentication state.

Since authentication is not yet the focus, do not invent a complete authorization system in this phase.

Follow the current project architecture.

---

# 28. PAGINATION

Historical economic observations can grow large.

Do not return thousands of records by default.

Support:

```text
page
pageSize
from
to
```

or an equivalent range-based API.

Maximum page size must be enforced.

Example:

```text
?page=1&pageSize=100
```

Do not allow unlimited page sizes.

---

# 29. FILTERING

Support useful filters such as:

```text
series
from
to
frequency
category
```

Do not create unnecessary filters.

The goal is efficient retrieval for future macro-analysis.

---

# 30. CACHING

SQL Server remains the source of truth.

Redis may be used for frequently requested economic data.

Recommended:

```text
FRED
 ↓
SQL Server
 ↓
Redis cache
 ↓
API
```

Do not make Redis the source of truth.

Cache only where there is a demonstrated benefit.

---

# 31. RETRIES

FRED requests may fail because of:

* temporary network failures
* timeout
* rate limits
* provider errors

Implement controlled retry behavior.

Use:

```text
retry
+
exponential backoff
+
maximum retry count
```

Do not retry forever.

Do not retry permanent errors indefinitely.

---

# 32. RATE LIMITING

Respect the provider's limits.

Do not create aggressive polling.

Economic data changes much less frequently than market candles.

The synchronization schedule should reflect the actual frequency of the data.

For example:

```text
Monthly series
→ no reason to request every second
```

---

# 33. FAILURE BEHAVIOR

If FRED is unavailable:

```text
FRED unavailable
      ↓
log structured error
      ↓
preserve previous database data
      ↓
mark sync failure
      ↓
retry later
```

Do NOT:

```text
FRED unavailable
      ↓
delete old economic data
```

Existing valid data must remain available.

---

# 34. OBSERVABILITY

Log:

```text
provider
series
sync start
sync end
records requested
records inserted
records updated
records skipped
duration
status
error code
traceId
```

Never log:

```text
FRED API key
```

Use structured logging.

---

# 35. SYNC RESULT

A synchronization operation should return a useful internal result.

For example:

```text
EconomicSyncResult

Provider
Series
StartedAt
CompletedAt
RecordsReceived
RecordsInserted
RecordsUpdated
RecordsSkipped
Status
Error
```

This makes synchronization measurable and testable.

---

# 36. DATABASE INDEXING

Review the Phase 3 indexes.

Economic observations will commonly be queried by:

```text
Series + ObservationDate
```

and:

```text
Series + date range
```

Create appropriate indexes.

Do not create excessive indexes without a query reason.

---

# 37. PERFORMANCE

The system must support:

```text
large historical ranges
+
incremental updates
+
date-range queries
```

Avoid:

```text
load entire history into memory
```

Prefer:

```text
stream/batch/process
```

where appropriate.

Use reasonable batch sizes.

---

# 38. NO LOOK-AHEAD

Economic data is especially sensitive to look-ahead bias.

The stored data must preserve timing information.

Do not allow a future economic release to appear as if it were known before its release.

Future backtesting will require:

```text
release time
+
availability time
+
observation period
```

Where provider information allows it.

Document limitations if FRED data alone cannot provide complete real-time publication timing for a particular series.

---

# 39. TESTING

Add tests for:

### Provider tests

```text
FRED response mapping
valid observation
missing observation
invalid response
provider error
timeout
```

### Application tests

```text
GetSeries
GetObservations
GetLatest
Synchronize
duplicate handling
incremental sync
```

### Database tests

```text
unique constraint
indexes
foreign keys
decimal precision
date handling
```

### Configuration tests

```text
missing API key
disabled provider
invalid URL
invalid timeout
```

### Failure tests

```text
FRED unavailable
rate limit
timeout
partial failure
```

### Architecture tests

Ensure:

```text
Application
    DOES NOT
depend directly on FRED classes
```

and:

```text
Domain
    DOES NOT
depend on Infrastructure
```

---

# 40. DO NOT CREATE FAKE ECONOMIC DATA

Do not seed fake:

```text
CPI
GDP
NFP
Fed Rate
Treasury Yield
```

values and present them as real data.

For tests, use clearly synthetic test fixtures.

Example:

```text
TEST_ONLY
CPI = 3.2
```

must remain clearly test data.

---

# 41. DOCUMENTATION

Update:

```text
docs/
├── architecture/
│   └── economic-data.md
├── development/
│   └── economic-data.md
└── decisions/
```

Document:

* provider architecture
* FRED adapter
* configuration
* synchronization
* data model
* historical backfill
* incremental sync
* duplicate handling
* revisions
* missing values
* rate limits
* failure behavior
* look-ahead considerations

---

# 42. FRONTEND

Do NOT build the complete trading dashboard yet.

A minimal development/admin page may be created only if useful for verifying the pipeline.

It may show:

```text
Economic Data

Provider: FRED
Status: Connected

Series:
CPI
Core CPI
Federal Funds Rate
10Y Treasury
Unemployment
GDP

Last Sync:
...

Records:
...

Status:
Healthy
```

Do not build the final Signal Center or final analyst UI in this phase.

---

# 43. DOCKER

The implementation must work with the existing Docker architecture.

Do not put secrets directly into:

```text
Dockerfile
docker-compose.yml
```

Use environment injection.

The API container must be able to access:

```text
FRED_API_KEY
```

through the existing configuration system.

---

# 44. SECURITY

Never expose:

```text
FRED_API_KEY
```

to the browser.

Never put it in:

```text
NEXT_PUBLIC_*
```

Never log it.

Never return it from an API endpoint.

Never commit it to Git.

---

# 45. WHAT THIS PHASE MUST NOT DO

Do NOT implement:

```text
❌ OpenAI analysis
❌ AI macro interpretation
❌ BUY/SELL signals
❌ signal confidence
❌ strategy scoring
❌ confluence scoring
❌ trade execution
❌ final signal UI
❌ backtesting logic
❌ win-rate calculation
❌ analyst performance
❌ automatic trading
```

Those belong to later phases.

---

# 46. EXPECTED ARCHITECTURE

The final Phase 8 flow should look approximately like:

```text
                    XAUUSD-AI
                        │
                        ▼
              Economic Data Service
                        │
                        ▼
             IEconomicDataProvider
                        │
                        ▼
          FredEconomicDataProvider
                        │
                        ▼
                     FRED
                        │
                        ▼
                 Raw Response
                        │
                        ▼
                 Normalization
                        │
                        ▼
             Economic Observation
                        │
                        ▼
                  SQL Server
                        │
             ┌──────────┴──────────┐
             ▼                     ▼
          Redis*                   API
                                   │
                                   ▼
                              Future AI
```

`Redis*` is optional and remains a cache.

---

# 47. IMPORTANT DESIGN PRINCIPLE

The future AI must never have to understand FRED's API format.

Bad:

```text
AI
 ↓
FRED JSON
```

Good:

```text
FRED
 ↓
Provider Adapter
 ↓
Our Economic Model
 ↓
Database
 ↓
Macro Analysis
 ↓
AI
```

The application owns the data contract.

---

# 48. DEFINITION OF DONE

Phase 8 is complete only when:

* [ ] `IEconomicDataProvider` exists
* [ ] FRED adapter exists
* [ ] FRED configuration is typed
* [ ] API key is protected
* [ ] economic series can be configured
* [ ] historical observations can be retrieved
* [ ] incremental synchronization works
* [ ] duplicate observations are prevented
* [ ] missing values are handled correctly
* [ ] economic data is normalized
* [ ] SQL Server stores normalized observations
* [ ] appropriate indexes exist
* [ ] synchronization state is tracked
* [ ] retry/backoff exists
* [ ] provider failures are handled
* [ ] structured logging exists
* [ ] own API endpoints exist
* [ ] pagination/range filtering exists
* [ ] tests exist
* [ ] architecture tests pass
* [ ] Docker configuration works
* [ ] documentation is updated
* [ ] no secrets are committed
* [ ] no fake economic data is used as real data
* [ ] no AI signal logic was introduced
* [ ] no business/trading logic was changed

---

# 49. FINAL VALIDATION

Before declaring Phase 8 complete, inspect the existing implementation from Phases 1–7.

Do not overwrite existing architecture unnecessarily.

Reuse existing:

* configuration
* database models
* persistence
* logging
* error handling
* provider patterns
* API conventions
* testing conventions
* Docker configuration

Only add what is actually required.

Keep files focused and maintainable.

Do not create giant classes.

Do not create generic abstractions without a real use case.

At the end, provide a concise implementation report containing:

1. Files created
2. Files modified
3. Database changes
4. APIs added
5. Tests added
6. Configuration added
7. Provider integration status
8. Any limitations
9. Any external action that the user must perform

If an external credential is genuinely required, stop only at that point and clearly identify what the user must provide.

Otherwise, continue implementing the phase automatically.

# END OF PHASE 8
