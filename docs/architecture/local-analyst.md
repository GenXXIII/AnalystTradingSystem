# Local analyst and deterministic signal engine

Phase 12 adds a continuous, deterministic XAUUSD local analyst. It is independent of Phase 11 AI specialists, Target Analyst, Full Analyst, Telegram, and trade execution.

```text
completed normalized candle
  -> changed-candle checkpoint
  -> data-quality gate
  -> local technical analysis
  -> deterministic structure / liquidity / candle / momentum / KTR conditions
  -> configurable score
  -> NOTHING | BUY | SELL | STOP
  -> SQL lifecycle event and checkpoint
```

Only completed candles are requested. Missing history, invalid or incomplete OHLC, duplicates, gaps beyond the configured limit, and stale data result in `NOTHING` with a data-quality reason. No provider, AI, or order-book inference is made by this flow.

Signals use a public `LOCAL-...` signal identifier and retain their origin candle. An active BUY or SELL is updated on later completed candles rather than duplicated. It ends as STOP on target, invalidation, opposing configured confluence, or expiry. `TradingSignalLifecycleEvents` preserves the append-only audit and `LocalAnalystProcessingStates` stores the per-symbol/timeframe checkpoint and last decision.

The Phase 12 rules combine existing deterministic EMA/RSI/ATR, swing/HH-HL-LH-LL, support/resistance, and candle components. The local layer classifies BOS and CHoCH from a closed-candle break of the previous swing, derives liquidity only from price/tick-volume evidence, includes prior-session extremes where available, and records each condition used in its score. Support/resistance zones carry their deterministic source, creation time, and active status.

The worker evaluates only when it sees a changed completed candle. A checkpoint makes repeated polling return the persisted snapshot without repeating indicator analysis or opening another signal. SQL further enforces one active LocalAnalyst signal for an instrument/timeframe.
