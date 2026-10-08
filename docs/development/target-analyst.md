# Target Analyst development guide

Target Analyst is opt-in. SQL persistence, normalized evidence, market data, and technical analysis must be ready before enabling it. Copy the Phase 13 variables from `.env.example` into the ignored `.env` file.

The generated immutable context also captures the current independent Local Analyst snapshot when one is available. The web result persists from SQL across navigation/reload, blocks replacement while active, and exposes explicit cancellation without deleting history.

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

## Recommended Groq profile

Use Groq for the high-throughput profile. Put only `GROQ_API_KEY` in the ignored `.env`, then run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\set-groq-ai-profile.ps1`. Target uses `openai/gpt-oss-20b` for scouts, specialists, and Risk; only a confirmed Target Master call uses `openai/gpt-oss-120b`. A rate limit, transient failure, or Groq structured-output rejection advances through `qwen/qwen3.8-27b` and the alternate GPT-OSS model. The response's actual model is persisted with each workspace result.

GPT-OSS reasoning is set to its lowest supported effort and Qwen reasoning is disabled. Identical terminal snapshots reuse SQL results for five minutes. New snapshots pass a zero-token market gate, then Structure and Flow scout first; KTR is called only when they need a tie-breaker. Supporting specialists, Risk, and the 120B Master are lazy. Evidence is capped at six items and 2,000 compressed characters per workspace, with a 10,000-token run budget. A workspace-specific key or `TARGET_AI_API_KEY` still takes precedence over `GROQ_API_KEY` when provider isolation is required.

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

The terminal presents Target Analyst beside Full Analyst in a switchable right-side rail. Cancelling removes the active target card while preserving its history and evidence.
