# Target Analyst architecture

Phase 13 adds a user-triggered, predictive Target Analyst that is independent of Local Analyst, Full Analyst, trade execution, and Telegram. A request creates a durable job before any AI call and can finish only as one validated active target or `NO VALID TARGET`.

```text
user request
  -> ANALYZING job
  -> immutable market/evidence/configuration snapshot
  -> Structure, Liquidity, Candle, Flow, KTR, News specialists
  -> Risk review of the candidate set
  -> Master evidence synthesis (never a vote count)
  -> deterministic validation gate
  -> ACTIVE one-target result | NO_VALID_TARGET
```

The snapshot contains completed multi-timeframe candles, deterministic technical summaries, normalized evidence IDs, data versions, provider information, prompt versions, and AI configuration versions. Evidence selection enforces `AvailableAtUtc <= AnalysisTimeUtc`; historical AI interpretations are included only when their analysis time is not later than the target snapshot.

Every AI workspace has an independent provider, adapter, key, model, base URL, temperature, timeout, output-token limit, retry limit, rate limit, prompt version, and configuration version. The provider factory is extensible: the current `OpenAiCompatible` adapter names a wire protocol, not a fixed vendor. OpenRouter and any provider exposing that protocol can use it; a future native protocol is added as another adapter without changing the Target Analyst domain or orchestration.

Specialists return at most one cited candidate. Risk can reject it. Master returns exactly one target/invalidation pair or an explicit no-target reason. A separate deterministic gate verifies confidence, target distance, direction context, invalidation placement, validity duration, independent support, risk acceptance, citations, staleness, and look-ahead safety.

Active targets are monitored from completed market candles. Invalidation is evaluated conservatively before target hit when both levels occur inside one candle. Terminal states are `TARGET_HIT`, `INVALIDATED`, `EXPIRED`, or `CANCELLED`. All snapshot, evidence, specialist, Master, and lifecycle rows remain in SQL history.

The API surface is:

- `POST /api/target-analyst/jobs`
- `GET /api/target-analyst/jobs/{id}`
- `GET /api/target-analyst/active/{symbol}`
- `GET /api/target-analyst/history/{symbol}`
- `GET /api/target-analyst/jobs/{id}/lifecycle`
- `POST /api/target-analyst/jobs/{id}/cancel`
- `GET /api/target-analyst/configuration` (secret-free)

No endpoint emits BUY, SELL, WAIT, execution instructions, orders, or Telegram messages.
