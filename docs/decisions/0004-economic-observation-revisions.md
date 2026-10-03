# ADR 0004: Current economic observations with append-only revisions

Status: Accepted
Date: 2026-10-03

## Context

Economic series values can be revised. The current read path needs a simple,
indexed identity, while future historical analysis must be able to reconstruct
what value the application previously received and when it changed.

## Decision

Keep one current `EconomicObservations` row per economic series and observation
date, protected by a unique SQL index. Before changing any provider value,
status, original representation, or real-time date, copy the complete previous
version into append-only `EconomicObservationRevisions` with its original fetch
time and the UTC superseded time.

Missing provider values remain nullable and retain their original marker. The
incremental cursor overlaps a configurable recent range so revisions can be
discovered without downloading unlimited history.

## Consequences

Current range and latest-value queries remain direct and efficient. Historical
reconstruction requires combining the current row with its revisions, but no
previously observed value is silently destroyed. FRED observation dates alone
are not treated as publication timestamps; future backtests must honor fetch,
revision, and dedicated release timing.
