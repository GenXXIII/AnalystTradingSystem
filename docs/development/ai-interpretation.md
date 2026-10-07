# AI interpretation development guide

All specialists are disabled by default. Configure only the specialist that should run; one specialist's provider settings do not affect another.

The environment prefix is the specialist name followed by `_AI_`. For example:

```env
NEWS_AI_ENABLED=true
NEWS_AI_PROVIDER=ProviderA
NEWS_AI_ADAPTER=OpenAiCompatible
NEWS_AI_REQUIRES_API_KEY=true
NEWS_AI_API_KEY=USER_PROVIDED_LATER
NEWS_AI_MODEL=provider-model
NEWS_AI_BASE_URL=https://provider.example/v1/
NEWS_AI_TEMPERATURE=0.2
NEWS_AI_TIMEOUT_SECONDS=60
NEWS_AI_MAX_OUTPUT_TOKENS=2000
NEWS_AI_MAX_RETRIES=2
NEWS_AI_REQUESTS_PER_MINUTE=10
```

The same fields exist for `CANDLE`, `STRUCTURE`, `LIQUIDITY`, `FLOW`, `KTR`, `RISK`, and `MASTER`. A base URL ending in `/chat/completions` is used directly; otherwise the adapter appends `chat/completions`.

General limits use `AI_INTERPRETATION_*`: prompt version, default/maximum lookback, maximum selected evidence, maximum compressed characters, current-context cache minutes, and maximum API page size. Current-time calls reuse unchanged evidence inside that short cache window; explicit historical calls retain the exact analysis timestamp. Changing the prompt version or any non-secret effective workspace setting produces a new configuration version and cache identity while preserving older interpretations. API keys are deliberately excluded from that persisted fingerprint.

Specialist/type combinations are deliberate: News accepts news, macro, analyst-claim, and geopolitical interpretation modes; Candle, Structure, Liquidity, Flow, and KTR accept technical evidence; Risk accepts geopolitical risk or evidence synthesis; Master accepts evidence synthesis. Unsupported combinations fail before a provider call.

Run a specialist with `POST /api/ai-interpretations`. Historical analysis timestamps are supported, but future timestamps are rejected before storage or provider access. Use `GET /api/ai-interpretations/specialists` to verify safe effective configuration; it reports only whether a key exists, never the key.

Provider failures return and persist stable Phase 11 failure codes. They do not disable market data, technical analysis, or other deterministic pipelines. Normal CI uses fake HTTP/provider implementations and never calls a paid provider.
