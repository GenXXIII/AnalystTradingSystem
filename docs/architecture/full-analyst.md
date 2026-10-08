# Full Analyst architecture

Phase 14 adds an application-owned, user-triggered Full Analyst for XAUUSD. It produces exactly one current-state decision: `BUY`, `SELL`, or `WAIT`. It is independent from the deterministic Local Analyst, the Phase 13 Target Analyst, and the external Telegram Scanner.

## Flow

1. `POST /api/full-analyst/jobs` creates an `Analyzing` job.
2. The application captures an immutable multi-timeframe snapshot using completed candles at or before `AnalysisTimeUtc`.
3. Phase 11 evidence is selected, deduplicated, ranked, validated with `AvailableAtUtc <= AnalysisTimeUtc`, and compressed per workspace.
4. Structure, Liquidity, Candle, Flow, KTR, and News run asynchronously. Risk then evaluates the scoped results. Full Master synthesizes evidence quality and conflicts without majority voting.
5. Malformed output, disabled/failed workspaces, missing or stale data, insufficient evidence, weak confidence, and invalid lifecycle values safely resolve to `WAIT`.
6. Valid `BUY` or `SELL` results become `Active`. A monitoring worker expires or invalidates them. User cancellation stops monitoring without deleting the snapshot or AI outputs.

## Persistence and audit

`FullAnalyses` stores the snapshot, snapshot/state hash, evidence IDs, structured workspace outputs, master decision, confidence, agreement, conflicts, invalidation, provider/model/prompt versions, token counts, and lifecycle status. `FullAnalysisLifecycleEvents` is append-only.

Workspace output caching is keyed by market/evidence state plus prompt and configuration versions. A repeated state can reuse a valid interpretation without a provider call. Market-data polling alone never triggers AI execution.

## Phase boundary

Phase 14 exposes `futureAvailable` only for `BUY` and `SELL`. It does not generate a Future path; that belongs to Phase 15. It creates no target object, sends no Telegram signal, and performs no trade or account operation.
