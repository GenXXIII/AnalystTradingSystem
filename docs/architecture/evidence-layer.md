# Evidence layer

## Scope

Phase 10 provides a normalized, attributed, timestamp-aware evidence foundation
for later analysis. It does not perform AI reasoning, choose which source is
correct, calculate trade confidence or probability, or emit trading signals.
SQL Server remains the source of truth; Redis is not used because the bounded
SQL queries and indexes are sufficient for the current workload.

## Flow and ownership

```text
Market / technical / news / economic / analyst records
                         |
                         v
Application normalization and validation
                         |
             valid ------+------ invalid
               |                    |
               v                    v
       identity/content hashes   quarantine
               |
               v
      SQL evidence + relations + clusters
               |
               v
       filtered evidence-pack API
```

Application owns the enums, normalizer, ingestion/query contracts, evidence
pack, conflicts, coverage, and validation errors. Infrastructure maps the
existing source pipelines to that contract and implements SQL persistence. API
returns application DTOs rather than EF entities.

## Evidence contract

An evidence record retains:

- controlled evidence and source types;
- source key, optional provider external ID, identity hash, and content hash;
- canonical and original instrument symbols plus an optional timeframe;
- observed/event, availability, publication, collection, validity, creation,
  and update times in UTC;
- title, summary, fixed-precision numeric value, normalized/original unit,
  direction, importance, category, and optional ISO-style currency code;
- original source URL and JSON metadata for source-specific fields;
- record quality, completeness, timestamp quality, source reliability, and
  deterministic XAUUSD relevance with its reason.

Controlled evidence types are `Market`, `Technical`, `Candle`, `CandleFlow`,
`Liquidity`, `OrderFlow`, `Session`, `News`, `Economic`, and `Analyst`.
Controlled source types are `InternalMarketData`, `InternalTechnicalEngine`,
`NewsProvider`, `EconomicProvider`, `AnalystProvider`, `Manual`, and `Other`.
The richer types can store future deterministic candle-flow, liquidity,
session, and order-flow observations without interpreting them as a trade.

`EvidenceRelations` preserves deterministic relationships: `Duplicate`,
`Republished`, `SameEvent`, `Contradicts`, `Supports`, `Updates`, and
`References`. `EvidenceClusters` and `EvidenceClusterMembers` represent events
without flattening their member records. Phase 10 automatically creates only
relationships it can establish exactly and deterministically.

## Time and look-ahead safety

`AvailableAtUtc` is the universal analysis boundary. Every list, detail,
source, conflict, and pack query applies:

```text
AvailableAtUtc <= asOf/analysisTime
ValidFromUtc is null or <= asOf/analysisTime
ValidToUtc is null or > asOf/analysisTime
```

The source adapters assign availability conservatively:

| Source | Event/observed time | Availability boundary |
| --- | --- | --- |
| Completed market candle | Candle open time | Candle close time |
| News | Provider publication time | Provider publication time |
| Analyst claim | Publication time | Publication time |
| Economic observation | Observation date marker | Provider fetch time |

Collection time is preserved separately for news, analyst, market-fetch, and
economic records. An economic provider observation date is never presented as
an exact release time. Such records use `DateOnly` timestamp quality and the
known fetch time as availability. When an economic value changes, the prior
evidence version receives `ValidToUtc`; a new version is inserted and linked by
an `Updates` relation. A historical pack therefore cannot see a later revision.

## Deduplication, relations, and conflicts

The identity hash uses normalized `source + external ID` when the provider has
a stable ID. Otherwise it uses source, canonical instrument, event time, and a
deterministic content fingerprint. A unique database index on `IdentityHash`
is the final concurrency safeguard.

An exact same-source content fingerprint is deduplicated. Exact matching
content from a different source is retained but linked as `Republished`, so it
does not masquerade as a second independent fact. Deterministic cluster keys
group known related records while preserving every member.

Conflicts are opposite `Bullish` and `Bearish` directions for the same
instrument, timeframe, and category from different sources within the
configured time window. Both records remain. The API reports the disagreement
and never selects a winner.

## Quality, relevance, and missing data

Quality describes record integrity only. A complete record with a stable
external ID, exact timestamp, and known source can be `High`; partial or less
precise data is lowered accordingly. Direction does not affect quality, and no
field represents prediction accuracy or trade confidence.

Relevance is deterministic and retains its reason. Existing source-specific
XAUUSD filters feed relevant records into the evidence model; generic evidence
uses canonical instrument, category, and explicit gold/USD/Fed/macro keywords.
Evidence packs include only relevant records.

Coverage is reported per evidence type as `Available`, `Partial`, or
`Unavailable`, with complete and partial record counts. Missing order-flow or
other provider data remains `Unavailable`; the system does not fabricate it.

## Evidence packs and query performance

An evidence pack contains its instrument, analysis window, primary and
confirmation timeframes, groups for every evidence type, ordered market
timeframe groups with `Primary`, `Confirmation`, or `Context` roles,
deterministic conflicts, and coverage. Defaults are derived from the requested
primary timeframe (for example M15 confirms with M5 and M5 with M1), but callers
can provide another valid confirmation timeframe.

Pack reads are bounded by lookback, relevance, availability, and maximum items
per evidence type. General APIs enforce page and date-range limits. SQL indexes
cover identity, source/external ID, content hash, instrument/time,
type/time, source type/time, and timeframe/time. Historical evidence is not
automatically deleted.

## Limitations

- Exact hashes and supplied deterministic cluster keys do not detect semantic
  paraphrases; no LLM clustering is performed.
- Legacy/provider records can expose only timestamps and source metadata that
  were actually captured. Economic release time remains unknown when FRED does
  not provide it in the observation payload.
- Current technical endpoints calculate results on demand. Only persisted
  technical observations participate in an evidence pack.
- Order-flow remains unavailable until a legitimate source supplies it.
- Quality is not accuracy, relevance is not trade probability, and conflicts
  are not resolved in this phase.

