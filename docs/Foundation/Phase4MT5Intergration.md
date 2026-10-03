# PHASE 4 — MT5 INTEGRATION

## ROLE

You are continuing development of my XAUUSD AI Trading Intelligence System.

Phases 1–3 are already completed:

* Phase 1 — Project Foundation
* Phase 2 — Configuration & Secrets
* Phase 3 — Database Foundation

Now implement **Phase 4 — MT5 Integration**.

Do NOT redesign the architecture created in Phases 1–3 unless a concrete technical problem requires it.

Do NOT skip directly to later phases.

---

# 1. PHASE OBJECTIVE

Build a reliable MT5 Desktop integration that allows the backend to retrieve market data from MetaTrader 5.

The first goal is:

```text
MT5 Desktop
    ↓
MT5 Integration Layer
    ↓
Application
    ↓
Infrastructure
    ↓
SQL Server
```

The integration must be designed so that the rest of the application does NOT become tightly coupled to the MT5-specific implementation.

MT5 is a market-data provider/integration.

It is NOT the AI engine.

It is NOT the strategy engine.

It is NOT the signal engine.

It is NOT the backtesting engine.

Those belong to later phases.

---

# 2. CURRENT MT5 ENVIRONMENT

I currently use MT5 on mobile, but I have now installed **MT5 Desktop on Windows** because the application needs desktop-terminal integration.

The terminal path is expected to be:

```text
C:\Program Files\MetaTrader 5\terminal64.exe
```

However, do NOT assume this path blindly.

The application must use the configured:

```env
MT5_TERMINAL_PATH=
```

from the environment/configuration system.

Current configuration concept:

```env
MT5_LOGIN=USER_PROVIDED_LATER
MT5_PASSWORD=USER_PROVIDED_LATER
MT5_SERVER=USER_PROVIDED_LATER
MT5_TERMINAL_PATH=C:\Program Files\MetaTrader 5\terminal64.exe
```

Never hardcode these values.

Never commit credentials to Git.

Never display the MT5 password in logs.

---

# 3. IMPORTANT ARCHITECTURE RULE

Do NOT allow the Application layer to directly depend on an MT5-specific SDK/library.

Use an abstraction.

For example:

```text
Application
    ↓
IMarketDataProvider
    ↓
Infrastructure
    ↓
Mt5MarketDataProvider
    ↓
MT5 Desktop
```

The exact interface/class names may be adjusted to fit the existing Phase 1–3 architecture, but preserve the dependency direction.

The system must be able to replace MT5 later without rewriting the Application layer.

For example:

```text
IMarketDataProvider
       │
       ├── Mt5MarketDataProvider
       │
       └── FutureOtherProvider
```

Do not create unnecessary abstractions that have no practical purpose.

---

# 4. MT5 RESPONSIBILITIES

Implement only the following responsibilities in this phase:

### Connection

* Start/connect to MT5 Desktop when appropriate.
* Initialize the MT5 integration.
* Authenticate using configured credentials where required.
* Detect connection failures.
* Detect initialization failures.
* Detect terminal availability problems.
* Detect invalid credentials.
* Detect invalid server/account configuration.

### Account information

Provide a safe way to verify that the configured MT5 account is reachable.

Retrieve only the information necessary to verify connectivity, such as:

* login/account identifier
* server
* connection status
* terminal status

Do NOT expose sensitive account information unnecessarily.

Do NOT implement trading/order execution.

---

# 5. MARKET DATA

Implement read-only market-data access.

The initial instrument is:

```text
XAUUSD
```

But do not hardcode the symbol throughout the system.

Use configuration or a provider abstraction.

The system should be able to retrieve:

### Current quote

* bid
* ask
* timestamp

### Candles/OHLC

Support the timeframes required by the Phase 3 database foundation:

```text
M1
M5
M15
M30
H1
H4
D1
```

Each candle should contain at least:

```text
Symbol
Timeframe
OpenTime
Open
High
Low
Close
Volume
```

Use the correct volume meaning provided by MT5.

Do not invent volume values.

Use `decimal` for financial prices where appropriate in the application/domain/database model.

---

# 6. HISTORICAL DATA

Implement the ability to request historical XAUUSD candles.

The interface should support requests similar to:

```text
GetCandles(
    symbol,
    timeframe,
    from,
    to
)
```

The exact implementation is up to the existing architecture.

Requirements:

* UTC timestamps
* deterministic results
* correct timeframe mapping
* correct symbol handling
* reasonable validation
* cancellation support
* timeout handling
* clear errors

Do not download unlimited history automatically.

The caller must specify the requested range.

---

# 7. DATA NORMALIZATION

MT5-specific data must be converted into the application's own domain/application models.

Do not let MT5-specific types leak throughout the system.

Conceptually:

```text
MT5 native data
      ↓
MT5 adapter
      ↓
Application/Domain market model
      ↓
Persistence
```

This is important because later technical indicators, strategies, backtesting, and AI analysis should work with our own normalized data rather than MT5-specific objects.

---

# 8. TIME AND TIMEZONE

Be extremely careful with timestamps.

The database uses UTC.

Normalize incoming market timestamps consistently.

Document:

* what timezone MT5 provides
* how it is converted
* what timezone is stored
* what timezone is exposed through the API

Do not silently mix:

```text
local time
broker/server time
UTC
```

The system must have one clear internal time standard.

---

# 9. MT5 SYMBOL HANDLING

Do not assume every broker names gold exactly:

```text
XAUUSD
```

Some brokers may use variations.

Create a configurable symbol mapping/setting where appropriate.

For example:

```text
Application symbol: XAUUSD
MT5 symbol: XAUUSD
```

Later this could support another broker-specific symbol without changing the rest of the system.

Do NOT add complicated multi-broker functionality yet.

---

# 10. MT5 CONFIGURATION

Use the typed configuration system created in Phase 2.

For example:

```text
Mt5Options
```

It should contain only configuration actually needed at this stage.

Potential settings:

```text
Enabled
Login
Password
Server
TerminalPath
Symbol
ConnectionTimeout
RequestTimeout
```

Do not scatter:

```csharp
Environment.GetEnvironmentVariable(...)
```

throughout the application.

Use the existing configuration architecture.

---

# 11. ENABLE/DISABLE BEHAVIOR

Respect:

```env
MT5_ENABLED=false
```

When disabled:

* application must still start
* health checks must clearly show MT5 as disabled
* MT5 code must not attempt connection
* normal application startup must not fail because MT5 is disabled

When enabled:

```env
MT5_ENABLED=true
```

the integration should attempt initialization according to the application's startup/lifecycle design.

Do not make the entire API unusable just because MT5 is temporarily unavailable unless that behavior is explicitly required.

---

# 12. HEALTH CHECK

Add an MT5 health/status check.

It should distinguish between states such as:

```text
Disabled
Available
Connected
Disconnected
ConfigurationError
TerminalNotFound
AuthenticationFailed
ConnectionFailed
```

Do not expose passwords or secrets.

The general health response should remain safe for clients.

For example:

```json
{
  "name": "mt5",
  "status": "Healthy"
}
```

or an appropriate equivalent using the existing health architecture.

---

# 13. ERROR HANDLING

Create clear application-level errors.

Examples:

```text
MT5_DISABLED
MT5_CONFIGURATION_INVALID
MT5_TERMINAL_NOT_FOUND
MT5_INITIALIZATION_FAILED
MT5_CONNECTION_FAILED
MT5_AUTHENTICATION_FAILED
MT5_SYMBOL_NOT_FOUND
MT5_DATA_REQUEST_FAILED
MT5_TIMEOUT
MT5_UNAVAILABLE
```

Do not expose raw internal exceptions directly to frontend users.

Log the technical details internally.

Return safe messages externally.

---

# 14. LOGGING

Use structured logging.

Log useful operational information such as:

```text
MT5 initialization started
MT5 initialization succeeded
MT5 connection lost
MT5 connection restored
MT5 historical data request started
MT5 historical data request completed
MT5 request failed
```

Do NOT log:

```text
MT5 password
API keys
connection secrets
```

Avoid logging huge candle datasets.

Log counts and ranges instead.

For example:

```text
Retrieved 5000 candles for XAUUSD H1 from 2026-01-01 to 2026-02-01
```

rather than logging all 5000 records.

---

# 15. DATABASE STORAGE

Phase 3 already created the market-candle database foundation.

Now connect MT5 retrieval to that storage.

Implement the basic ingestion flow:

```text
MT5
 ↓
Retrieve candles
 ↓
Normalize
 ↓
Validate
 ↓
Deduplicate
 ↓
Persist SQL Server
```

Respect the unique constraints created in Phase 3.

The ingestion process must be idempotent.

If the same candle is retrieved twice:

```text
first request → insert
second request → do not create duplicate
```

Do not create duplicate candles.

Do not overwrite historical data incorrectly.

---

# 16. INCREMENTAL DATA SYNC

Implement a basic incremental synchronization capability.

Conceptually:

```text
Last stored candle
       ↓
Determine missing period
       ↓
Request missing MT5 candles
       ↓
Normalize
       ↓
Deduplicate
       ↓
Store
```

Do not yet build the complete offline catch-up system from Phase 17.

This phase only needs the foundation required for reliable MT5 data ingestion.

Phase 17 will handle the broader offline/backfill architecture.

---

# 17. API ENDPOINTS

Create only the minimal API endpoints needed to test the integration.

Possible endpoints:

```text
GET /api/mt5/status
GET /api/market/xauusd/quote
GET /api/market/xauusd/candles
```

The exact routing should follow the project's existing API conventions.

Do not build the complete dashboard.

Do not build frontend trading screens.

Do not build signal screens.

Do not expose MT5 credentials through the API.

---

# 18. APPLICATION INTERFACES

Create clean application contracts.

For example:

```text
IMarketDataProvider
IMt5Connection
IMarketDataRepository
```

Only create interfaces that have a real architectural purpose.

Avoid:

```text
IWhateverService
WhateverService
WhateverManager
WhateverHelper
```

for every small class.

Keep responsibilities focused and findable.

---

# 19. INFRASTRUCTURE STRUCTURE

Keep MT5 implementation in Infrastructure.

A reasonable structure is:

```text
XauAi.Infrastructure/
└── MarketData/
    └── Mt5/
        ├── Mt5MarketDataProvider.cs
        ├── Mt5ConnectionManager.cs
        ├── Mt5SymbolMapper.cs
        ├── Mt5Options.cs
        └── ...
```

Adjust the exact structure to match the existing project conventions.

Do not put MT5-specific implementation in Domain.

Do not put MT5-specific implementation directly in API controllers.

---

# 20. TESTING

Create tests for:

### Configuration

* MT5 disabled
* missing login
* missing password
* missing server
* invalid terminal path
* invalid configuration

### Connection

* successful initialization
* terminal unavailable
* connection failure
* authentication failure
* graceful shutdown

### Market data

* quote retrieval
* candle retrieval
* timeframe mapping
* symbol mapping
* empty result
* invalid range
* timeout
* cancellation

### Data normalization

* correct OHLC mapping
* correct timestamps
* UTC conversion
* decimal precision
* volume mapping

### Persistence

* candle inserted
* duplicate candle prevented
* multiple timeframes supported
* multiple time ranges supported

### Architecture

Verify that:

```text
Domain
    ↓
does NOT depend on MT5

Application
    ↓
does NOT directly depend on MT5-specific implementation

Infrastructure
    ↓
contains MT5 implementation
```

---

# 21. INTEGRATION TEST

Create a real integration test that can be run when MT5 credentials are available.

Do not require real MT5 credentials for ordinary unit tests.

The integration test should verify:

```text
Application
   ↓
MT5 integration
   ↓
retrieve XAUUSD data
   ↓
normalize
   ↓
persist
   ↓
read from database
```

Use an explicit integration-test configuration/flag.

Do not accidentally connect to a real MT5 account during normal unit-test execution.

---

# 22. FRONTEND

Only create a very small MT5 connection/data verification UI if necessary.

For example:

```text
MT5 Status
Connected

Symbol
XAUUSD

Bid
...

Ask
...

Last Update
...
```

And a basic candle-data test view if useful.

Do NOT build:

* trading dashboard
* AI dashboard
* signal dashboard
* strategy dashboard
* complete charts
* news dashboard
* analyst dashboard

Those belong to later phases.

---

# 23. NO ORDER EXECUTION

This phase is strictly read-only.

DO NOT implement:

```text
Buy
Sell
PlaceOrder
ModifyOrder
CloseOrder
Trade execution
Position management
```

The system is an analytics/intelligence platform.

MT5 integration in this phase is for market-data access and connection verification.

---

# 24. DO NOT IMPLEMENT LATER PHASES

Do NOT implement:

* technical indicators
* candlestick strategy
* trend strategy
* breakout strategy
* pullback strategy
* reversal strategy
* momentum strategy
* support/resistance strategy
* multi-timeframe strategy
* news collection
* FRED integration
* analyst collection
* AI news analysis
* AI market analysis
* master AI
* signal generation
* backtesting
* win-rate calculation
* performance statistics
* full dashboard
* offline catch-up system
* automated trading

Those belong to later phases.

---

# 25. PERFORMANCE REQUIREMENTS

The MT5 integration must be designed for efficient data retrieval.

Do not:

```text
request one candle
save one candle
request next candle
save next candle
...
```

when a bulk historical request is available.

Prefer:

```text
request range
 ↓
receive batch
 ↓
normalize batch
 ↓
bulk/efficient persistence
```

Use asynchronous operations where appropriate.

Support cancellation tokens.

Avoid loading unnecessarily large datasets into memory.

Do not optimize prematurely with complicated infrastructure.

---

# 26. SECURITY REQUIREMENTS

Never expose:

```text
MT5_PASSWORD
OPENAI_API_KEY
FRED_API_KEY
NEWSDATA_API_KEY
```

to the frontend.

Never put secrets into:

```text
Git
GitHub
Dockerfile
source code
logs
API responses
screenshots
```

The frontend communicates only with our backend.

The backend communicates with MT5.

---

# 27. DOCUMENTATION

Update:

```text
README.md
```

with:

* MT5 Desktop requirement
* how to install MT5
* how to find `terminal64.exe`
* how to configure MT5
* how to enable/disable MT5
* how to run the MT5 integration
* how to run MT5 integration tests
* troubleshooting

Also update the architecture documentation with:

```text
Application
     ↓
IMarketDataProvider
     ↓
Mt5MarketDataProvider
     ↓
MT5 Desktop
```

Document important MT5 assumptions.

---

# 28. DEFINITION OF DONE

Phase 4 is complete only when:

* [ ] MT5 Desktop integration exists
* [ ] MT5 configuration uses Phase 2 typed configuration
* [ ] MT5 credentials are never hardcoded
* [ ] MT5 terminal path is configurable
* [ ] MT5 can be enabled/disabled
* [ ] MT5 connection status can be checked
* [ ] XAUUSD quote can be retrieved
* [ ] XAUUSD historical candles can be retrieved
* [ ] M1/M5/M15/M30/H1/H4/D1 are supported
* [ ] MT5 data is normalized
* [ ] timestamps are handled consistently in UTC
* [ ] broker-specific symbol mapping is supported
* [ ] candles can be persisted
* [ ] duplicate candles are prevented
* [ ] basic incremental synchronization works
* [ ] errors are handled safely
* [ ] secrets never appear in logs/API responses
* [ ] unit tests exist
* [ ] integration tests exist
* [ ] architecture tests remain valid
* [ ] project builds successfully
* [ ] Docker environment remains functional
* [ ] documentation is updated
* [ ] no later-phase features were accidentally implemented

---

# 29. FINAL IMPLEMENTATION RULE

Do not create a giant MT5 implementation file.

Keep responsibilities separated:

```text
Configuration
      ↓
Connection
      ↓
Symbol Mapping
      ↓
Market Data Retrieval
      ↓
Normalization
      ↓
Persistence
      ↓
API
```

Each file/class should have a clear responsibility.

The result should be maintainable, testable, understandable, and ready for Phase 5.

After completing Phase 4, provide a concise implementation report containing:

1. Files created
2. Files modified
3. Architecture changes
4. MT5 connection result
5. XAUUSD data retrieval result
6. Database ingestion result
7. Tests passed
8. Any issue that genuinely requires my action

Do NOT ask me to approve the next phase.

Stop only if an action genuinely requires a private credential, external account action, or information that only I can provide.
