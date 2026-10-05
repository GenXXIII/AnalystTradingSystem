# XAUUSD-AI — PHASE 10

## Data Normalization, Deduplication & Evidence Foundation

You are continuing development of the XAUUSD-AI project.

Implement **Phase 10 only**.

Do not skip ahead to Phase 11.

Do not implement the AI News Analysis yet.

Do not implement the Master AI Reasoning yet.

Do not implement the final BUY/SELL signal engine yet.

The purpose of this phase is to create a clean, reliable, timestamp-aware evidence layer that future AI analysis can consume.

---

# 1. PROJECT CONTEXT

The system currently collects information from multiple domains:

```text
Market Data
    ↓
Technical / Candle Analysis
    ↓
News
    ↓
Economic Data
    ↓
Analyst Data
```

These sources have different:

* formats
* timestamps
* identifiers
* frequencies
* reliability
* duplication patterns
* terminology
* update schedules

Phase 10 creates the normalization and evidence layer.

The target architecture is:

```text
                    XAUUSD-AI
                        │
       ┌────────────────┼────────────────┐
       │                │                │
       ▼                ▼                ▼
 Market Data         News Data       Economic Data
       │                │                │
       └───────────────┬┴────────────────┘
                       │
                       ▼
                 Analyst Data
                       │
                       ▼
              NORMALIZATION LAYER
                       │
                       ▼
              DEDUPLICATION LAYER
                       │
                       ▼
               QUALITY VALIDATION
                       │
                       ▼
              EVIDENCE FOUNDATION
                       │
                       ▼
             Future AI Analysis
```

---

# 2. MAIN OBJECTIVE

Build a unified evidence layer that allows the future AI to consume data consistently.

The system must:

* normalize timestamps
* normalize instruments
* normalize source types
* normalize directions
* normalize categories
* normalize units where appropriate
* identify duplicate records
* identify related records
* preserve original source attribution
* preserve original timestamps
* detect conflicting information
* calculate data-quality metadata
* prevent future-data leakage
* create a consistent evidence contract
* preserve raw historical information
* make evidence queryable efficiently

---

# 3. VERY IMPORTANT

This phase is NOT AI reasoning.

Do NOT ask OpenAI to decide:

```text
BUY
SELL
WAIT
```

Do NOT ask OpenAI:

```text
Which news is important?
Which analyst is correct?
Is gold bullish?
Is gold bearish?
```

Do not calculate final confidence.

Do not calculate final trade probability.

Do not generate trade signals.

Those belong to later phases.

---

# 4. WHAT PHASE 10 SHOULD PRODUCE

At the end of this phase, future AI should be able to request something conceptually like:

```text
Give me all relevant evidence for XAUUSD
between T1 and T2.
```

The system should return normalized evidence such as:

```text
MARKET EVIDENCE
- 15M bearish structure
- liquidity sweep detected
- bearish expansion

NEWS EVIDENCE
- Fed-related headline
- USD-related headline

ECONOMIC EVIDENCE
- CPI observation
- Treasury yield observation
- Fed rate observation

ANALYST EVIDENCE
- Analyst A bullish
- Analyst B neutral
- Analyst C bearish
```

The AI will interpret this later.

---

# 5. EVIDENCE MUST KEEP ITS IDENTITY

Do NOT merge everything into one generic text blob.

Bad:

```text
"Gold is bearish because CPI is high and analysts expect lower prices."
```

Good:

```text
Evidence:
Type = Technical
Source = Internal Technical Engine
Timestamp = ...
Observation = Bearish 15M structure

Evidence:
Type = Economic
Source = FRED
Timestamp = ...
Observation = CPI = ...

Evidence:
Type = Analyst
Source = Analyst X
Timestamp = ...
Observation = Bearish gold outlook
```

The future AI needs to know where every claim came from.

---

# 6. EVIDENCE MODEL

Create or adapt an application-owned evidence model.

Conceptually:

```text
Evidence
--------
Id
EvidenceType
SourceType
SourceId
ExternalId
Instrument
Timestamp
ValidFrom
ValidTo
Title
Summary
Value
Unit
Direction
Importance
Confidence
Quality
OriginalSourceUrl
CreatedAt
```

Do not blindly create every field if existing models already provide equivalent information.

Reuse existing architecture.

---

# 7. EVIDENCE TYPES

Use controlled types.

Recommended:

```text
Market
Technical
Candle
Liquidity
OrderFlow
Session
News
Economic
Analyst
```

Future types may be added.

Do not use arbitrary strings everywhere if an enum/value-object is more appropriate.

---

# 8. SOURCE TYPES

Use controlled source categories.

For example:

```text
InternalMarketData
InternalTechnicalEngine
NewsProvider
EconomicProvider
AnalystProvider
Manual
Other
```

The exact names should follow the existing project conventions.

---

# 9. INSTRUMENT NORMALIZATION

Different providers may refer to gold differently:

```text
XAUUSD
XAU/USD
GOLD
Gold
GOLDUSD
```

The application should normalize these to its internal canonical representation.

For this project:

```text
Canonical Instrument:
XAUUSD
```

Do NOT destroy the original provider symbol.

Store both where useful:

```text
CanonicalSymbol = XAUUSD
OriginalSymbol = XAU/USD
```

This makes debugging possible.

---

# 10. TIMEFRAME NORMALIZATION

Market-related evidence must use consistent timeframe identifiers.

Supported timeframes:

```text
M1
M5
M15
M30
H1
H4
D1
```

Future expansion can support:

```text
W1
MN1
```

if required.

The system must distinguish:

```text
Primary analysis timeframe
Confirmation timeframe
Context timeframe
```

Example:

```text
Primary:
M15

Confirmation:
M5

Higher Context:
H1 / H4 / D1
```

---

# 11. TIME NORMALIZATION

All system timestamps must use UTC.

For example:

```text
2026-10-04T01:30:00Z
```

Do not store ambiguous local timestamps.

However, preserve source-specific publication timestamps where required.

The system must clearly distinguish:

```text
EventTime
PublishedAt
ObservedAt
CollectedAt
CreatedAt
UpdatedAt
```

Do not treat these as the same thing.

---

# 12. CRITICAL: NO LOOK-AHEAD

This is one of the most important requirements of the entire project.

Evidence must only be available to analysis if it was actually available at the analysis timestamp.

Example:

```text
Analysis Time:
10:00 UTC

News Published:
10:05 UTC
```

That news must NOT be included in the 10:00 analysis.

Correct:

```text
PublishedAt <= AnalysisTime
```

Incorrect:

```text
PublishedAt > AnalysisTime
```

Implement tests for this.

---

# 13. ECONOMIC DATA TIMING

Economic observations can have:

```text
Observation Date
Release Time
Availability Time
```

Do not assume:

```text
Observation Date = Release Time
```

where that is not true.

If the provider does not supply exact release timing, preserve that limitation instead of inventing a timestamp.

This is important for future backtesting.

---

# 14. NEWS TIMING

News records should preserve:

```text
PublishedAt
CollectedAt
Source
```

Future AI/backtesting should use:

```text
PublishedAt
```

as the information availability boundary whenever reliable.

---

# 15. ANALYST TIMING

Analyst predictions must use:

```text
PublishedAt
```

not merely:

```text
CollectedAt
```

Example:

```text
Analyst published:
08:00

System collected:
08:04
```

The evidence became available at:

```text
08:00
```

not 08:04.

---

# 16. NORMALIZE DIRECTION

Different sources may use:

```text
BUY
LONG
BULLISH
UP
POSITIVE
```

These should map to:

```text
Bullish
```

Likewise:

```text
SELL
SHORT
BEARISH
DOWN
NEGATIVE
```

should map to:

```text
Bearish
```

And:

```text
NEUTRAL
SIDEWAYS
RANGE
```

can map to:

```text
Neutral
```

However:

Do NOT force ambiguous language into Bullish/Bearish.

Use:

```text
Unknown
```

when appropriate.

---

# 17. NORMALIZE IMPORTANCE

News and economic providers may use different importance systems.

Normalize to a controlled model such as:

```text
Low
Medium
High
Critical
Unknown
```

Do not assume that one provider's "3" equals another provider's "3".

The provider adapter must perform the mapping.

---

# 18. NORMALIZE CATEGORIES

Normalize categories into application-owned categories.

Examples:

```text
Fed
InterestRates
Inflation
Employment
GDP
USD
Treasury
Geopolitical
CentralBank
Commodity
MarketStructure
Liquidity
Technical
Session
```

Provider-specific categories can remain in:

```text
OriginalCategory
```

---

# 19. NORMALIZE CURRENCY

Where currency is available:

```text
USD
EUR
GBP
JPY
CNY
```

Use ISO-style currency codes.

For XAUUSD analysis, USD is particularly important.

Do not infer a currency when the source does not provide enough information.

---

# 20. UNIT NORMALIZATION

Economic values may have different units:

```text
Percent
Index
USD
USD Billions
USD Trillions
Thousands
Millions
```

Preserve the original unit.

If converting units, store enough metadata to understand the conversion.

Never silently change:

```text
3.2%
```

into:

```text
0.032
```

unless the internal contract explicitly defines that representation.

---

# 21. DECIMAL PRECISION

Do not use floating-point arithmetic for financial values where exact decimal representation matters.

Use the project's appropriate decimal types.

Examples:

```text
Gold price
Economic value
Target price
Yield
Percentage
```

should be represented consistently.

---

# 22. DEDUPLICATION

Duplicate data can come from:

```text
same provider
same article
same analyst
republished article
multiple API requests
historical backfill
incremental sync
```

The system must prevent duplicate evidence.

---

# 23. PRIMARY DEDUPLICATION KEY

Use a provider's stable:

```text
ExternalId
```

when available.

For example:

```text
Provider + ExternalId
```

should identify the same external record.

---

# 24. FALLBACK DEDUPLICATION

If no ExternalId exists, use a carefully constructed fingerprint.

Possible inputs:

```text
Source
Instrument
PublishedAt
NormalizedTitle
Analyst
ContentHash
```

Do NOT use title alone.

---

# 25. CONTENT HASH

For text-based records, use a deterministic hash when appropriate.

Example:

```text
NormalizedTitle
+
NormalizedSummary
+
PublishedAt
+
Source
```

→ hash

This can help identify duplicates.

Do not use hashes as a replacement for meaningful source identifiers when the provider already supplies one.

---

# 26. REPUBLISHED CONTENT

The same story can appear on several websites.

Example:

```text
Original source
      ↓
Publication A
      ↓
Publication B
      ↓
Publication C
```

The system should distinguish:

```text
Same event
```

from:

```text
Independent evidence
```

Do not automatically count every copy as independent confirmation.

---

# 27. RELATED EVIDENCE

Add the ability to associate related evidence.

Conceptually:

```text
EvidenceRelation
----------------
EvidenceId
RelatedEvidenceId
RelationType
```

Possible relation types:

```text
Duplicate
Republished
SameEvent
Contradicts
Supports
Updates
References
```

Do not use AI to determine all relationships in this phase.

Deterministic relationships can be established first.

---

# 28. CONFLICT DETECTION

The system should be able to identify basic conflicts.

Example:

```text
Analyst A:
Bullish

Analyst B:
Bearish
```

This is not an error.

It is:

```text
Conflicting Evidence
```

Do not delete either record.

The future AI needs to see disagreement.

---

# 29. SUPPORTING EVIDENCE

Likewise:

```text
Technical:
Bearish

Analyst:
Bearish

News:
USD strength
```

can later be considered supporting evidence.

Phase 10 should preserve the records.

Do not decide the final conclusion.

---

# 30. EVIDENCE QUALITY

Create a quality classification.

Possible:

```text
High
Medium
Low
Unknown
```

Quality must be based on data integrity/source properties, NOT on whether the evidence agrees with a desired trade.

For example:

```text
Valid provider timestamp
Stable source ID
Complete data
Reliable provider
```

may contribute to quality.

But:

```text
Bullish
```

must never automatically mean:

```text
High Quality
```

---

# 31. DO NOT CONFUSE QUALITY WITH ACCURACY

This distinction is critical.

```text
Quality
```

means:

```text
How trustworthy/complete is the data record?
```

while:

```text
Accuracy
```

means:

```text
How well did the prediction perform?
```

Accuracy belongs to later phases.

---

# 32. EVIDENCE RELEVANCE

Create a deterministic relevance layer.

For XAUUSD, relevant areas include:

```text
XAUUSD
Gold
USD
Federal Reserve
FOMC
Interest Rates
Treasury Yields
CPI
PCE
NFP
Employment
Inflation
Major geopolitical events
```

The relevance system should avoid pulling unrelated information into the AI context.

---

# 33. RELEVANCE SCORE

If a relevance score is implemented, clearly define it as:

```text
Data Relevance
```

not:

```text
Trade Probability
```

For example:

```text
Relevance = 0.92
```

can mean:

```text
Highly relevant to XAUUSD
```

It must NOT mean:

```text
92% chance XAUUSD goes up
```

---

# 34. EVIDENCE PACK

Create an application-level evidence query/aggregation model.

Conceptually:

```text
EvidencePack
------------
AnalysisTime
Instrument
PrimaryTimeframe
ConfirmationTimeframe
MarketEvidence
NewsEvidence
EconomicEvidence
AnalystEvidence
```

The exact DTO should follow the project's conventions.

---

# 35. MULTI-TIMEFRAME MARKET EVIDENCE

The evidence pack must preserve:

```text
D1
H4
H1
M30
M15
M5
M1
```

where data exists.

The future analyst needs the complete context.

For example:

```text
D1:
Bullish

H4:
Bullish

H1:
Neutral

M30:
Bearish

M15:
Bearish Setup

M5:
Bearish Confirmation
```

Do not collapse these into one label.

---

# 36. PRIMARY TIMEFRAME

The system should support a requested primary timeframe.

Example:

```text
Primary:
M15
```

with:

```text
Confirmation:
M5
```

and higher-timeframe context:

```text
M30
H1
H4
D1
```

Another analysis can use:

```text
Primary:
M5
```

with:

```text
Confirmation:
M1
```

The architecture must not hard-code only M15.

---

# 37. CANDLE FLOW EVIDENCE

The future technical engine will produce evidence such as:

```text
Bullish Push
Liquidity Sweep
Failed Breakout
Rejection
Bearish Engulfing
Bearish Expansion
```

Phase 10 should store these as structured evidence.

Example:

```text
EvidenceType:
CandleFlow

Sequence:
Bullish Push
→ Liquidity Sweep
→ Failed Breakout
→ Rejection
→ Bearish Expansion
```

Do not convert this directly into SELL.

---

# 38. STRATEGY EVIDENCE

The future technical engine may identify:

```text
Candlestick Strategy
Trend Strategy
Breakout Strategy
Pullback Strategy
Reversal Strategy
Liquidity Strategy
Multi-Timeframe Strategy
```

Phase 10 should be capable of storing these analytical observations without making the final trade decision.

---

# 39. ORDER-FLOW EVIDENCE

Where available:

```text
Delta
CVD
Open Interest
Footprint
Volume
```

should be stored separately.

Do not pretend these values exist if the current provider does not provide them.

Missing data remains missing.

---

# 40. SESSION EVIDENCE

Preserve session context:

```text
Asia
London
New York
Overlap
Close
```

The future AI can later consider:

```text
London liquidity sweep
New York continuation
Asian range breakout
```

Do not hard-code a directional conclusion.

---

# 41. NEWS EVENT CLUSTERING

Multiple headlines may describe one event.

Example:

```text
Headline A:
Fed official comments

Headline B:
Fed official signals rate path

Headline C:
Market reacts to Fed comments
```

These may belong to the same event cluster.

Phase 10 should provide a structure for grouping related events.

Do not use an LLM to summarize the event yet.

---

# 42. ECONOMIC EVENT CLUSTERING

Economic releases may have related records:

```text
CPI
Core CPI
Previous CPI
Forecast CPI
Actual CPI
```

Preserve their relationships.

Do not flatten everything into unrelated numbers.

---

# 43. ANALYST EVIDENCE

Analyst evidence must preserve:

```text
Analyst
Source
Direction
Target
Time Horizon
PublishedAt
Reason
```

Do not convert analyst predictions into market facts.

---

# 44. EVIDENCE SOURCE ATTRIBUTION

Every evidence record must be traceable.

The future UI should be able to answer:

```text
Where did this evidence come from?
```

Possible answer:

```text
Source:
FRED

Series:
Federal Funds Rate

Observation:
...

Timestamp:
...
```

or:

```text
Source:
Analyst X

Published:
...

Original Article:
...
```

---

# 45. API

Create application-owned endpoints as appropriate.

Possible:

```text
GET /api/evidence
GET /api/evidence/{id}
GET /api/evidence/pack
GET /api/evidence/conflicts
GET /api/evidence/sources
```

Support filters:

```text
instrument
evidenceType
sourceType
from
to
timeframe
direction
importance
```

Do not expose internal database entities directly if the architecture uses DTOs.

---

# 46. EVIDENCE PACK API

A future analysis request might conceptually be:

```text
GET /api/evidence/pack
    ?instrument=XAUUSD
    &primaryTimeframe=M15
    &analysisTime=2026-10-04T01:00:00Z
```

The response should include only information available at:

```text
2026-10-04T01:00:00Z
```

This is essential for future backtesting.

---

# 47. DATABASE INDEXING

Important query patterns include:

```text
Instrument + Timestamp
EvidenceType + Timestamp
SourceType + Timestamp
ExternalId
Source + ExternalId
PrimaryTimeframe + Timestamp
```

Create indexes based on actual queries.

Do not over-index.

---

# 48. DATA RETENTION

Do not automatically delete historical evidence.

Historical data is important for:

```text
Backtesting
Accuracy
Analyst evaluation
AI evaluation
Performance analysis
```

Retention rules should be configurable later.

---

# 49. RAW DATA PRESERVATION

Do not destroy useful source information during normalization.

Keep:

```text
OriginalSymbol
OriginalCategory
OriginalDirection
OriginalTimestamp
Source
ExternalId
```

where useful.

This allows debugging normalization mistakes.

---

# 50. DATA QUALITY VALIDATION

Validate:

```text
Required identifiers
Valid timestamp
Valid instrument
Valid source
Valid numeric values
Valid direction
Valid timeframe
Valid URLs where applicable
```

Invalid data should be:

```text
Rejected
or
Quarantined
```

rather than silently accepted.

---

# 51. QUARANTINE

If the project already has an ingestion-error system, reuse it.

Otherwise create a lightweight mechanism for invalid external records.

Conceptually:

```text
Incoming Record
      ↓
Validation
      │
      ├── Valid → Normalize → Store
      │
      └── Invalid → Quarantine / Error Log
```

Do not discard problematic data without traceability.

---

# 52. IDEMPOTENCY

The same synchronization request executed twice must produce the same database state.

Example:

```text
Sync 1
100 records inserted

Sync 2
same 100 records

Result:
0 duplicates
```

Test this.

---

# 53. BATCH PROCESSING

Large normalization jobs must not load everything into memory.

Prefer:

```text
Fetch
 ↓
Batch
 ↓
Normalize
 ↓
Deduplicate
 ↓
Persist
 ↓
Next batch
```

Use reasonable batch sizes.

---

# 54. PERFORMANCE

Do not make the evidence layer perform expensive operations unnecessarily.

Avoid:

```text
O(all historical data)
```

for every request.

Use:

* database indexes
* date filtering
* pagination
* bounded queries
* caching where appropriate

---

# 55. REDIS

If Redis already exists in the project:

Use it only where useful.

Possible cache:

```text
Recent Evidence Pack
Latest Market Context
Latest Economic Data
Latest News
Latest Analyst Data
```

SQL Server remains the source of truth.

---

# 56. LOGGING

Log:

```text
NormalizationStarted
NormalizationCompleted
RecordsReceived
RecordsNormalized
RecordsRejected
RecordsDeduplicated
ConflictsDetected
EvidencePackRequested
Duration
Provider
TraceId
```

Never log API keys.

---

# 57. TESTING

Create tests for:

### Timestamp

```text
Future evidence rejected
Past evidence accepted
Timezone conversion
Publication vs collection time
```

### Instrument

```text
XAU/USD → XAUUSD
GOLD → XAUUSD
```

where such mapping is intentionally configured.

### Direction

```text
BUY → Bullish
SELL → Bearish
Neutral → Neutral
Ambiguous → Unknown
```

### Deduplication

```text
same ExternalId → duplicate
same content fingerprint → possible duplicate
different source → not automatically duplicate
```

### Conflicts

```text
Bullish + Bearish
→ conflicting evidence
```

### Evidence quality

```text
missing timestamp
missing source
invalid value
```

must be handled correctly.

### Look-ahead

```text
Analysis 10:00
Evidence 10:05

→ MUST NOT appear
```

---

# 58. INTEGRATION TEST

Build an end-to-end test:

```text
News
+
Economic
+
Analyst
+
Market
        ↓
Normalization
        ↓
Deduplication
        ↓
Evidence Store
        ↓
Evidence Pack
```

Verify that the final evidence pack is correctly structured.

---

# 59. NO FINAL SIGNAL

Even if evidence strongly suggests:

```text
SELL
```

Phase 10 must NOT produce:

```text
SELL — READY
```

The output is:

```text
Evidence Pack
```

Only.

---

# 60. FUTURE AI INPUT

The future AI should eventually receive something like:

```text
XAUUSD
Analysis Time: 10:00 UTC
Primary TF: M15
Confirmation TF: M5

MARKET STRUCTURE
D1: Bullish
H4: Bullish
H1: Neutral
M30: Bearish
M15: Bearish
M5: Bearish

LIQUIDITY
Previous High: ...
Equal High: Detected
Sweep: Detected

CANDLE FLOW
Bullish Push
→ Liquidity Sweep
→ Failed Breakout
→ Rejection
→ Bearish Expansion

NEWS
...

ECONOMIC
...

ANALYST
...

CONFLICTS
...

DATA QUALITY
...
```

This is the foundation for Phase 11+.

---

# 61. AI MUST KNOW UNKNOWN DATA

Do not hide missing information.

For example:

```text
Order Flow:
Unavailable
```

is better than:

```text
Order Flow:
Bullish
```

when no order-flow provider exists.

The AI must know what it does NOT have.

---

# 62. SOURCE CONFIDENCE

Do not create arbitrary "AI confidence" yet.

However, the evidence can contain data-quality metadata.

Example:

```text
Source Reliability:
Known / Unknown

Data Completeness:
Complete / Partial

Timestamp Quality:
Exact / Approximate / Unknown
```

These describe the data.

They are not trade confidence.

---

# 63. FINAL EVIDENCE QUALITY SUMMARY

The system should be able to summarize:

```text
Evidence Coverage
────────────────────────────
Market Data       Available
Technical Data    Available
News              Available
Economic Data     Available
Analyst Data      Available
Order Flow        Partial
```

This is useful later because the AI should not pretend to have complete information.

---

# 64. SECURITY

Do not expose:

```text
API keys
provider credentials
internal database credentials
private configuration
```

to the frontend.

Use server-side services.

---

# 65. DOCKER

Phase 10 must continue working with the existing Docker architecture.

Do not create unnecessary containers.

Reuse:

```text
API
Web
SQL Server
Redis
```

where appropriate.

---

# 66. DOCUMENTATION

Update:

```text
docs/
├── architecture/
│   └── evidence-layer.md
├── development/
│   └── normalization.md
└── decisions/
```

Document:

* evidence model
* normalization rules
* deduplication
* timestamp rules
* look-ahead protection
* source attribution
* conflict handling
* quality handling
* relevance
* evidence-pack API
* limitations

---

# 67. DEFINITION OF DONE

Phase 10 is complete only when:

* [ ] Unified evidence model exists
* [ ] Evidence types are structured
* [ ] Source types are structured
* [ ] Instrument normalization exists
* [ ] Timeframe normalization exists
* [ ] Direction normalization exists
* [ ] Category normalization exists
* [ ] Importance normalization exists
* [ ] Unit handling exists
* [ ] UTC handling is consistent
* [ ] Publication time is preserved
* [ ] Collection time is preserved
* [ ] Look-ahead protection exists
* [ ] Duplicate detection exists
* [ ] Database uniqueness protection exists
* [ ] Content fingerprinting exists where appropriate
* [ ] Related evidence can be represented
* [ ] Conflicting evidence can be represented
* [ ] Evidence quality metadata exists
* [ ] Relevance filtering exists
* [ ] Market evidence can be represented
* [ ] Candle-flow evidence can be represented
* [ ] Liquidity evidence can be represented
* [ ] Session evidence can be represented
* [ ] News evidence can be represented
* [ ] Economic evidence can be represented
* [ ] Analyst evidence can be represented
* [ ] Evidence Pack exists
* [ ] Evidence Pack API exists
* [ ] Filtering exists
* [ ] Pagination/range limiting exists
* [ ] SQL indexes are appropriate
* [ ] Redis is used only where useful
* [ ] Invalid records are handled
* [ ] Structured logging exists
* [ ] Integration tests exist
* [ ] Look-ahead tests pass
* [ ] Deduplication tests pass
* [ ] Architecture tests pass
* [ ] Documentation is updated
* [ ] No AI reasoning was added
* [ ] No BUY/SELL engine was added
* [ ] No fake confidence was added
* [ ] No future information leaks into historical analysis

---

# 68. FINAL VALIDATION

Before declaring Phase 10 complete:

Inspect all previous phases.

Do not replace working implementations unnecessarily.

Reuse existing:

* Domain
* Application
* Infrastructure
* database
* Redis
* configuration
* logging
* API conventions
* provider abstractions
* tests
* Docker
* documentation

Keep the code maintainable.

Do not create giant files.

Do not create giant services.

Do not duplicate existing functionality.

Do not change unrelated business logic.

At completion, provide:

1. Files created
2. Files modified
3. Database changes
4. New APIs
5. Normalization rules
6. Deduplication strategy
7. Evidence model
8. Evidence Pack example
9. Tests added
10. Documentation added
11. External action required, if any
12. Known limitations

Then stop.

# END OF PHASE 10
