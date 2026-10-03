# ADR-0001: Clean Architecture and feature boundaries

Status: Accepted  
Date: 2026-10-01

## Context

The roadmap spans market ingestion, deterministic analytics, language-model
reasoning, validation, and multiple external providers. Provider concerns must
not leak into the domain or make future tests require live services.

## Decision

Use four backend projects: Domain, Application, Infrastructure, and API. Keep
Application organized by business feature rather than broad technical buckets.
Use the API as the composition root. Defer MediatR until a later use case has
enough coordination complexity to justify it.

## Consequences

Dependencies point inward, external integrations remain replaceable, and
architecture tests can reject forbidden layer references. Small Phase 1 use
cases remain direct and do not pay for an unnecessary mediator abstraction.
