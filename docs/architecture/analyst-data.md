# Analyst-data architecture

Phase 9 stores attributed analyst claims as evidence. It does not turn a claim
into a fact, an AI conclusion, a reliability score, or a trading signal.

## Boundary and flow

```text
Permitted RSS/Atom source
        |
Infrastructure adapter
        |
IAnalystDataProvider
        |
deterministic normalization and XAUUSD relevance
        |
exact-identity deduplication and revision preservation
        |
SQL Server
        |
application-owned analyst APIs
```

`IAnalystDataProvider` and all provider-neutral request/result models are owned
by `XauAi.Application`. XML DTOs and HTTP behavior remain in Infrastructure.
The interface includes latest, historical, item lookup, search, and status
operations so another legitimate adapter can be added without changing the
Domain, Application services, API, or frontend contracts.

The Phase 9 adapter reads a single operator-configured RSS or Atom feed. No
feed is enabled in committed configuration. The adapter never bypasses a
paywall, robots restriction, authentication requirement, rate limit, or other
access control. An optional bearer credential stays in backend configuration.

## Normalized model

| Concept | SQL table | Purpose |
| --- | --- | --- |
| Provider adapter | `DataProviders` | Identifies the collection mechanism, not the publisher |
| Analyst source | `AnalystSources` | Bank, broker, research organization, publication, institution, or other publisher |
| Analyst identity | `Analysts` | Optional named person associated with one source |
| Source item | `AnalystPublications` | Immutable publication metadata, summary, URL, version, and relationship |
| Prediction | `AnalystStatements` | One structured, attributed claim; the Phase 3 table name is retained for migration compatibility |
| Sync state/run | `AnalystSyncStates`, `AnalystSyncRuns` | Incremental cursor metadata, metrics, failures, and audit history |

One publication can own multiple prediction rows. An unidentified author leaves
`AnalystId` null; the pipeline never invents a person. Targets, ranges,
confidence, and horizon values are nullable when the source does not state them.
Direction and horizon use controlled enums. Original claim text is stored only
from source fields the adapter is permitted to retain.

## Attribution and historical integrity

Every stored prediction retains who, what, when, and where through its analyst
or source, structured claim, `PublishedAtUtc`, and original URL. `CollectedAtUtc`
is separate from publication time. Existing predictions are never rewritten
when a source publishes a later view.

A changed item with the same provider identity becomes a new publication
version linked to its original publication. Exact matching content under a
different identity becomes a `Republished` publication linked to the stored
item and does not create a second independent prediction. The implementation
does not claim semantic equivalence for paraphrases; that deeper evidence
normalization belongs to a later phase.

Database uniqueness protects source identity, analyst identity, publication
identity plus version, and claim identity within a publication. Fallback
publication identity combines source, publication time, title, and content; a
title alone is never used.

## Deterministic extraction

The first-stage filter uses configured XAUUSD-related terms covering gold,
USD, the Federal Reserve, inflation, employment, rates, Treasury yields, and
major geopolitical context. The normalizer conservatively recognizes explicit
direction phrases, target or target-range phrases, and structured duration
phrases. Unreliable values stay `Unknown` or null. No LLM is called.

## Look-ahead protection

Prediction queries always apply:

```text
PublishedAtUtc <= AsOfUtc
```

The API defaults `AsOfUtc` to current UTC and rejects future as-of timestamps.
Historical analysis must pass its analysis time explicitly. Collection time is
never substituted for publication time.

## Explicitly deferred

Phase 9 does not implement analyst accuracy, performance outcomes, consensus,
sentiment, evidence weighting, AI interpretation, BUY/SELL/WAIT generation,
or trade execution. The stored attribution, timestamp, direction, target, and
horizon fields are the foundation for those future phases.
