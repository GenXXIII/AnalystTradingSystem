# Full Analyst development guide

Full Analyst is disabled by default. Enable `FULL_ANALYST_ENABLED` only after SQL persistence, technical analysis, evidence ingestion, and all eight Full AI workspaces are configured.

Each workspace uses its own `FULL_<WORKSPACE>_AI_*` settings for provider, API key, primary model, ordered fallback models, base URL, timeout, token limit, temperature, rate limit, prompt version, and configuration version. `FULL_AI_API_KEY` is an optional Full-only shared key; a workspace-specific key takes precedence. Full workspaces do not implicitly read Target Analyst credentials.

The recommended Groq profile is applied with `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\set-groq-ai-profile.ps1` after `GROQ_API_KEY` is set in `.env`. Future stays independent on `openai/gpt-oss-20b`, with automatic fallbacks to `qwen/qwen3.8-27b` and `openai/gpt-oss-120b`. Identical terminal snapshots reuse SQL results for five minutes. New snapshots pass a zero-token market gate; Structure and Flow scout first, KTR is only a tie-breaker, and the remaining specialists, Risk, and Master are lazy. Evidence is capped at six items and 2,000 compressed characters per workspace, with a 9,000-token run budget. A dedicated `FULL_AI_API_KEY` can still override the shared Groq key without reading Target credentials.

Useful endpoints:

- `POST /api/full-analyst/jobs`
- `GET /api/full-analyst/jobs/{id}`
- `GET /api/full-analyst/active/XAUUSD`
- `GET /api/full-analyst/history/XAUUSD`
- `GET /api/full-analyst/jobs/{id}/lifecycle`
- `POST /api/full-analyst/jobs/{id}/cancel`
- `GET /api/full-analyst/configuration`

The backend remains the Phase 14 Full Analyst contract, while the web terminal presents it as **Future Analyst** beside **Target Analyst** in the right-side analyst rail. Future shows the current BUY/SELL/WAIT outlook and invalidation on the real-time chart; it only displays TP when an independent, compatible active Target plan exists. The Local Analyst current-signal card is not shown there; all-timeframe possible-signal status, signal history, and chart markers remain available. Active results reload from SQL, block a replacement generation, and remain until cancelled or terminally monitored. API keys never appear in configuration responses or persisted workspace JSON.

Run deterministic verification from the repository root:

```powershell
dotnet build XauAi.slnx --configuration Release
dotnet test tests/XauAi.UnitTests --configuration Release --no-build
dotnet test tests/XauAi.ArchitectureTests --configuration Release --no-build
dotnet tool run dotnet-ef migrations has-pending-model-changes --configuration Release --no-build --project backend/XauAi.Infrastructure/XauAi.Infrastructure.csproj --startup-project backend/XauAi.Infrastructure/XauAi.Infrastructure.csproj --context XauAiDbContext
dotnet format XauAi.slnx --verify-no-changes --severity warn --no-restore
```

Run SQL integration tests with `XAUAI_TEST_SQLSERVER_CONNECTION_STRING` configured. Provider-backed Full AI calls are not required for deterministic CI.
