# ADR-0003: SQL Server persistence and evidence model

Status: Accepted

Date: 2026-10-01

## Context

The system will eventually ingest time-series, news, macro, and analyst data;
derive deterministic observations; run versioned AI and strategy analyses; and
measure signal/backtest outcomes. The schema must preserve what was known and
which versions produced a result without implementing those later workflows now.

## Decision

Use SQL Server with EF Core code-first migrations as the authoritative store.
Use GUID internal keys with SQL Server sequential generation for entity-owned
keys. Normalize provider, instrument, and timeframe identities. Store financial
values as fixed-point decimals and semantic timestamps as UTC-normalized
`datetimeoffset(7)` values.

Represent raw and derived inputs with a common `EvidenceRecord` identity backed
by concrete, foreign-key-constrained tables. Link AI analyses, strategy
evaluations, and signals to evidence with explicit join tables. Keep strategy
versions, AI configuration versions, signal outcomes, backtest trades, and
performance statistics as separate historical records.

Use a unique clustered candle identity on instrument, timeframe, open time, and
provider. Use extensible technical-observation rows instead of a column per
indicator. Keep permitted article content separate from news metadata.

Run Docker migrations as a one-shot administrator process; run the API with a
restricted read/write login. Production migration execution remains a deployment
responsibility.

## Consequences

- Overlapping market synchronization is idempotent at the database boundary.
- Evidence and historical versions can be traced without rewriting prior output.
- Adding indicators does not require adding a column for every parameter set.
- The common evidence identity adds one row and join operation per persisted
  evidence item, traded for referential integrity and explainability.
- GUID clustered-index fragmentation is avoided for candles by clustering on the
  time-series business identity; other owned GUIDs use sequential generation.
- Future partitioning remains possible but is intentionally deferred until real
  volume and retention requirements exist.
