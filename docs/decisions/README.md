# Architecture decision records

Significant decisions are stored here as immutable, numbered records. When a
decision changes, add a new record that supersedes the old one instead of
silently rewriting history.

Use this shape:

```text
# ADR-NNNN: Title
Status: Proposed | Accepted | Superseded
Date: YYYY-MM-DD

## Context
## Decision
## Consequences
```

Current records:

- [ADR-0001: Clean Architecture and feature boundaries](0001-clean-architecture.md)
- [ADR-0002: Typed configuration and secret boundaries](0002-typed-configuration-and-secret-boundaries.md)
- [ADR-0003: SQL Server persistence and evidence model](0003-sql-server-persistence-and-evidence-model.md)
- [ADR-0004: Current economic observations with append-only revisions](0004-economic-observation-revisions.md)
