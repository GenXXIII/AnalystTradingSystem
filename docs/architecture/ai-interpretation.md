# AI evidence interpretation

Phase 11 adds a provider-independent interpretation layer above normalized Phase 10 evidence. It does not participate in market-data ingestion and does not create trading signals.

## Flow

```text
normalized evidence
  -> deterministic availability/relevance selection
  -> cluster/content deduplication and conflict preservation
  -> bounded JSON compression
  -> configured specialist
  -> provider adapter
  -> strict structured-response validation
  -> versioned SQL persistence and evidence links
```

`AvailableAtUtc <= AnalysisTimeUtc` is enforced before a provider request. Evidence validity, symbol, timeframe, relevance, specialist/type compatibility, clusters, content hashes, and bounded lookback are also evaluated deterministically. For technical requests, the requested timeframe is primary, the immediately lower timeframe is confirmation, and higher timeframes are context. Reposts do not add artificial confidence. Opposing evidence is retained as a conflict rather than majority-voted.

## Specialist and provider boundaries

News, Candle, Structure, Liquidity, Flow, KTR, Risk, and Master are independent configurations. Each chooses its own provider attribution, adapter, endpoint, model, key, temperature, timeout, output-token limit, retry count, and request rate. `Provider` records who produced the output; `Adapter` selects the transport protocol. Phase 11 includes an `OpenAiCompatible` chat-completions adapter without making OpenAI a universal dependency. Target Analyst and Full Analyst provider configuration remains in their later phases and is never inherited from these specialists.

Specialists receive compressed evidence and never access SQL or raw HTTP. Provider adapters own authentication, request format, timeout, retry, rate-limit errors, response extraction, and token usage. Keys remain configuration-only and are not stored with `AiAnalysis`, logged, or returned by APIs.

## Validation and safety

The response schema separates facts, interpretations, unknowns, conflicts, and cited evidence IDs. Enum values, confidence, required fields, evidence references, unknown JSON properties, and deterministic measurements are validated before persistence. A reported measurement must exactly match a selected evidence record's value and unit.

The schema has no trade decision, target, entry, stop-loss, take-profit, risk/reward, execution, or active-setup fields. Evidence content is treated as untrusted data so embedded prompt instructions are ignored. Prompt instructions and strict unknown-property rejection enforce the Phase 11 boundary.

## Persistence and lifecycle

The existing `AiAnalyses` foundation stores structured output, specialist, provider/model, prompt version, evidence version, cache/input hashes, analysis time, evidence update time, interpretation fields, execution status, lifecycle status, token use, latency, and safe failure details. `AiAnalysisEvidence` provides the many-to-many evidence trace.

Lifecycle values are `Current`, `Stale`, `Superseded`, and `Invalid`. A successful replacement supersedes the previous current interpretation for the same instrument/specialist/type/timeframe. Evidence changes or validity expiry mark a current result stale. Failures are persisted as invalid and do not interrupt deterministic analysis.

The cache identity includes selected evidence IDs/version, interpretation type, specialist, prompt version, a deterministic non-secret configuration version, provider, adapter, model, and analysis context. The configuration version covers effective workspace and interpretation settings but intentionally excludes the API key. It is persisted in the existing `AnalysisVersion` column and exposed as `configurationVersion`. The exact compressed-input digest is stored separately for traceability. Explicit historical requests use the exact analysis time; current requests use the configured five-minute context window. Identical in-process calls are coalesced and a filtered unique SQL index guarantees only one completed/current row for a cache identity.
