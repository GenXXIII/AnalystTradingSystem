# Local analyst development guide

The engine is opt-in and disabled by default in base application settings. Enable it only after market-data and technical-analysis collection are configured:

```env
LOCAL_ANALYST_ENABLED=true
LOCAL_ANALYST_SYMBOL=XAUUSD
LOCAL_ANALYST_TIMEFRAMES=M1,M5,M15,M30,H1,H4,D1
LOCAL_ANALYST_EVALUATION_INTERVAL_SECONDS=30
LOCAL_ANALYST_HISTORY_LIMIT=250
LOCAL_ANALYST_MINIMUM_CANDLES=205
LOCAL_ANALYST_MAXIMUM_ALLOWED_GAPS=30
LOCAL_ANALYST_ENTRY_SCORE_THRESHOLD=3
LOCAL_ANALYST_MINIMUM_DIRECTIONAL_LEAD=2
LOCAL_ANALYST_VALIDITY_CANDLES=M1:120,M5:72,M15:32,M30:24,H1:12,H4:8,D1:5
```

All `LOCAL_ANALYST_*` variables are listed in `.env.example`. Component weights, RSI confirmation ranges, ATR invalidation/target multipliers, stale/gap limits, volatility handling, and validity durations are configuration values; no secret or external AI provider configuration is needed.

Use these endpoints:

- `GET /api/local-analyst/status`
- `POST /api/local-analyst/XAUUSD/M5/evaluate`
- `GET /api/local-analyst/XAUUSD/M5/current`
- `GET /api/local-analyst/XAUUSD/history?timeframe=M5&limit=100`
- `GET /api/local-analyst/signals/{signalId}/lifecycle`

The evaluation endpoint uses a closed candle and is safe to call repeatedly: unchanged-candle calls return a cached persisted snapshot unless the configuration changed or the prior result was a retryable market-data failure. That exception lets scheduled candle backfill repair `INSUFFICIENT_HISTORY`, `STALE_MARKET_DATA`, or `MARKET_DATA_GAPS` without waiting for the next H4/D1 candle. The Phase 12 v5 defaults surface possible directional setups at a three-component score while retaining a two-point directional lead, closed-candle confirmation, data-quality validation, invalidation, target, and stop gates. Local Analyst does not use AI, an AI API key, or automatic order placement. A `NOTHING` response is an intentional decision and includes a deterministic reason such as `SCORE_BELOW_THRESHOLD`.
