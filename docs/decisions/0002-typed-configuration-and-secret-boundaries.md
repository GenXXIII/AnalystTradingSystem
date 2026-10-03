# ADR-0002: Typed configuration and secret boundaries

Status: Accepted  
Date: 2026-10-01

## Context

Future phases need multiple replaceable providers, but Phase 2 must not create
clients or require credentials before a feature is enabled. Raw environment
access spread across services would make validation, testing, and secret safety
inconsistent.

## Decision

Bind provider-neutral Infrastructure option classes from environment-specific
configuration at the API composition root. Use one central friendly environment
variable adapter, feature-local `Enabled` gates, conditional validators, and
startup validation. Keep browser configuration limited to
`NEXT_PUBLIC_API_URL`. Require explicit positive AI budget and request limits
before AI can be enabled.

Provider interfaces will be introduced with their use cases in the integration
phases rather than as empty Phase 2 abstractions.

## Consequences

Disabled integrations need no credentials, enabled integrations fail early with
safe messages, and later providers can change without redesigning application
configuration. Production must explicitly supply a trusted CORS origin. The
configuration layer prepares cost controls but does not yet enforce usage or
make external calls.
