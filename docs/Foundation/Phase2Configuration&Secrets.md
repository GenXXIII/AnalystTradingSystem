# PHASE 2 — CONFIGURATION & SECRETS

Continue building the **XAUUSD AI Trading Intelligence System** from Phase 1.

Phase 1 established the project foundation, architecture, repository, frontend, backend, Docker foundation, testing foundation, logging, health checks, and documentation.

Now implement:

# PHASE 2 — CONFIGURATION & SECRETS

The purpose of this phase is to create a professional configuration and secret-management system that all future phases can use.

Do NOT implement the actual MT5 integration, news collection, economic-data collection, AI analysis, strategy engine, signal engine, or trading logic yet.

---

# 1. Main Objective

Create a configuration architecture that supports:

* Development
* Testing
* Production
* Local Docker
* External API credentials
* MT5 credentials
* Database configuration
* Redis configuration
* AI configuration
* News-provider configuration
* Economic-data-provider configuration
* Application settings
* Logging settings
* Future strategy settings

The system must make it easy for me to provide private credentials later without modifying source code.

---

# 2. Core Rule

Never hardcode secrets.

Bad:

```csharp
var apiKey = "real-api-key";
```

Bad:

```typescript
const apiKey = "real-api-key";
```

Bad:

```yaml
OPENAI_API_KEY: "real-key"
```

Good:

```text
Environment
    ↓
Configuration
    ↓
Typed settings
    ↓
Application service
```

The source code must never contain real credentials.

---

# 3. Configuration Categories

Design configuration around clear categories.

At minimum:

```text
Application
Database
Redis
AI
MT5
News
Economic Data
Analysts
Logging
CORS
API
```

The architecture must allow additional providers to be added later without redesigning the entire configuration system.

---

# 4. Environment Separation

Support:

```text
Development
Testing
Production
```

Use appropriate configuration files and environment variables.

For example:

```text
appsettings.json
appsettings.Development.json
appsettings.Testing.json
appsettings.Production.json
```

Do not put secrets into these committed files.

Non-secret defaults can exist there.

Secrets should come from environment variables or an appropriate secret-management mechanism.

---

# 5. `.env.example`

Create a complete `.env.example`.

It should document every external/private value that the project will eventually require.

Use placeholders.

Example:

```env
# ==========================================
# APPLICATION
# ==========================================

ASPNETCORE_ENVIRONMENT=Development

APP_NAME=XAUUSD-AI
APP_VERSION=1.0.0

# ==========================================
# API
# ==========================================

API_PORT=5080

# ==========================================
# FRONTEND
# ==========================================

NEXT_PUBLIC_API_URL=http://localhost:5080

# ==========================================
# DATABASE
# ==========================================

DATABASE_CONNECTION_STRING=USER_PROVIDED_LATER

# ==========================================
# REDIS
# ==========================================

REDIS_CONNECTION_STRING=localhost:6379

# ==========================================
# OPENAI
# ==========================================

OPENAI_API_KEY=USER_PROVIDED_LATER
OPENAI_MODEL=MODEL_DEFINED_LATER

# ==========================================
# MT5
# ==========================================

MT5_LOGIN=USER_PROVIDED_LATER
MT5_PASSWORD=USER_PROVIDED_LATER
MT5_SERVER=USER_PROVIDED_LATER
MT5_TERMINAL_PATH=USER_PROVIDED_LATER

# ==========================================
# NEWS
# ==========================================

NEWS_API_KEY=USER_PROVIDED_LATER

# ==========================================
# ECONOMIC DATA
# ==========================================

ECONOMIC_DATA_API_KEY=USER_PROVIDED_LATER

# ==========================================
# ANALYST SOURCES
# ==========================================

ANALYST_PROVIDER_API_KEY=USER_PROVIDED_LATER
```

Do NOT assume every provider above will necessarily be paid.

The exact providers will be finalized in their respective implementation phases.

Do not invent API keys.

---

# 6. Important Provider Rule

The system must NOT become tightly coupled to one external provider.

For example:

```text
Application
      ↓
INewsProvider
      ↓
Current News Provider
```

not:

```text
Application
      ↓
NewsData-specific implementation everywhere
```

Use interfaces/abstractions where they provide real architectural value.

This allows a provider to be replaced later.

The same principle applies to:

* AI provider
* News provider
* Economic provider
* Analyst provider
* Market-data provider

---

# 7. AI Configuration

Prepare configuration for the future AI analysis pipeline.

The final project will eventually use AI for:

```text
News analysis
Macro analysis
Market-context analysis
Strategy analysis
Historical-context analysis
Master analysis
```

Configuration should therefore support:

```text
AI provider
API key
Model
Temperature/settings where applicable
Timeout
Retry policy
Token/output limits
Cost controls
```

Do not make actual AI requests in this phase.

---

# 8. AI Cost Control

This project must prioritize **strong analysis while minimizing unnecessary API usage**.

Design configuration for future cost control.

For example:

```text
AI:
    Enabled
    Provider
    Model
    MaxOutputTokens
    TimeoutSeconds
    MaxRetries
    DailyBudget
    RequestLimit
```

The exact configuration names may be improved if you have a better design.

The important requirement is:

> The architecture must make unnecessary AI calls difficult rather than easy.

Later phases should be able to:

* cache analysis
* avoid duplicate analysis
* batch information
* preprocess with deterministic code
* send only relevant information to AI
* use smaller models where appropriate
* reserve stronger models for complex reasoning
* record token usage
* record API cost where available

Do not implement the complete AI cost-control engine yet.

Prepare the configuration architecture for it.

---

# 9. MT5 Configuration

Prepare configuration for future MT5 integration:

```text
MT5_LOGIN
MT5_PASSWORD
MT5_SERVER
MT5_TERMINAL_PATH
```

Potential future settings:

```text
MT5_SYMBOL
MT5_TIMEZONE
MT5_CONNECTION_TIMEOUT
MT5_RECONNECT_DELAY
```

Do not connect to MT5 yet.

Do not request my MT5 credentials yet unless Phase 2 technically requires them for configuration validation.

Use:

```text
USER_PROVIDED_LATER
```

instead.

---

# 10. Database Configuration

Prepare the configuration system for SQL Server.

Use a connection string from configuration.

Never hardcode:

```text
Server
Database
User
Password
```

inside application source code.

The architecture should allow:

```text
Local development
Docker
Testing
Production
```

to use different database configurations.

Do not build the database schema yet.

That belongs to Phase 3.

---

# 11. Redis Configuration

Prepare:

```text
REDIS_CONNECTION_STRING
```

and future Redis settings such as:

```text
Redis:
    Enabled
    ConnectionString
    InstanceName
    DefaultExpiration
```

Do not implement the complete caching strategy yet.

That comes later.

---

# 12. News Configuration

Prepare configuration for the selected news provider.

The provider should be replaceable.

Configuration should support:

```text
API key
Base URL
Timeout
Retry count
Rate limit
Enabled
```

Do not make external news requests yet.

Do not start collecting news yet.

News collection is Phase 7.

---

# 13. Economic Data Configuration

Prepare configuration for the economic-data provider.

Support:

```text
API key
Base URL
Timeout
Retry
Rate limit
Enabled
```

Do not collect economic data yet.

Economic-data implementation is Phase 8.

---

# 14. Analyst Source Configuration

Prepare configuration for analyst sources.

The final system will eventually collect and analyze analyst commentary.

Do not assume that every analyst website provides a public API.

The architecture must distinguish:

```text
Official API
RSS/feed
Public source
Permitted web data
Manual source
```

Do not implement scraping that violates a provider's terms.

Do not collect analyst data yet.

Analyst integration is Phase 9.

---

# 15. Configuration Validation

Implement startup/configuration validation.

The application should clearly identify missing required configuration.

For example:

```text
Configuration validation failed:

OPENAI_API_KEY is required when AI is enabled.
```

But do NOT require an API key for a feature that is intentionally disabled.

For example:

```text
AI_ENABLED=false
```

should allow the application to start without an OpenAI key during early development.

The same principle applies to future providers.

---

# 16. Feature Flags

Create a clean mechanism for enabling/disabling future integrations.

For example:

```text
AI_ENABLED=false
MT5_ENABLED=false
NEWS_ENABLED=false
ECONOMIC_DATA_ENABLED=false
ANALYSTS_ENABLED=false
```

This allows the application to run while only some integrations are configured.

Do not create hundreds of unnecessary feature flags.

Use them where they provide genuine value.

---

# 17. Secret Safety

Implement safeguards against accidentally exposing secrets.

Check that:

* Secrets are never logged
* Secrets are never returned by API endpoints
* Secrets are not included in error responses
* Secrets are not exposed to frontend code
* Server-only secrets remain server-side
* `.env` is ignored by Git
* `.env.example` contains placeholders only

Very important:

`NEXT_PUBLIC_*` variables are potentially exposed to the browser.

Therefore:

```text
OPENAI_API_KEY
MT5_PASSWORD
NEWS_API_KEY
DATABASE_CONNECTION_STRING
```

must NEVER use:

```text
NEXT_PUBLIC_
```

---

# 18. Frontend Configuration

The frontend should only receive configuration that is safe for the browser.

For example:

```env
NEXT_PUBLIC_API_URL=http://localhost:5080
```

Do NOT expose:

```text
OPENAI_API_KEY
MT5_PASSWORD
DATABASE_PASSWORD
NEWS_API_KEY
```

to Next.js client-side code.

The frontend communicates with the backend.

```text
Browser
   ↓
Next.js
   ↓
Backend API
   ↓
Private services
```

---

# 19. Docker Secrets / Environment Handling

Configure Docker so secrets are supplied externally.

Do not put real secrets directly into:

```text
Dockerfile
docker-compose.yml
Git
```

Use environment injection appropriately.

Create a safe local-development pattern.

Document how the developer should supply environment variables.

---

# 20. Configuration Documentation

Create:

```text
docs/development/configuration.md
```

Document every configuration variable:

| Variable                     | Required            | Secret     | Used By  | Phase |
| ---------------------------- | ------------------- | ---------- | -------- | ----- |
| `DATABASE_CONNECTION_STRING` | Yes later           | Yes        | Backend  | 3     |
| `REDIS_CONNECTION_STRING`    | Yes later           | Depends    | Backend  | 3     |
| `OPENAI_API_KEY`             | Yes when AI enabled | Yes        | AI       | 11+   |
| `MT5_LOGIN`                  | Yes for MT5         | Yes        | MT5      | 4     |
| `MT5_PASSWORD`               | Yes for MT5         | Yes        | MT5      | 4     |
| `MT5_SERVER`                 | Yes for MT5         | No/limited | MT5      | 4     |
| `NEWS_API_KEY`               | Provider-dependent  | Yes        | News     | 7     |
| `ECONOMIC_DATA_API_KEY`      | Provider-dependent  | Yes        | Economic | 8     |

Clearly mark provider-dependent values.

Do not claim a key is required if the selected provider doesn't require one.

---

# 21. Configuration Access

Do not scatter:

```csharp
Environment.GetEnvironmentVariable(...)
```

throughout the application.

Create a proper configuration mechanism.

Prefer typed configuration objects such as:

```text
ApplicationOptions
DatabaseOptions
RedisOptions
AiOptions
Mt5Options
NewsOptions
EconomicDataOptions
AnalystOptions
```

Use dependency injection.

This makes the configuration:

* testable
* discoverable
* maintainable
* strongly typed

---

# 22. Configuration Security Tests

Create tests proving that:

* Missing required configuration is detected
* Disabled integrations do not require credentials
* Secrets are not returned through API responses
* Frontend cannot access server-only secrets
* Configuration binding works correctly
* Invalid configuration fails clearly

Do not use real production credentials in tests.

---

# 23. Development Configuration

Create safe local development defaults where possible.

For example:

```text
AI_ENABLED=false
MT5_ENABLED=false
NEWS_ENABLED=false
ECONOMIC_DATA_ENABLED=false
ANALYSTS_ENABLED=false
```

This allows the foundation to run without external accounts.

Do not use fake credentials that could accidentally be mistaken for real ones.

---

# 24. What I Will Provide Later

Do not ask me to provide everything now.

The project will eventually require private information such as:

```text
OpenAI API key
MT5 credentials
News provider key
Economic-data provider key
Other provider credentials
```

When each phase actually requires them, tell me:

1. What the credential is
2. Why it is needed
3. Where I obtain it
4. What account/plan is required
5. Exact environment-variable name
6. Exact location where I should enter it
7. How to test it
8. How to keep it secure

Until then, use:

```text
USER_PROVIDED_LATER
```

---

# 25. DO NOT BUY OR CONNECT SERVICES AUTOMATICALLY

Do not assume I have purchased a provider.

Do not tell me to subscribe to an expensive provider unless that provider is actually required.

The architecture must support replacing providers.

When a future phase reaches a provider decision, evaluate:

* cost
* API limits
* data quality
* historical coverage
* reliability
* latency
* licensing
* terms of use
* suitability for XAUUSD

The project should minimize recurring API costs wherever possible.

---

# 26. Phase 2 Must NOT Implement

Do NOT implement:

```text
❌ MT5 connection
❌ Live XAUUSD data
❌ Historical market-data collection
❌ Candlestick strategies
❌ Technical indicators
❌ News collection
❌ Economic data collection
❌ Analyst collection
❌ AI analysis
❌ AI signals
❌ Backtesting
❌ Win-rate calculations
❌ Trading execution
❌ Full dashboard
```

Only create the configuration foundation required for those future phases.

---

# 27. Phase 2 Testing

Run and verify:

```text
[ ] Backend starts with development configuration
[ ] Missing required configuration is detected
[ ] Disabled providers do not require credentials
[ ] Configuration binding works
[ ] Docker environment variables work
[ ] Frontend receives only safe configuration
[ ] Secrets are not exposed in logs
[ ] Secrets are not exposed through API
[ ] .env remains ignored by Git
[ ] .env.example contains no real secrets
[ ] Unit tests pass
[ ] Integration tests pass
[ ] Architecture tests pass
```

Actually run the tests and fix failures.

Do not simply claim they pass.

---

# 28. Phase 2 Definition of Done

Phase 2 is complete only when:

```text
✅ Configuration architecture exists
✅ Environment separation exists
✅ Typed configuration exists
✅ .env.example is complete
✅ Secrets are protected
✅ Feature flags work
✅ Configuration validation works
✅ Docker configuration works
✅ Frontend/server configuration is separated
✅ AI configuration foundation exists
✅ MT5 configuration foundation exists
✅ News configuration foundation exists
✅ Economic-data configuration foundation exists
✅ Analyst configuration foundation exists
✅ Configuration documentation exists
✅ Tests pass
✅ No real credentials are committed
```

---

# 29. Final Verification Report

After completing Phase 2, provide:

1. Files created/modified
2. Configuration architecture
3. Environment variables
4. Secret-handling design
5. Feature flags
6. Tests executed
7. Test results
8. Security verification
9. Any remaining issue
10. Confirmation that Phase 2 is complete
11. What Phase 3 will implement

Do not move architectural responsibility back to me.

Make reasonable technical decisions yourself.

Do not ask for approval between normal development steps.

Only stop when something genuinely requires a private credential or an external action that only I can perform.

# END OF PHASE 2
