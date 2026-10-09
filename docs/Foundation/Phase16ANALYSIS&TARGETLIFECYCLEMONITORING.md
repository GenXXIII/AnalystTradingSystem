PHASE 16 — ANALYSIS & TARGET LIFECYCLE MONITORING

OBJECTIVE

Build the lifecycle monitoring system for AnalystTradingSystem.

The purpose of this phase is to continuously monitor active:

- Local Analyst signals
- Target Analyst results
- Full Analyst results
- Future Scenarios

and correctly determine when each active result should:

- remain active
- progress
- become invalid
- expire
- complete
- be cancelled

This phase is responsible for lifecycle management only.

It must NOT create new BUY/SELL decisions.
It must NOT create new Target decisions.
It must NOT replace Full Analyst.
It must NOT replace Future Scenario.
It must NOT execute trades.

The lifecycle monitor observes existing analysis objects and manages their active state.


CORE FLOW

Market Data
    ↓
Lifecycle Monitor
    ↓
Active Analysis Objects
    ↓
Evaluate Current Conditions
    ↓
┌──────────────┬──────────────┬──────────────┬──────────────┐
↓              ↓              ↓              ↓
CONTINUE     PROGRESS      INVALIDATE      EXPIRE
                                │
                                ↓
                           COMPLETE / END
                                │
                                ↓
                             HISTORY


1. INDEPENDENT LIFECYCLE MANAGEMENT

Each system must have its own lifecycle.

Local Analyst:
- independent lifecycle

Target Analyst:
- independent lifecycle

Full Analyst:
- independent lifecycle

Future Scenario:
- independent lifecycle

Do not create one giant lifecycle state shared by all systems.

Example:

LOCAL
🟢 BUY
→ ACTIVE

TARGET
🎯 4,205
→ ACTIVE

FULL
🟢 BUY
→ ACTIVE

FUTURE
🔮 Bullish Path
→ ACTIVE

One system may expire while the others remain active.

Example:

Local → INVALIDATED
Target → ACTIVE
Full → ACTIVE
Future → ACTIVE

This is valid.


2. LOCAL ANALYST LIFECYCLE

Local Analyst states:

- NOTHING
- BUY
- SELL
- STOP
- EXPIRED
- COMPLETED

Lifecycle:

NOTHING
   ↓
SIGNAL DETECTED
   ↓
BUY / SELL
   ↓
ACTIVE
   ↓
Monitor Conditions
   ├── Target reached
   ├── Invalidation
   ├── Time expired
   └── Signal replaced

When the local setup ends:

ACTIVE
↓
STOP / EXPIRED / COMPLETED
↓
Remove active chart representation
↓
Keep history


3. LOCAL SIGNAL MONITORING

Monitor:

- signal candle
- current price
- structure
- momentum
- volatility
- support/resistance
- invalidation conditions
- configured validity period
- local target condition

Do not recalculate the entire market analysis unnecessarily.

Use incremental monitoring where possible.

The lifecycle monitor should determine whether the existing setup is still valid, not create a new setup.


4. LOCAL STOP

🟡 STOP belongs only to Local Analyst.

STOP means:

“The current Local setup is no longer active.”

Possible reasons:

- invalidation
- opposite structure
- support/resistance failure
- configured timeout
- target completed
- setup conditions disappeared

Do not use Local STOP for:

- Full Analyst
- Target Analyst
- Future Scenario
- Telegram


5. TARGET ANALYST LIFECYCLE

Target Analyst states:

- ANALYZING
- ACTIVE
- HIT
- INVALIDATED
- EXPIRED
- CANCELLED
- FAILED

Flow:

[TARGET ANALYST]
      ↓
ANALYZING
      ↓
TARGET CREATED
      ↓
ACTIVE
      ↓
┌──────────────┬──────────────┬──────────────┐
↓              ↓              ↓
TARGET HIT   INVALIDATED    EXPIRED
      │
      └──────────────┐
                     ↓
                  HISTORY

User cancellation:

ACTIVE
↓
CANCELLED
↓
Remove active Target UI
↓
Keep history


6. TARGET MONITORING

Target Analyst produces exactly ONE target.

The lifecycle monitor must track:

- current price
- target price
- invalidation/SL level
- validity period
- market condition
- target status

Example:

TARGET
🎯 4,205
🛡 Invalidation: 4,165

If price reaches the target:

TARGET HIT

If invalidation condition occurs:

TARGET INVALIDATED

If validity expires:

TARGET EXPIRED

Do not create Target 2 automatically.


7. TARGET INDEPENDENCE

Target lifecycle must not depend on:

- Local BUY/SELL
- Full BUY/SELL/WAIT
- Future direction
- Telegram signal

Example:

LOCAL
🔴 SELL

TARGET
🎯 4,205

FULL
⚪ WAIT

The Target can remain active according to its own conditions.

Do not automatically cancel it because another system disagrees.


8. FULL ANALYST LIFECYCLE

Full Analyst states:

- REQUESTED
- ANALYZING
- BUY
- SELL
- WAIT
- ACTIVE
- INVALIDATED
- EXPIRED
- CANCELLED
- COMPLETED
- FAILED

Flow:

[FULL ANALYST]
      ↓
REQUESTED
      ↓
ANALYZING
      ↓
MASTER DECISION
      ↓
BUY / SELL / WAIT

BUY / SELL
      ↓
ACTIVE
      ↓
Monitor
      ├── Invalidation
      ├── Expiry
      ├── Completion
      └── Cancellation

WAIT
      ↓
No Future
      ↓
Completed / Expired


9. FULL ANALYST MONITORING

The lifecycle monitor must not turn:

WAIT → BUY

or:

WAIT → SELL

automatically.

If the market changes significantly, a new Full Analyst request may be required.

The lifecycle monitor observes the existing Full Analyst result.

It does not replace the Full Analyst decision engine.


10. FULL ANALYST INVALIDATION

A Full Analyst result may become invalid when:

- core supporting structure breaks
- invalidation condition is reached
- evidence becomes materially stale
- major market regime changes
- important news invalidates the scenario
- data integrity becomes insufficient
- configured validity expires

When invalidated:

FULL ANALYST
↓
INVALIDATED
↓
Remove active analysis representation
↓
Preserve history


11. FULL ANALYST WAIT

WAIT is a valid final decision.

Example:

FULL ANALYST
⚪ WAIT

No active Future.

Do not create:

- Future
- Target
- BUY
- SELL

from WAIT.

The system may keep the analysis record for history and evaluation.


12. FUTURE SCENARIO LIFECYCLE

Future states:

- PENDING
- ANALYZING
- ACTIVE
- COMPLETED
- INVALIDATED
- EXPIRED
- CANCELLED

Flow:

FULL BUY/SELL
      ↓
FUTURE
      ↓
ACTIVE
      ↓
Monitor Expected Path
      ↓
Stage Progress
      ├── COMPLETED
      ├── INVALIDATED
      ├── EXPIRED
      └── CANCELLED


13. FUTURE MONITORING

Monitor:

- current price
- current path stage
- expected price zones
- trigger conditions
- invalidation conditions
- structure
- liquidity
- momentum
- volatility
- important news
- validity period

Example:

Expected:

Pullback
↓
Support Reaction
↓
Continuation

Market:

Pullback detected
↓
Support reached
↓
Bullish reaction
↓
Continuation

The lifecycle should update:

Stage 1 → COMPLETED
Stage 2 → COMPLETED
Stage 3 → ACTIVE


14. FUTURE INVALIDATION

Future should be invalidated when its conditions fail.

Example:

Expected:

Pullback
↓
Support
↓
Continuation

Actual:

Pullback
↓
Support breaks
↓
Bearish structure

Result:

⚠️ FUTURE PATH INVALIDATED

Do not convert Future invalidation into:

🟡 STOP

STOP belongs to Local Analyst only.


15. FUTURE COMPLETION

Future can be marked COMPLETED when:

- the expected path reaches its final meaningful stage
- the configured scenario objective is achieved
- the scenario has sufficiently resolved
- configured completion conditions are met

Completion does not mean guaranteed profit.

It means the expected scenario has reached its defined endpoint.


16. FUTURE EXPIRATION

Every Future may have a validity period.

Example:

Created:
13:40

Valid until:
14:30

At expiry:

ACTIVE
↓
EXPIRED

Remove active Future visualization.

Keep the complete historical record.


17. FUTURE CANCELLATION

Provide:

[ CANCEL FUTURE ]

When cancelled:

ACTIVE
↓
CANCELLED
↓
Remove active Future
↓
Keep history

Cancellation must not modify:

- Local
- Target
- Full
- Telegram


18. ACTIVE ANALYSIS REGISTRY

Create an efficient registry for active objects.

Example:

ActiveAnalysisRegistry

├── LocalSignals
├── ActiveTargets
├── ActiveFullAnalyses
└── ActiveFutureScenarios

Only active objects should be monitored continuously.

Historical records should not be repeatedly scanned during every market update.


19. LIFECYCLE MONITOR PIPELINE

Market Data Update
      ↓
Data Validation
      ↓
Load Active Objects
      ↓
Determine Relevant Changes
      ↓
Evaluate Local
      ↓
Evaluate Target
      ↓
Evaluate Full
      ↓
Evaluate Future
      ↓
Apply State Transitions
      ↓
Persist Changes
      ↓
Publish UI Events
      ↓
Update Active Registry


20. STATE TRANSITIONS

Every state transition must be explicit.

Do not allow arbitrary state changes.

Example:

ACTIVE
→ INVALIDATED

valid

ACTIVE
→ CANCELLED

valid

INVALIDATED
→ ACTIVE

invalid

COMPLETED
→ ACTIVE

invalid

The system must enforce valid state-transition rules.


21. STATE TRANSITION SERVICE

Create a focused lifecycle component:

IAnalysisLifecycleService

or separate services:

ILocalSignalLifecycle
ITargetLifecycle
IFullAnalysisLifecycle
IFutureScenarioLifecycle

Prefer separate focused services when responsibilities become large.

Each transition must have:

- CurrentState
- RequestedState
- Reason
- Timestamp
- Trigger
- Source
- Relevant market data reference


22. LIFECYCLE EVENT MODEL

Create a reusable lifecycle event structure.

AnalysisLifecycleEvent

├── Id
├── EntityId
├── EntityType
├── PreviousState
├── NewState
├── Reason
├── Trigger
├── Symbol
├── Timeframe
├── Price
├── EventTime
├── DataVersion
└── CreatedAt

Examples:

BUY → INVALIDATED
TARGET ACTIVE → HIT
FUTURE ACTIVE → EXPIRED
FULL ACTIVE → CANCELLED


23. REASON CODES

Use structured reason codes instead of arbitrary text.

Examples:

LOCAL_INVALIDATION
LOCAL_TIMEOUT
LOCAL_TARGET_REACHED

TARGET_HIT
TARGET_INVALIDATION
TARGET_TIMEOUT
TARGET_CANCELLED

FULL_INVALIDATION
FULL_STALE
FULL_TIMEOUT
FULL_CANCELLED

FUTURE_STAGE_COMPLETED
FUTURE_INVALIDATION
FUTURE_TIMEOUT
FUTURE_CANCELLED
FUTURE_SCENARIO_COMPLETED

DATA_STALE
DATA_INVALID
PROVIDER_FAILURE
MANUAL_CANCEL


24. MARKET DATA VERSIONING

Every lifecycle decision should reference the market data used to make the transition.

Store:

- Provider
- Data timestamp
- Candle timestamp
- Timeframe
- Data version
- Market snapshot ID if available

This allows later investigation:

“What exact market data caused this lifecycle transition?”


25. LOOK-AHEAD PROTECTION

Lifecycle monitoring must never use future data.

Rule:

Evidence.AvailableAt <= AnalysisTime

and:

MarketData.Timestamp <= EvaluationTime

Do not allow later candles or future news information to determine an earlier lifecycle result.

This is especially important for future backtesting.


26. NEWS-AWARE LIFECYCLE

Important economic events may invalidate existing analysis.

Example:

FULL BUY
↓
FOMC release
↓
Extreme market reaction
↓
Existing assumptions may be invalid
↓
Re-evaluate / invalidate according to configured rules

Do not automatically assume:

News = invalid

Instead evaluate the actual market reaction and configured invalidation conditions.


27. STALE ANALYSIS

Every active analysis should have configurable freshness rules.

Example:

Fresh
↓
Still valid

Stale
↓
Needs re-evaluation / expiration

Invalid
↓
End active state

Do not keep an analysis active forever simply because no explicit invalidation price was reached.


28. VALIDITY WINDOWS

Support configurable validity windows by:

- Analysis type
- Timeframe
- Scenario type
- Market condition

Examples:

Local M5
→ shorter validity

Full 15M
→ longer validity

Future
→ scenario-specific validity

Target
→ target-specific validity

Do not hardcode one global validity period for everything.


29. TIME EXPIRATION

When validity expires:

ACTIVE
↓
EXPIRED
↓
Remove active UI
↓
Preserve history

Do not automatically create a replacement analysis.

A new analysis must be explicitly requested or triggered by the appropriate independent system.


30. DUPLICATE PROTECTION

The lifecycle system must prevent duplicate transitions.

Example:

Price reaches Target.

Bad:

TARGET HIT
TARGET HIT
TARGET HIT

Correct:

TARGET ACTIVE
↓
TARGET HIT

Then additional market ticks must not create another HIT event.


31. IDEMPOTENCY

Lifecycle processing must be idempotent.

If the same market update is processed twice:

First:

ACTIVE → HIT

Second:

No additional transition.

Use:

- Event IDs
- Market data IDs
- timestamps
- state checks
- fingerprints/hashes

where appropriate.


32. CONCURRENCY

Handle multiple market updates safely.

Prevent:

- double completion
- double invalidation
- conflicting state transitions
- duplicate lifecycle events

Use appropriate:

- optimistic concurrency
- row/version tokens
- transaction boundaries
- unique constraints

depending on the existing architecture.


33. FAILURE HANDLING

If lifecycle processing fails:

Do not fabricate a state transition.

Example:

Database failure
↓
Retry

Provider failure
↓
Pause evaluation

Invalid data
↓
Skip evaluation

Unknown state
↓
Log and fail safely

Never:

Market data failure
→ INVALIDATED

unless the configured lifecycle rule explicitly says the analysis should expire/invalidate because of data quality.


34. ACTIVE UI SYNCHRONIZATION

When state changes, the UI must update.

Example:

ACTIVE
↓
INVALIDATED

Then:

- Remove active chart object
- Remove active panel if applicable
- Preserve historical record

For Full Analyst:

ACTIVE
↓
INVALIDATED

Remove:

- active Full panel
- active Future representation

But preserve:

- Full analysis history
- Future history
- lifecycle events


35. FULL + FUTURE RELATIONSHIP

Full and Future are related but have separate lifecycles.

Example:

FULL
🟢 BUY
ACTIVE

FUTURE
🔮 Bullish Path
ACTIVE

If Future becomes invalid:

FULL
🟢 BUY
ACTIVE

FUTURE
⚠️ INVALIDATED

This is allowed.

Future invalidation does not automatically invalidate Full Analyst.

Likewise:

Full Analyst invalidation should normally end its associated active Future because the Future depends on the Full decision remaining valid.

The dependency must be explicit and deterministic.


36. FULL INVALIDATION → FUTURE

When Full Analyst becomes invalid:

FULL
↓
INVALIDATED

Then its active Future should transition to:

FUTURE
↓
INVALIDATED

Reason:

PARENT_FULL_ANALYSIS_INVALIDATED

Do not leave a Future active when the Full analysis it depends on has ended.


37. TARGET INDEPENDENCE FROM FULL

Target Analyst remains independent.

Example:

TARGET
🎯 4,205
ACTIVE

FULL
⚪ WAIT

Target does not automatically disappear.

Only Target's own lifecycle rules determine its state.


38. LOCAL INDEPENDENCE

Local lifecycle must remain independent.

Example:

LOCAL
🟢 BUY
ACTIVE

FULL
🔴 SELL
ACTIVE

TARGET
🎯 4,205
ACTIVE

FUTURE
🔮 Bearish
ACTIVE

All four can exist simultaneously.


39. NO AUTOMATIC TRADING

Phase 16 must never:

- open a trade
- close a trade
- modify an order
- move a broker stop loss
- send trading execution commands

Lifecycle monitoring only manages analysis states and UI/history.


40. PERSISTENCE

Persist:

- Current state
- Previous state
- State reason
- Transition time
- Market price
- Market data reference
- ValidUntil
- Entity ID
- Parent ID
- Lifecycle events
- Cancellation information
- Completion information

Never rely only on in-memory state.

If the application restarts, it must reconstruct active analysis correctly.


41. RESTART RECOVERY

On application startup:

Load active registry
      ↓
Validate active records
      ↓
Check validity
      ↓
Check latest market data
      ↓
Resume monitoring

The system must not lose active:

- Local signals
- Targets
- Full analyses
- Future scenarios

after restart.


42. OFFLINE / DATA GAP HANDLING

If the application was offline:

Do not assume the analysis stayed valid.

After reconnect:

Recover market data
      ↓
Determine missing period
      ↓
Backfill
      ↓
Evaluate lifecycle
      ↓
Apply valid transitions

Avoid using the current price alone to reconstruct what happened during the missing period.


43. LIFECYCLE HISTORY

Keep complete historical records.

Example:

Analysis:

ACTIVE
↓
Stage Changed
↓
Invalidation Warning
↓
INVALIDATED

History must preserve every meaningful transition.

This is required for:

- debugging
- performance analysis
- backtesting
- accuracy measurement
- AI calibration
- user review


44. UI HISTORY

Active UI should be temporary.

Example:

ACTIVE
→ chart/panel visible

ENDED
→ active representation removed

HISTORY
→ historical record retained

Do not delete the analysis itself when it ends.


45. MONITORING FREQUENCY

Do not force one monitoring frequency for all systems.

Use the smallest practical evaluation frequency based on:

- timeframe
- market-data frequency
- lifecycle condition
- analysis type

Example:

M5 Local
→ monitor frequently

Target
→ monitor against current price/data

Full
→ monitor meaningful state changes

Future
→ monitor path conditions

Avoid unnecessary AI calls.


46. PERFORMANCE OPTIMIZATION

Use:

- Active-only queries
- Cached active objects
- State hashes
- Incremental processing
- Event-driven updates
- Batch lifecycle evaluation where appropriate
- Database indexes
- Efficient time-based queries
- Idempotent event processing

Do not scan all historical analysis records on every tick.


47. DATABASE INDEXES

Consider indexes for:

- Status
- Symbol
- Timeframe
- ValidUntil
- CreatedAt
- UpdatedAt
- FullAnalysisId
- Target ID
- FutureScenarioId
- Active state
- Lifecycle event timestamp

Optimize based on actual query patterns.


48. OBSERVABILITY

Log:

- Lifecycle evaluation started
- Lifecycle transition
- Lifecycle transition rejected
- Lifecycle evaluation skipped
- Data stale
- Data invalid
- Provider failure
- Recovery started
- Recovery completed
- Duplicate event prevented
- Concurrency conflict
- Future invalidated by Full invalidation

Metrics:

- Active analysis count
- Transition count
- Invalidations
- Expirations
- Target hits
- Future completions
- Future invalidations
- Average lifecycle processing time
- Failed evaluations
- Duplicate prevention count


49. TESTING

Create unit tests for every lifecycle.

LOCAL:

- NOTHING → BUY
- NOTHING → SELL
- BUY → STOP
- SELL → STOP
- ACTIVE → EXPIRED
- Duplicate processing → no duplicate event

TARGET:

- ANALYZING → ACTIVE
- ACTIVE → HIT
- ACTIVE → INVALIDATED
- ACTIVE → EXPIRED
- ACTIVE → CANCELLED
- HIT cannot return to ACTIVE

FULL:

- REQUESTED → ANALYZING
- ANALYZING → BUY
- ANALYZING → SELL
- ANALYZING → WAIT
- ACTIVE → INVALIDATED
- ACTIVE → EXPIRED
- ACTIVE → CANCELLED
- WAIT does not create Future

FUTURE:

- PENDING → ANALYZING
- ANALYZING → ACTIVE
- ACTIVE → COMPLETED
- ACTIVE → INVALIDATED
- ACTIVE → EXPIRED
- ACTIVE → CANCELLED
- Full invalidation → Future invalidation


50. INTEGRATION TESTS

Test complete flows.

Example:

Full BUY
↓
Future created
↓
Future ACTIVE
↓
Future stage progresses
↓
Future COMPLETED

Another:

Full BUY
↓
Future ACTIVE
↓
Full invalidated
↓
Future invalidated

Another:

Target ACTIVE
↓
Price reaches target
↓
Target HIT
↓
Local remains unchanged
↓
Full remains unchanged

Another:

Telegram signal
↓
No lifecycle modification to Local/Target/Full/Future


51. CRASH RECOVERY TEST

Simulate:

ACTIVE Future
↓
Application crashes
↓
Restart
↓
Recover Future
↓
Continue monitoring

The state must remain correct.

Repeat for:

- Local
- Target
- Full
- Future


52. CONCURRENCY TEST

Simulate multiple workers processing the same market update.

Expected:

One valid lifecycle transition.

Not:

Multiple identical transitions.


53. FINAL ARCHITECTURE

                         XAUUSD MARKET DATA
                                  │
                                  ▼
                       LIFECYCLE MONITOR
                                  │
          ┌───────────────────────┼────────────────────────┐
          │                       │                        │
          ▼                       ▼                        ▼
       LOCAL                    TARGET                    FULL
       LIFECYCLE                LIFECYCLE                LIFECYCLE
                                                              │
                                                              ▼
                                                           FUTURE
                                                          LIFECYCLE

Each lifecycle is independent.

Full → Future is the only explicit parent dependency.

Local and Target do not depend on Full.

Telegram remains completely independent.


54. PHASE 16 DELIVERABLES

By the end of Phase 16:

- Active analysis registry
- Local lifecycle monitoring
- Target lifecycle monitoring
- Full Analyst lifecycle monitoring
- Future lifecycle monitoring
- Explicit state machines
- Valid state transitions
- Lifecycle event model
- Reason codes
- Data-version tracking
- Look-ahead protection
- Validity windows
- Expiration handling
- Invalidation handling
- Completion handling
- Cancellation handling
- Duplicate protection
- Idempotent processing
- Concurrency protection
- Data-quality handling
- Provider failure handling
- Restart recovery
- Offline/data-gap recovery
- Active UI synchronization
- Historical lifecycle persistence
- Full → Future dependency handling
- Local/Target/Full independence
- No automatic trading
- Unit tests
- Integration tests
- Crash recovery tests
- Concurrency tests
- Performance optimization
- Observability and metrics


PHASE 16 COMPLETION CRITERIA

The phase is complete when the system can reliably manage all active analysis lifecycles:

LOCAL
🟢 BUY / 🔴 SELL
↓
ACTIVE
↓
STOP / EXPIRED / COMPLETED


TARGET
🎯 ONE TARGET
↓
ACTIVE
↓
HIT / INVALIDATED / EXPIRED / CANCELLED


FULL
🟢 BUY / 🔴 SELL
↓
ACTIVE
↓
INVALIDATED / EXPIRED / COMPLETED / CANCELLED


FULL
⚪ WAIT
↓
NO FUTURE


FUTURE
🔮 EXPECTED PATH
↓
ACTIVE
↓
STAGE PROGRESS
↓
COMPLETED / INVALIDATED / EXPIRED / CANCELLED


The system must preserve every lifecycle transition in history while removing ended objects from the active UI.

After Phase 16 is completed, continue to:

PHASE 17 — TRADE JOURNAL & BACKTESTING