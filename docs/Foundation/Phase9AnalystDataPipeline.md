# XAUUSD-AI — PHASE 9

## Analyst Data Pipeline

You are continuing development of the **XAUUSD-AI** project.

Implement **Phase 9 only**.

Do not skip ahead to Phase 10 or later.

The objective of this phase is to build the application's own **Analyst Data Pipeline**.

External analyst sources are inputs.

They are NOT the application's architecture.

The application must own the normalized analyst-data model.

---

# 1. PROJECT CONTEXT

The system analyzes XAUUSD using:

```text
Market Data
Technical Analysis
Market Structure
Liquidity
Candle Flow
Order-Flow Data*
Session Flow
Economic Data
News
Analyst Opinions
Historical Performance
AI Reasoning
```

The strategic architecture is:

```text
Market Data
      ↓
Market Intelligence
      ↓
News
      ↓
Economic Data
      ↓
Analyst Data
      ↓
Evidence Aggregation
      ↓
Future AI Analysis
      ↓
Future Signal Engine
```

Phase 9 adds:

```text
Analyst Sources
      ↓
Analyst Data Collection
      ↓
Normalization
      ↓
Deduplication
      ↓
SQL Server
      ↓
Future Analyst Performance Evaluation
```

Do NOT implement the future AI analyst yet.

---

# 2. MAIN GOAL

Build a reliable pipeline that can collect and store analyst opinions and predictions about:

* XAUUSD
* Gold
* USD
* interest rates
* Fed policy
* Treasury yields
* inflation
* major macro conditions

The system should preserve:

* who made the prediction
* what they predicted
* when they predicted it
* what instrument they predicted it for
* direction
* target
* timeframe/horizon
* reasoning
* source
* source URL
* publication time
* external identifier
* collection time

This information will later allow the system to measure analyst performance.

---

# 3. IMPORTANT PRINCIPLE

Do NOT treat analyst opinions as facts.

Separate:

```text
FACT
```

from:

```text
ANALYST CLAIM
```

from:

```text
AI INTERPRETATION
```

For example:

```text
FACT:
Gold price was X at 10:00 UTC.

ANALYST CLAIM:
Analyst A expects gold to move toward Y.

AI INTERPRETATION:
The analyst view agrees/conflicts with current market structure.
```

Phase 9 stores the analyst claim.

Future AI phases interpret it.

---

# 4. EXTERNAL PROVIDERS ARE INPUTS

Do not build the application around another provider's UI.

The architecture must remain:

```text
YOUR APPLICATION
        │
        ▼
IAnalystDataProvider
        │
        ├── Provider A
        ├── Provider B
        └── Future Provider
```

The rest of the application must not depend directly on provider-specific API models.

For example:

```text
Application
    ↓
IAnalystDataProvider
    ↓
ExternalAnalystProvider
    ↓
External API / legitimate source
```

---

# 5. DO NOT ASSUME A SINGLE ANALYST PROVIDER

The system must support multiple sources.

Examples of source categories:

```text
Financial institutions
Research organizations
Market analysts
Economists
Broker research
Financial publications
Public analyst commentary
```

Only integrate sources that provide legitimate access.

Do not scrape sites merely to bypass:

* authentication
* paywalls
* robots restrictions
* rate limits
* access controls
* terms of service

Do not implement unauthorized data extraction.

---

# 6. PROVIDER ABSTRACTION

Create:

```text
IAnalystDataProvider
```

The interface should support operations conceptually such as:

```text
GetLatestAnalystItemsAsync(...)
GetHistoricalAnalystItemsAsync(...)
GetAnalystItemAsync(...)
SearchAsync(...)
```

Use the project's existing conventions.

Do not expose external-provider response models through Application.

---

# 7. NORMALIZED ANALYST MODEL

The application's normalized analyst item should conceptually contain:

```text
AnalystPrediction
------------------
Id
SourceId
AnalystId
Instrument
AssetClass
Direction
PublishedAt
CollectedAt
Title
Summary
Content
TargetPrice
TargetCurrency
TimeHorizon
Confidence
Reason
SourceUrl
ExternalId
Language
Category
Status
CreatedAt
UpdatedAt
```

Do not blindly add every field if an existing Phase 3 model already covers them.

Reuse the existing database foundation.

---

# 8. ANALYST IDENTITY

An analyst should be represented separately from a prediction where useful.

Conceptually:

```text
AnalystSource
Analyst
Prediction
```

Example:

```text
Source:
Research Organization A

Analyst:
John Smith

Prediction:
Gold expected higher over next 2 weeks
```

This allows future statistics such as:

```text
Analyst
    ↓
Predictions
    ↓
Evaluated outcomes
    ↓
Historical performance
```

Do not calculate performance yet.

---

# 9. SOURCE MODEL

Create or reuse an appropriate source model.

Conceptually:

```text
AnalystSource
-------------
Id
Name
Type
Provider
Website
Country
IsActive
CreatedAt
UpdatedAt
```

Examples of source types:

```text
Bank
Broker
Research
Publication
Independent Analyst
Institution
Other
```

Do not create fake sources.

---

# 10. ANALYST MODEL

Where the source provides an identifiable analyst:

```text
Analyst
-------
Id
SourceId
Name
Role
ProfileUrl
IsActive
CreatedAt
UpdatedAt
```

If the source does not identify an individual analyst, allow:

```text
AnalystId = null
```

Do not invent a person.

---

# 11. PREDICTION MODEL

A prediction should be an immutable historical statement.

Example:

```text
Analyst:
Analyst A

Instrument:
XAUUSD

Published:
2026-10-01 08:00 UTC

Direction:
Bullish

Target:
4,100

Time Horizon:
7 days

Reason:
Expected USD weakness and falling yields
```

This must remain as the original analyst statement.

Do not rewrite it later because the market changed.

---

# 12. DIRECTION

Use a controlled enumeration.

For example:

```text
Bullish
Bearish
Neutral
Unknown
```

Do not force every article into BUY/SELL.

An analyst might say:

```text
Gold expected to remain range-bound.
```

That should be:

```text
Neutral
```

not:

```text
SELL
```

---

# 13. TARGET

Targets must be optional.

An analyst may provide:

```text
Target = 4,100
```

or:

```text
No explicit target
```

Do not invent targets.

Support:

```text
TargetPrice
TargetRangeLow
TargetRangeHigh
```

only if actually available and useful.

---

# 14. TIME HORIZON

Store the analyst's intended horizon.

Examples:

```text
Intraday
1 Day
Several Days
1 Week
2 Weeks
1 Month
3 Months
6 Months
1 Year
Long Term
Unknown
```

Prefer structured duration fields when possible.

For example:

```text
HorizonValue
HorizonUnit
```

This will later make performance evaluation easier.

---

# 15. PUBLICATION TIME

Store:

```text
PublishedAt
```

This is critical.

Later backtesting must not accidentally use analyst information that was published after the signal time.

Also store:

```text
CollectedAt
```

These are different.

Example:

```text
PublishedAt:
08:00 UTC

CollectedAt:
08:03 UTC
```

The analyst prediction became available at publication time, not collection time.

---

# 16. SOURCE URL

Store the original source URL when legally and technically available.

Example:

```text
SourceUrl
```

This allows the UI to show:

```text
View Original Source
```

Do not invent URLs.

Do not modify URLs unnecessarily.

---

# 17. EXTERNAL ID

Where the provider supplies a stable identifier:

```text
ExternalId
```

Store it.

Use it for deduplication.

If no external ID exists, use a carefully designed fallback identity based on available source fields.

Do not use title alone as the identity.

---

# 18. CONTENT STORAGE

Store only content permitted by the source/provider.

Prefer:

```text
Title
Summary
StructuredClaim
```

when full content storage is not permitted.

Do not blindly copy entire copyrighted articles into the database.

The system should retain enough information to identify and analyze the analyst claim without unnecessarily reproducing protected content.

---

# 19. STRUCTURED CLAIM EXTRACTION

The ingestion layer should preserve the analyst's original meaning.

Conceptually extract:

```text
Instrument
Direction
Target
Time Horizon
Reason
PublishedAt
```

Example:

```text
Raw statement:
"Gold could move toward 4,100 over the coming week as
expectations for lower rates increase."

Normalized:

Instrument:
XAUUSD

Direction:
Bullish

Target:
4100

Horizon:
1 week

Reason:
Lower-rate expectations
```

If a field cannot be reliably extracted:

```text
Unknown / null
```

Do not guess.

---

# 20. FACT VS INTERPRETATION

Do not transform analyst statements into application facts.

Example:

Bad:

```text
Gold will rise.
```

Correct:

```text
Analyst A predicts bullish gold movement.
```

The database should preserve attribution.

---

# 21. MULTIPLE PREDICTIONS IN ONE ARTICLE

One article can contain multiple claims.

Example:

```text
Gold:
Bullish

USD:
Bearish

10Y Yield:
Lower
```

Do not necessarily create three unrelated articles.

Use an article/source item with structured claims or prediction records where appropriate.

The data model should preserve the relationship.

---

# 22. DEDUPLICATION

The same analyst opinion may appear across multiple sources.

Example:

```text
Original research
      ↓
Publication A
      ↓
Publication B
      ↓
Publication C
```

Do NOT treat these as three independent analyst opinions.

This is critical for future evidence weighting.

The pipeline should attempt to identify:

```text
Original Source
Related Article
Republished Content
```

where the available data allows.

Do not claim two articles are independent evidence merely because they have different URLs.

---

# 23. ANALYST CONSENSUS

Do NOT calculate a final consensus score in Phase 9.

However, the data model should make future aggregation possible.

Future system:

```text
Analyst A → Bullish
Analyst B → Bullish
Analyst C → Bearish
Analyst D → Neutral
```

Then future AI/evidence engine can determine:

```text
Analyst evidence:
Mixed, with bullish majority
```

But Phase 9 should only collect and normalize the underlying records.

---

# 24. SOURCE RELIABILITY

Do NOT assign arbitrary reliability scores such as:

```text
Analyst A = 92%
Analyst B = 76%
```

There is no basis for that yet.

Future performance measurement must be based on actual historical outcomes.

Phase 9 only stores the evidence needed for that measurement.

---

# 25. ANALYST PERFORMANCE FOUNDATION

The database should preserve enough information to later calculate:

```text
Analyst
 ↓
Prediction
 ↓
Evaluation period
 ↓
Actual market outcome
 ↓
Correct / Incorrect / Partial / Expired
```

Do NOT implement the evaluation engine yet.

Do not calculate:

```text
Win Rate
Accuracy
Profit Factor
```

in Phase 9.

---

# 26. IMMUTABILITY

Original analyst predictions should be historically preserved.

If an analyst later publishes a new prediction:

```text
Old Prediction
       +
New Prediction
```

Do not overwrite the old prediction.

This is essential for unbiased performance measurement.

---

# 27. VERSIONING

If an analyst modifies an existing publication, preserve enough metadata to understand that it was updated.

Possible fields:

```text
OriginalPublishedAt
UpdatedAt
Version
```

Only implement detailed version history if the source provides reliable revision information.

Otherwise document the limitation.

---

# 28. COLLECTION STATE

Track provider synchronization.

Conceptually:

```text
AnalystSyncState
----------------
Provider
LastSuccessfulSync
LastAttempt
Status
Error
```

For individual sources where necessary:

```text
Source
LastPublishedAt
LastExternalId
```

This supports incremental collection.

---

# 29. HISTORICAL COLLECTION

The provider should support historical collection when available.

Example:

```text
Initial:
2026-01-01 → 2026-10-01

Later:
2026-10-01 → now
```

Do not download the complete history every time.

---

# 30. INCREMENTAL COLLECTION

Preferred flow:

```text
Last successful collection
          ↓
Request newer records
          ↓
Normalize
          ↓
Deduplicate
          ↓
Store
          ↓
Update sync state
```

If a provider supports cursor-based pagination, use the cursor.

If it supports date filtering, use the date.

If neither exists, use the provider's supported pagination mechanism.

---

# 31. PAGINATION

External APIs may return many analyst items.

Handle pagination safely.

Do not assume:

```text
page 1 = all results
```

Support:

```text
page
cursor
next page
```

according to provider capabilities.

---

# 32. RATE LIMITING

Respect external source limits.

Implement:

```text
retry
exponential backoff
rate-limit handling
timeout
maximum retries
```

Do not continuously hammer a provider.

---

# 33. FAILURE HANDLING

If an analyst provider fails:

```text
Provider unavailable
        ↓
Log error
        ↓
Preserve existing data
        ↓
Mark synchronization failure
        ↓
Retry later
```

Do not delete historical analyst data.

---

# 34. NORMALIZATION BOUNDARY

Use:

```text
External Analyst Response
          ↓
Provider Adapter
          ↓
Normalized Analyst Model
          ↓
Application
          ↓
SQL Server
```

Do not allow external API DTOs to leak into:

```text
Domain
Application
Frontend
```

---

# 35. API

Expose your own application API.

Possible endpoints:

```text
GET /api/analysts
GET /api/analysts/{id}
GET /api/analyst-sources
GET /api/analyst-predictions
GET /api/analyst-predictions/{id}
GET /api/analyst-predictions/latest
GET /api/analyst-predictions/status
```

Support useful filters:

```text
instrument
direction
source
analyst
from
to
horizon
```

Do not expose provider-specific API formats.

---

# 36. PAGINATION ON OUR API

Do not return thousands of analyst records.

Support:

```text
page
pageSize
```

or cursor pagination.

Enforce a maximum page size.

---

# 37. FILTER EXAMPLE

A future request might be:

```text
GET /api/analyst-predictions
    ?instrument=XAUUSD
    &direction=Bullish
    &from=2026-09-01
    &to=2026-10-01
```

The API should return our normalized model.

Not the external provider's response.

---

# 38. REDIS

SQL Server remains the source of truth.

Redis can cache:

```text
latest analyst predictions
recent analyst items
frequently requested filters
```

Do not cache everything.

Do not make Redis authoritative.

---

# 39. DATABASE INDEXING

Common queries will include:

```text
Instrument + PublishedAt
Source + PublishedAt
Analyst + PublishedAt
Direction + PublishedAt
ExternalId
```

Create indexes based on actual query patterns.

Avoid excessive indexes.

---

# 40. UNIQUE CONSTRAINTS

Where possible, protect duplicates at database level.

For example:

```text
Provider
+
ExternalId
```

may be unique.

If ExternalId is unavailable, use an appropriate fallback identity.

Do not use:

```text
Title
```

alone.

---

# 41. SEARCH

Do not build a complex search engine yet.

Basic filtering is enough.

Future search can support:

```text
Gold
XAUUSD
Fed
CPI
USD
Yields
```

But Phase 9 should remain focused.

---

# 42. LOGGING

Log structured information such as:

```text
Provider
Source
SyncStarted
SyncCompleted
RecordsReceived
RecordsInserted
RecordsSkipped
RecordsUpdated
Duration
Status
Error
TraceId
```

Never log:

```text
API credentials
```

or sensitive authentication information.

---

# 43. TESTING

Create unit tests for:

```text
Provider response mapping
Direction mapping
Target parsing
Horizon parsing
Missing target
Missing analyst
Missing source
Timestamp conversion
```

Integration tests for:

```text
Database insertion
Duplicate prevention
Incremental sync
Pagination
Filtering
```

Failure tests for:

```text
Timeout
Rate limit
Provider error
Malformed response
Missing fields
```

Architecture tests:

```text
Application
    ↓
IAnalystDataProvider
```

must remain independent of provider-specific implementation.

---

# 44. TEST HISTORICAL INTEGRITY

Verify:

```text
Prediction A
published at T1
```

is not replaced by:

```text
Prediction B
published at T2
```

The database must preserve both.

Test that old predictions remain queryable.

---

# 45. NO FAKE ANALYST RESULTS

Do not seed fake predictions such as:

```text
Analyst A → BUY
Analyst B → SELL
```

and present them as real analyst data.

Test fixtures may contain synthetic data, but they must be clearly test-only.

---

# 46. FRONTEND

Do not build the final Signal Center yet.

A small development page may be added for verification:

```text
ANALYST DATA

Sources
────────────────────────────
Source A     Connected
Source B     Connected

Recent Predictions
────────────────────────────
Analyst A
XAUUSD
Bullish
Target: XXXX
Horizon: 1 week
Published: ...

Analyst B
XAUUSD
Bearish
Target: XXXX
Horizon: 3 days
Published: ...
```

The final analyst-analysis UI belongs to later phases.

---

# 47. SECURITY

Protect all external credentials.

Do not expose provider API keys to:

```text
Next.js
Browser
Mobile client
Frontend environment
```

Use server-side configuration.

Never commit credentials.

---

# 48. NO AI YET

This phase must NOT call OpenAI.

Do not ask AI:

```text
Is this analyst correct?
Is this analyst reliable?
Is gold bullish?
Should we BUY?
Should we SELL?
```

Those belong to future analysis/evaluation phases.

---

# 49. NO AUTOMATIC SIGNAL GENERATION

Do not create:

```text
BUY
SELL
WAIT
```

signals from analyst data.

Analyst data is only evidence.

Future system:

```text
Analyst Data
     +
Market Data
     +
Technical Analysis
     +
Economic Data
     +
News
     ↓
Evidence Aggregation
     ↓
Master Analyst
     ↓
Future Signal Engine
```

---

# 50. IMPORTANT: ANALYST DATA ≠ ANALYST ACCURACY

Do not confuse:

```text
Number of bullish analysts
```

with:

```text
Probability of price rising
```

For example:

```text
8 analysts bullish
```

does not automatically mean:

```text
80% chance of price increase
```

The system must later measure actual historical performance.

Phase 9 only collects the raw evidence required for that measurement.

---

# 51. HISTORICAL EVALUATION PREPARATION

Preserve:

```text
Prediction Time
Direction
Target
Horizon
Instrument
Source
Analyst
Reason
```

because future evaluation will need to answer:

```text
What did the analyst predict?
When did they predict it?
For which instrument?
For what horizon?
At what target?
What actually happened afterward?
```

This is one of the most important purposes of Phase 9.

---

# 52. XAUUSD FOCUS

Prioritize analyst information directly related to:

```text
Gold
XAUUSD
XAU
Precious Metals
USD
Federal Reserve
Interest Rates
Treasury Yields
Inflation
Major geopolitical events
```

Do not ingest every financial article on the internet.

The collection pipeline should remain relevant to XAUUSD.

---

# 53. RELEVANCE FILTER

Create a deterministic first-stage relevance filter.

For example:

```text
XAUUSD
Gold
XAU
Gold price
Precious metals
USD
Fed
FOMC
CPI
PCE
NFP
Treasury yields
Interest rates
```

The exact implementation should use the project's existing normalization conventions.

Do not use an LLM just to perform simple keyword filtering in this phase.

---

# 54. SOURCE ATTRIBUTION

Every analyst prediction must preserve:

```text
WHO
WHAT
WHEN
WHERE
```

Meaning:

```text
WHO:
Analyst/source

WHAT:
Prediction

WHEN:
Publication time

WHERE:
Original source
```

This makes future AI reasoning auditable.

---

# 55. DATA FLOW

The complete Phase 9 flow should be:

```text
                    ANALYST SOURCES
                           │
                           ▼
                  Provider Adapters
                           │
                           ▼
                IAnalystDataProvider
                           │
                           ▼
                    Raw Responses
                           │
                           ▼
                    Normalization
                           │
             ┌─────────────┴─────────────┐
             ▼                           ▼
       Relevance Filter             Deduplication
             │                           │
             └─────────────┬─────────────┘
                           ▼
                  Analyst Predictions
                           │
                           ▼
                       SQL Server
                           │
                           ▼
                    Application API
                           │
                           ▼
                 Future AI Analyst
```

---

# 56. EXPECTED ARCHITECTURE

Maintain:

```text
XauAi.Application/
└── Analysts/
    ├── Contracts/
    ├── Models/
    ├── GetAnalysts/
    ├── GetSources/
    ├── GetPredictions/
    ├── GetLatest/
    ├── Synchronize/
    └── Status/
```

Infrastructure should contain provider implementations.

For example:

```text
XauAi.Infrastructure/
└── Analysts/
    ├── Providers/
    │   └── ...
    ├── Mapping/
    └── Configuration/
```

Adapt this to the existing project structure rather than forcing an exact folder tree.

---

# 57. DO NOT CREATE GIANT CLASSES

Avoid:

```text
AnalystService.cs
```

containing:

* API calls
* normalization
* deduplication
* database writes
* filtering
* synchronization
* statistics
* AI analysis

Separate responsibilities.

---

# 58. DOCUMENTATION

Create/update:

```text
docs/
├── architecture/
│   └── analyst-data.md
├── development/
│   └── analyst-data.md
└── decisions/
```

Document:

* analyst provider abstraction
* normalized model
* source attribution
* prediction lifecycle
* deduplication
* historical preservation
* incremental sync
* provider limitations
* copyright/content-storage limitations
* future performance evaluation
* look-ahead prevention

---

# 59. LOOK-AHEAD PROTECTION

This is critical.

An analyst prediction must be timestamped.

Future analysis/backtesting must only be allowed to use:

```text
PublishedAt <= AnalysisTime
```

Never:

```text
PublishedAt > AnalysisTime
```

Do not allow future analyst predictions to leak into historical analysis.

Add tests for this boundary.

---

# 60. FUTURE ANALYST PERFORMANCE

Do NOT implement it yet.

But design for:

```text
Analyst
   ↓
Prediction
   ↓
Evaluation Window
   ↓
Market Outcome
   ↓
Prediction Result
```

Possible future outcomes:

```text
Correct
Incorrect
Partial
Expired
Unresolved
```

Do not force a prediction into correct/incorrect when its target or horizon is ambiguous.

---

# 61. DEFINITION OF DONE

Phase 9 is complete only when:

* [ ] `IAnalystDataProvider` exists
* [ ] provider abstraction is independent from external API models
* [ ] at least one legitimate analyst-data source is integrated OR the adapter boundary is fully implemented if credentials/access are unavailable
* [ ] analyst source model exists
* [ ] analyst model exists
* [ ] prediction model exists
* [ ] direction is structured
* [ ] target is optional
* [ ] time horizon is preserved
* [ ] publication timestamp is preserved
* [ ] collection timestamp is preserved
* [ ] source URL is preserved
* [ ] external ID is preserved
* [ ] analyst attribution is preserved
* [ ] analyst predictions are historically preserved
* [ ] duplicates are prevented
* [ ] incremental synchronization exists
* [ ] historical collection exists where provider supports it
* [ ] relevance filtering exists
* [ ] provider rate limits are respected
* [ ] retry/backoff exists
* [ ] provider failures are handled
* [ ] SQL Server persistence works
* [ ] indexes exist
* [ ] API endpoints exist
* [ ] pagination exists
* [ ] filtering exists
* [ ] look-ahead protection is tested
* [ ] structured logging exists
* [ ] configuration is secure
* [ ] credentials are never exposed
* [ ] tests exist
* [ ] architecture tests pass
* [ ] documentation is updated
* [ ] no AI interpretation was added
* [ ] no BUY/SELL signal generation was added
* [ ] no analyst accuracy score was invented
* [ ] no fake analyst data is presented as real data

---

# 62. EXTERNAL ACTION RULE

If a provider requires:

* API key
* account
* subscription
* authentication
* private access

and the credential/access is genuinely required:

STOP only at that specific integration point.

Clearly report:

```text
Required:
[credential/access]

Where it belongs:
[configuration]

Why it is required:
[reason]

What has already been implemented:
[adapter/pipeline/etc.]
```

Do not ask for credentials that are not actually required.

Never put a credential directly into source code.

---

# 63. FINAL VALIDATION

Before declaring Phase 9 complete:

Inspect Phases 1–8.

Reuse existing:

* configuration
* database
* persistence
* logging
* error handling
* provider abstractions
* API conventions
* testing conventions
* Docker
* documentation patterns

Do not replace working architecture unnecessarily.

Do not change existing business logic.

Keep the implementation maintainable and feature-oriented.

At completion, provide:

1. Files created
2. Files modified
3. Database changes
4. Provider integration status
5. APIs added
6. Configuration added
7. Tests added
8. Documentation added
9. External action required, if any
10. Known limitations

Then stop.

# END OF PHASE 9
