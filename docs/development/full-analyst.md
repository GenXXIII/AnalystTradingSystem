# Full Analyst development guide

Full Analyst is disabled by default. Enable `FULL_ANALYST_ENABLED` only after SQL persistence, technical analysis, evidence ingestion, and all eight Full AI workspaces are configured.

Each workspace uses its own `FULL_<WORKSPACE>_AI_*` settings for provider, API key, model, base URL, timeout, token limit, temperature, rate limit, prompt version, and configuration version. `FULL_AI_API_KEY` is an optional Full-only shared key; a workspace-specific key takes precedence. Full workspaces do not implicitly read Target Analyst credentials.

For temporary development with OpenRouter's free router, set `FULL_AI_API_KEY`, enable each workspace, and use `OpenRouter`, `openrouter/free`, and `https://openrouter.ai/api/v1/` for its provider, model, and base URL. The ignored local `.env` may explicitly assign `FULL_AI_API_KEY=${TARGET_AI_API_KEY}` when both systems intentionally share one OpenRouter account.

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
