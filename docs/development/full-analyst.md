# Full Analyst development guide

Full Analyst is disabled by default. Enable `FULL_ANALYST_ENABLED` only after SQL persistence, technical analysis, evidence ingestion, and all eight Full AI workspaces are configured.

Each workspace uses its own `FULL_<WORKSPACE>_AI_*` settings for provider, API key, model, base URL, timeout, token limit, temperature, rate limit, prompt version, and configuration version. Full workspaces do not fall back to Target Analyst credentials.

Useful endpoints:

- `POST /api/full-analyst/jobs`
- `GET /api/full-analyst/jobs/{id}`
- `GET /api/full-analyst/active/XAUUSD`
- `GET /api/full-analyst/history/XAUUSD`
- `GET /api/full-analyst/jobs/{id}/lifecycle`
- `POST /api/full-analyst/jobs/{id}/cancel`
- `GET /api/full-analyst/configuration`

The web terminal exposes the user-triggered workspace through **AI workspace → Full Analyst**. API keys never appear in configuration responses or persisted workspace JSON.

Run deterministic verification from the repository root:

```powershell
dotnet build XauAi.slnx --configuration Release
dotnet test tests/XauAi.UnitTests --configuration Release --no-build
dotnet test tests/XauAi.ArchitectureTests --configuration Release --no-build
dotnet tool run dotnet-ef migrations has-pending-model-changes --configuration Release --no-build --project backend/XauAi.Infrastructure/XauAi.Infrastructure.csproj --startup-project backend/XauAi.Infrastructure/XauAi.Infrastructure.csproj --context XauAiDbContext
dotnet format XauAi.slnx --verify-no-changes --severity warn --no-restore
```

Run SQL integration tests with `XAUAI_TEST_SQLSERVER_CONNECTION_STRING` configured. Provider-backed Full AI calls are not required for deterministic CI.
