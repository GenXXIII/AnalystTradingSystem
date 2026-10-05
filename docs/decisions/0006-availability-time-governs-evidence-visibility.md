# ADR 0006: Availability time governs evidence visibility

## Status

Accepted for Phase 10.

## Context

Market candles, news, economic observations, analyst claims, and later
technical observations carry different event, publication, collection, and
revision times. Using one source-specific timestamp directly would allow later
information to leak into historical analysis or would incorrectly delay
evidence that was publicly available before collection.

## Decision

Every normalized record has an explicit `AvailableAtUtc` used by every
historical query. Reads require availability at or before the requested
analysis time and respect the record's validity interval. Publication time is
the boundary for news and analyst claims, candle close is the boundary for a
completed candle, and the known fetch time is the conservative boundary for an
economic observation whose exact release time is absent.

Event/observed, publication, collection, validity, creation, and update times
remain separate. Economic revisions create new evidence versions, close the
old validity interval, and use an `Updates` relation. Unknown release times are
marked `DateOnly`; they are never invented.

## Consequences

Historical evidence packs are reproducible and cannot see records or revisions
that were unavailable at their analysis time. Some economic evidence becomes
available later than its real-world release when the provider lacks an exact
release timestamp; this conservative limitation is visible in timestamp
quality and is preferable to hindsight leakage. Source adapters must assign
availability explicitly when new evidence domains are added.

