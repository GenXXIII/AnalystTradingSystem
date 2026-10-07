# 0007: Target analysis is snapshot-owned and provider-neutral

## Status

Accepted

## Decision

Each Target Analyst click creates its own durable analysis job and immutable snapshot. Target-specific specialist results, Master output, evidence links, and lifecycle events are persisted separately from `TradingSignals` and Phase 11 `AiAnalyses`.

The eight workspaces use independent provider configurations through an adapter factory. Provider names and model IDs are configuration data. `OpenAiCompatible` identifies a transport contract and does not make OpenAI, OpenRouter, or another vendor part of the business model.

Master synthesis is evidence-weighted and conflict-aware, not a specialist vote count. Its output must pass deterministic validation before activation. Provider failure, malformed output, stale/look-ahead evidence, inadequate independent support, risk rejection, or low confidence produces `NO VALID TARGET` rather than a fabricated price.

## Consequences

- Target results cannot overwrite Local or future Full Analyst records.
- Provider and prompt changes remain reproducible through snapshot versions.
- Cancellation and monitoring preserve all historical evidence and interpretations.
- Supporting a native provider protocol requires a new adapter, not changes to target orchestration or persistence.
- Target Analyst never creates a trade signal or execution instruction.
