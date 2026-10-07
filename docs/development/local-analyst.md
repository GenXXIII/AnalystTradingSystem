# Local analyst development guide

The engine is opt-in and disabled by default in base application settings. Enable it only after market-data and technical-analysis collection are configured:

```env
LOCAL_ANALYST_ENABLED=true
LOCAL_ANALYST_SYMBOL=XAUUSD
LOCAL_ANALYST_TIMEFRAMES=M1,M5,M15,M30,H1,H4,D1
LOCAL_ANALYST_EVALUATION_INTERVAL_SECONDS=30
LOCAL_ANALYST_HISTORY_LIMIT=250
LOCAL_ANALYST_MINIMUM_CANDLES=205
LOCAL_ANALYST_ENTRY_SCORE_THRESHOLD=4
LOCAL_ANALYST_VALIDITY_CANDLES=M1:15,M5:12,M15:8,M30:6,H1:5,H4:4,D1:3
```

All `LOCAL_ANALYST_*` variables are listed in `.env.example`. Component weights, RSI confirmation ranges, ATR invalidation/target multipliers, stale/gap limits, volatility handling, and validity durations are configuration values; no secret or external AI provider configuration is needed.

Use these endpoints:

- `GET /api/local-analyst/status`
- `POST /api/local-analyst/XAUUSD/M5/evaluate`
- `GET /api/local-analyst/XAUUSD/M5/current`
- `GET /api/local-analyst/XAUUSD/history?timeframe=M5&limit=100`
- `GET /api/local-analyst/signals/{signalId}/lifecycle`

The evaluation endpoint uses a closed candle and is safe to call repeatedly: unchanged-candle calls return a cached persisted snapshot. It does not place an order. A `NOTHING` response is an intentional decision and includes a deterministic reason such as `INSUFFICIENT_HISTORY`, `STALE_MARKET_DATA`, `MARKET_DATA_GAPS`, or `SCORE_BELOW_THRESHOLD`.
