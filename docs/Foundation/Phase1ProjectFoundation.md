# PHASE 1 — PROJECT FOUNDATION

You are building my **XAUUSD AI Trading Intelligence System**.

This is Phase 1 of a 20-phase project. Do not skip ahead.

## 1. Your Role

Act as the project's senior software architect and developer.

Build the project with:

* Clean architecture
* Maintainable code
* Clear file boundaries
* Feature-oriented organization
* Strong separation of concerns
* Testability
* Performance in mind
* Security in mind
* Future scalability in mind

Do NOT create a quick prototype that will need to be completely reorganized later.

The architecture created in Phase 1 must be suitable for the complete system described in the project roadmap.

Do not wait for my approval between normal development steps. Continue automatically according to the phase instructions.

Only stop and ask me when an action genuinely requires something only I can provide, such as a private API key, password, broker credential, account verification, or another external account action.

Never invent credentials, API keys, account information, or external secrets.

---

# 2. Project Purpose

The final application will analyze **XAUUSD (Gold)** using:

* MetaTrader 5 market data
* Technical indicators
* Economic data
* Federal Reserve information
* US economic releases
* Financial news
* Analyst opinions
* Historical market data
* AI analysis
* Historical signal performance
* Backtesting
* Statistical validation

The final system should NOT simply produce:

> BUY — 94% confidence

Instead, it should eventually combine:

* Current market conditions
* Technical conditions
* Macro conditions
* News
* Analyst views
* Historical comparable conditions
* AI reasoning
* Backtested statistical evidence

The system must distinguish between:

* AI confidence
* Historical signal accuracy
* Statistical sample size
* Expected value
* Actual historical performance

The AI must NOT be responsible for deterministic calculations such as RSI, EMA, backtesting results, or win-rate calculations. Those must be implemented using deterministic application code.

AI will primarily handle:

* Information understanding
* News interpretation
* Analyst-claim extraction
* Contextual reasoning
* Scenario analysis
* Explanation
* Combining structured evidence

---

# 3. Complete Project Roadmap

The complete project has 20 major phases:

1. Project Foundation
2. Configuration & Secrets
3. Database Foundation
4. MT5 Integration
5. Market Data Pipeline
6. Technical Analysis Engine
7. News Collection
8. Economic Data
9. Analyst Data
10. Data Normalization & Deduplication
11. AI News Analysis
12. AI Market Analysis
13. Master AI Reasoning
14. Signal Engine
15. Backtesting
16. Accuracy & Statistics
17. Offline Catch-up / Backfill
18. Dashboard
19. Security / Performance / Reliability
20. Full Testing & Final Audit

We are currently implementing:

# PHASE 1 — PROJECT FOUNDATION

Do NOT implement the later phases yet.

---

# 4. Technology Stack

Use the following stack unless there is a strong technical reason to change something.

## Backend

* ASP.NET Core
* C#
* Clean Architecture
* Feature-oriented organization
* REST API
* OpenAPI / Swagger
* Entity Framework Core later
* SQL Server later
* Redis later
* MediatR where appropriate
* Structured logging
* Dependency Injection
* Health checks

## Frontend

* Next.js
* TypeScript
* Modern React
* Feature-oriented organization

## Database

SQL Server.

Do not implement the complete database schema in Phase 1.

Database foundation is Phase 3.

## Cache

Redis.

Only establish the foundation in Phase 1.

Redis implementation details come later.

## AI

OpenAI API will eventually be used.

Do NOT make real AI API calls in Phase 1.

## Trading

MetaTrader 5 will eventually provide XAUUSD market data.

Do NOT implement MT5 integration in Phase 1.

## Containers

Use Docker and Docker Compose.

## Testing

Use:

* xUnit
* Integration tests
* Architecture tests
* Playwright later for frontend/end-to-end testing

## Source Control

Git / GitHub.

---

# 5. Repository Structure

Create the repository with this structure:

```text
XAUUSD-AI/
│
├── backend/
│   ├── XauAi.Api/
│   ├── XauAi.Application/
│   ├── XauAi.Domain/
│   └── XauAi.Infrastructure/
│
├── frontend/
│   └── xau-ai-web/
│
├── tests/
│   ├── XauAi.UnitTests/
│   ├── XauAi.IntegrationTests/
│   └── XauAi.ArchitectureTests/
│
├── docs/
│   ├── architecture/
│   ├── decisions/
│   └── development/
│
├── scripts/
│
├── .env.example
├── .gitignore
├── .dockerignore
├── docker-compose.yml
├── README.md
└── LICENSE
```

Keep the structure clean.

Do not put unrelated functionality into giant files.

---

# 6. Backend Architecture

Use these dependency rules:

```text
XauAi.Api
      ↓
XauAi.Application
      ↓
XauAi.Domain

XauAi.Infrastructure
      ↓
XauAi.Application
      ↓
XauAi.Domain
```

The important rule is:

```text
Domain
  ↓
must not depend on Infrastructure
must not depend on API
must not depend on external services
```

The Domain layer must remain independent.

Application contains application/use-case logic.

Infrastructure contains implementations for external systems.

API exposes HTTP endpoints and handles transport concerns.

---

# 7. Application Feature Structure

Prepare the architecture for future feature-oriented development.

Use:

```text
XauAi.Application/
│
├── Market/
├── News/
├── EconomicData/
├── Analysts/
├── AI/
├── Signals/
├── Backtesting/
└── Statistics/
```

Do not fully implement these features yet.

The purpose in Phase 1 is to establish an architecture that can support them cleanly later.

Avoid creating one giant:

```text
Services/
Repositories/
Helpers/
Utils/
Managers/
```

folder that eventually contains everything.

Use focused feature boundaries.

---

# 8. Frontend Structure

Create:

```text
frontend/xau-ai-web/
│
├── app/
├── components/
├── features/
├── lib/
├── hooks/
├── types/
├── config/
└── public/
```

Keep the frontend ready for future modules such as:

```text
Dashboard
Market
News
Economic Data
Analysts
Signals
Backtesting
Statistics
```

Do not build the complete dashboard in Phase 1.

---

# 9. Environment Configuration

Create:

```text
.env.example
```

Use placeholders for future private values.

Example:

```env
ASPNETCORE_ENVIRONMENT=Development

NEXT_PUBLIC_API_URL=http://localhost:5080

DATABASE_CONNECTION_STRING=USER_PROVIDED_OR_GENERATED_LATER

REDIS_CONNECTION_STRING=localhost:6379

OPENAI_API_KEY=USER_PROVIDED_LATER

NEWSDATA_API_KEY=USER_PROVIDED_LATER

MT5_LOGIN=USER_PROVIDED_LATER
MT5_PASSWORD=USER_PROVIDED_LATER
MT5_SERVER=USER_PROVIDED_LATER
MT5_TERMINAL_PATH=USER_PROVIDED_LATER
```

Important:

Never hardcode real secrets.

Never put API keys directly inside:

* C#
* TypeScript
* React components
* configuration committed to Git
* Dockerfiles
* README files

Use environment configuration and proper secret handling.

The values marked `USER_PROVIDED` will be supplied later when their corresponding phases require them.

---

# 10. Git Protection

Configure `.gitignore` properly.

It must protect things such as:

```text
.env
.env.*
!.env.example

node_modules/
.next/

bin/
obj/

logs/

.vs/
.vscode/

.idea/

Docker/local data

OS-generated files
```

Do not accidentally ignore `.env.example`.

The example configuration must remain committed.

---

# 11. Docker Foundation

Create a Docker Compose foundation for:

```text
XAUUSD-AI
│
├── Web
├── API
├── SQL Server
└── Redis
```

At this phase:

* API should start
* Web should start
* SQL Server container should start
* Redis container should start
* Basic connectivity/health checks should exist where appropriate

Do not implement the full database schema.

Do not implement MT5 inside Docker yet.

Do not force the MetaTrader terminal into a Linux container.

MT5 integration will be handled in Phase 4 after the architecture is ready.

---

# 12. API Foundation

Create a basic ASP.NET Core API.

It should have:

* Dependency injection
* Configuration
* Logging
* Health checks
* Swagger/OpenAPI
* Basic error handling
* Trace/correlation ID support
* Clean startup/shutdown behavior

Create a simple health endpoint such as:

```text
GET /health
```

It should eventually allow the application to determine whether the API is functioning.

Prepare the structure so later health checks can include:

* SQL Server
* Redis
* MT5
* News provider
* Economic providers
* OpenAI

Do not implement those external-provider checks yet.

---

# 13. API Error Response Foundation

Create a consistent API error format.

For example:

```json
{
  "success": false,
  "error": {
    "code": "SOME_ERROR_CODE",
    "message": "Human-readable message"
  },
  "traceId": "..."
}
```

Do not expose sensitive internal information.

Do not expose:

* API keys
* passwords
* connection strings
* stack traces to normal clients
* internal secrets

Use appropriate development vs production behavior.

---

# 14. Logging Foundation

Set up structured logging.

Logs should help diagnose:

* startup failures
* configuration failures
* API failures
* dependency failures
* background processing failures later

Never log secrets.

Never log:

```text
OPENAI_API_KEY
MT5_PASSWORD
database passwords
authentication tokens
```

or equivalent sensitive values.

Prepare logging so later we can trace:

```text
Request
   ↓
Application operation
   ↓
External provider
   ↓
Database/cache
```

---

# 15. Documentation Foundation

Create:

```text
docs/
├── architecture/
│   └── system-overview.md
│
├── decisions/
│   └── README.md
│
└── development/
    └── development-rules.md
```

## system-overview.md

Document the intended high-level architecture:

```text
                 XAUUSD AI SYSTEM
                        │
        ┌───────────────┼───────────────┐
        ↓               ↓               ↓
       MT5             News          Economic Data
        │               │               │
        ↓               ↓               ↓
   Market Data      News Sources     Fed/BLS/etc.
        │               │               │
        └───────────────┼───────────────┘
                        ↓
                    Database
                        ↓
                 Analysis Engine
                        ↓
                    OpenAI API
                        ↓
                 AI Market Analysis
                        ↓
              Signal / Backtesting
                        ↓
                   Statistics
                        ↓
                    Dashboard
```

Explain that deterministic code handles:

* indicators
* statistics
* backtesting
* signal outcome measurement

while AI handles:

* interpretation
* language understanding
* contextual reasoning
* news analysis
* scenario synthesis

## development-rules.md

Document rules such as:

* Keep files focused
* Avoid giant classes
* Avoid giant services
* Keep feature boundaries clear
* Do not hardcode secrets
* Prefer testable code
* Do not mix infrastructure logic into domain logic
* Do not mix UI concerns with API logic
* Do not create unnecessary abstractions
* Do not create technical debt intentionally
* Do not change business logic without a reason
* Document important architectural decisions

---

# 16. Architecture Tests

Create:

```text
tests/
├── XauAi.UnitTests/
├── XauAi.IntegrationTests/
└── XauAi.ArchitectureTests/
```

Phase 1 architecture tests should verify important dependency rules.

For example:

```text
Domain
  ❌ cannot depend on API
  ❌ cannot depend on Infrastructure

Application
  ❌ cannot depend on API
```

The exact implementation can use an appropriate architecture-testing approach.

---

# 17. Basic Integration Test Foundation

Create the integration-test project and establish the infrastructure needed for future API tests.

Do not implement all future tests.

At minimum, establish a basic test proving the API can start/respond in the intended test environment.

Keep test infrastructure separate from production code.

---

# 18. Frontend Foundation

Create the Next.js application.

It should:

* Start successfully
* Have TypeScript configured
* Have a clean application structure
* Have configuration separated from UI
* Have reusable component boundaries
* Have a basic API client foundation

Do not build the full trading dashboard.

A simple placeholder page is enough.

The purpose is to prove:

```text
Browser
   ↓
Next.js
   ↓
ASP.NET API
```

can communicate.

---

# 19. Basic API Client

Create a reusable frontend API client rather than scattering raw `fetch()` calls throughout components.

For example, establish a structure similar to:

```text
lib/
└── api/
    ├── client.ts
    └── errors.ts
```

Do not implement business-specific API services yet.

The client should be designed so future features can use it consistently.

---

# 20. Health / Startup Verification

The application should be testable with a basic startup flow:

```text
Docker Compose
      ↓
SQL Server starts
      ↓
Redis starts
      ↓
API starts
      ↓
Web starts
      ↓
API health check
      ↓
Frontend can reach API
```

Verify that the foundation actually works.

Do not merely create files without testing them.

---

# 21. Phase 1 MUST NOT IMPLEMENT

Do NOT implement:

### MT5

No:

* MT5 login
* MT5 password
* XAUUSD live price
* MT5 historical data
* MT5 trading execution

### News

No:

* NewsData API calls
* news collection
* news processing
* news AI analysis

### Economic Data

No:

* FRED implementation
* Federal Reserve API integration
* BLS implementation
* BEA implementation
* Treasury integration

### Analysts

No analyst data collection yet.

### Technical Indicators

Do not implement:

* RSI
* EMA
* MACD
* ATR
* market structure
* other technical indicators

### AI

Do not make OpenAI API calls.

Do not build the AI agents yet.

### Signals

Do not generate BUY/SELL signals.

### Backtesting

Do not implement backtesting.

### Statistics

Do not calculate win rate yet.

### Catch-up

Do not implement offline catch-up yet.

### Dashboard

Do not build the complete dashboard.

These belong to later phases.

---

# 22. User-Provided Values for Phase 1

There should be **NO required private credentials from me during Phase 1**.

Do not ask me for:

```text
OPENAI_API_KEY
NEWSDATA_API_KEY
MT5_LOGIN
MT5_PASSWORD
MT5_SERVER
```

yet.

Those will be requested only when their respective phase requires them.

If a value is necessary for Phase 1, use a safe placeholder or development configuration instead.

Never invent a real secret.

---

# 23. Phase 1 Definition of Done

Phase 1 is complete only when all of the following are true:

### Repository

* [ ] Repository created
* [ ] Correct folder structure
* [ ] Backend projects created
* [ ] Frontend created
* [ ] Test projects created
* [ ] Documentation structure created

### Architecture

* [ ] Clean Architecture dependency rules established
* [ ] Domain independent
* [ ] Application independent from API
* [ ] Infrastructure separated
* [ ] Feature-oriented structure prepared

### Configuration

* [ ] `.env.example` created
* [ ] No real secrets committed
* [ ] `.gitignore` configured
* [ ] Configuration system established

### Docker

* [ ] Docker Compose created
* [ ] API container starts
* [ ] Web container starts
* [ ] SQL Server starts
* [ ] Redis starts

### API

* [ ] API starts
* [ ] Swagger works
* [ ] Health endpoint works
* [ ] Logging works
* [ ] Error handling foundation works
* [ ] Trace ID exists

### Frontend

* [ ] Next.js starts
* [ ] TypeScript works
* [ ] API client foundation exists
* [ ] Frontend can communicate with API

### Testing

* [ ] Unit test project works
* [ ] Integration test project works
* [ ] Architecture test project works
* [ ] Dependency rules are tested

### Documentation

* [ ] Architecture documented
* [ ] Development rules documented
* [ ] Decision documentation initialized
* [ ] README explains how to start the project

### Security

* [ ] No secrets hardcoded
* [ ] No passwords logged
* [ ] No API keys logged
* [ ] Development configuration separated from secrets

---

# 24. Final Phase 1 Verification

After implementation, do not simply tell me that Phase 1 is finished.

Actually verify it.

Run the appropriate:

```text
build
tests
Docker Compose startup
health checks
frontend startup
API startup
```

and fix problems you discover.

Then provide a concise report containing:

1. What was created
2. Important files/folders
3. Architecture decisions
4. Tests executed
5. Verification results
6. Any remaining issue
7. Whether Phase 1 is actually complete
8. What Phase 2 will implement

Do not start Phase 2 until Phase 1 is actually stable.

However, because I want this project developed continuously, once Phase 1 is verified, continue to Phase 2 automatically unless you genuinely need a private credential or user-only external action.

# END OF PHASE 1
