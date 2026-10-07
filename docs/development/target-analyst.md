# Target Analyst development guide

Target Analyst is opt-in. SQL persistence, normalized evidence, market data, and technical analysis must be ready before enabling it. Copy the Phase 13 variables from `.env.example` into the ignored `.env` file.

## Provider-neutral configuration

Each workspace can use a different vendor or model:

```env
TARGET_STRUCTURE_AI_PROVIDER=ProviderName
TARGET_STRUCTURE_AI_ADAPTER=OpenAiCompatible
TARGET_STRUCTURE_AI_API_KEY=secret
TARGET_STRUCTURE_AI_MODEL=model-id
TARGET_STRUCTURE_AI_BASE_URL=https://provider.example/v1/
```

Repeat with `TARGET_LIQUIDITY`, `TARGET_CANDLE`, `TARGET_FLOW`, `TARGET_KTR`, `TARGET_NEWS`, `TARGET_RISK`, and `TARGET_MASTER`. Provider-specific secrets remain server-side. The public configuration endpoint exposes only `hasApiKey`, never the key.

## Current OpenRouter free-router example

OpenRouter uses the OpenAI-compatible chat-completions protocol, so it uses the existing adapter. One shared key can feed all eight workspaces while every workspace remains independently overridable:

```env
TARGET_AI_API_KEY=replace-with-your-openrouter-key

TARGET_STRUCTURE_AI_ENABLED=true
TARGET_STRUCTURE_AI_PROVIDER=OpenRouter
TARGET_STRUCTURE_AI_MODEL=openrouter/free
TARGET_STRUCTURE_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_LIQUIDITY_AI_ENABLED=true
TARGET_LIQUIDITY_AI_PROVIDER=OpenRouter
TARGET_LIQUIDITY_AI_MODEL=openrouter/free
TARGET_LIQUIDITY_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_CANDLE_AI_ENABLED=true
TARGET_CANDLE_AI_PROVIDER=OpenRouter
TARGET_CANDLE_AI_MODEL=openrouter/free
TARGET_CANDLE_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_FLOW_AI_ENABLED=true
TARGET_FLOW_AI_PROVIDER=OpenRouter
TARGET_FLOW_AI_MODEL=openrouter/free
TARGET_FLOW_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_KTR_AI_ENABLED=true
TARGET_KTR_AI_PROVIDER=OpenRouter
TARGET_KTR_AI_MODEL=openrouter/free
TARGET_KTR_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_NEWS_AI_ENABLED=true
TARGET_NEWS_AI_PROVIDER=OpenRouter
TARGET_NEWS_AI_MODEL=openrouter/free
TARGET_NEWS_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_RISK_AI_ENABLED=true
TARGET_RISK_AI_PROVIDER=OpenRouter
TARGET_RISK_AI_MODEL=openrouter/free
TARGET_RISK_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_MASTER_AI_ENABLED=true
TARGET_MASTER_AI_PROVIDER=OpenRouter
TARGET_MASTER_AI_MODEL=openrouter/free
TARGET_MASTER_AI_BASE_URL=https://openrouter.ai/api/v1/

TARGET_ANALYST_ENABLED=true
```

`TARGET_AI_API_KEY` is a shared fallback. Any `TARGET_<WORKSPACE>_AI_API_KEY` takes precedence, so keys can be separated later without code changes. `OPENROUTER_API_KEY`, `AI_API_KEY`, and `OPENAI_API_KEY` are also accepted as compatibility fallbacks, but `TARGET_AI_API_KEY` is clearest for this feature.

The free router may select different eligible free models over time. Structured outputs are still schema-validated. A malformed or unavailable specialist is recorded as failed; a Master failure always becomes `NO VALID TARGET`.

## Trigger and inspect

```http
POST /api/target-analyst/jobs
Content-Type: application/json

{
  "symbol": "XAUUSD",
  "timeframe": "M5"
}
```

The request never places a trade. Use the returned job ID to inspect its result and lifecycle or cancel an active target.
