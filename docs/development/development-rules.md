# Development rules

## Boundaries

- Keep Domain independent of API, Infrastructure, frameworks, and providers.
- Put use-case contracts and orchestration in Application.
- Put SQL Server, Redis, AllTick, Twelve Data, news, economic, and AI implementations in Infrastructure.
- Keep HTTP, serialization, and middleware concerns in API.
- Organize business work by feature; do not create catch-all Services, Helpers, Managers, or Utils folders.
- Keep UI concerns out of backend projects and provider concerns out of React components.

## Code quality

- Keep files and classes focused; split code when responsibilities diverge.
- Prefer explicit, testable dependencies through dependency injection.
- Add abstractions only when they express a real boundary or enable a known use case.
- Treat build warnings as errors.
- Test boundary rules and externally observable behavior.
- Record significant architectural decisions in `docs/decisions`.

## Data and intelligence

- Implement indicators, backtests, expected value, outcome measurement, and statistics deterministically.
- Use AI for interpretation, extraction, scenarios, synthesis, and explanation.
- Never label model confidence as measured historical accuracy.
- Preserve sample size and test context alongside every future performance statistic.

## Security and observability

- Never hardcode or commit real secrets, credentials, tokens, or connection strings.
- Never write secrets or request authorization values to logs.
- Log structured properties, not interpolated secret-bearing objects.
- Return stable client-safe errors; do not expose stack traces or provider internals.
- Carry the API correlation ID through future application, provider, database, and cache operations.

## Change discipline

- Respect the active roadmap phase and do not implement later-phase business behavior early.
- Do not change business rules without a documented reason and tests.
- Verify builds, tests, container configuration, health endpoints, and frontend checks before declaring a phase complete.
