PHASE 15 — FUTURE SCENARIO & EXPECTED PATH

OBJECTIVE

Build the Future Scenario & Expected Path system for AnalystTradingSystem.

The purpose of this phase is to create a structured expected short-term market path from a successful Full Analyst BUY or SELL decision.

Future Scenario is NOT a guaranteed prediction.
Future Scenario is NOT fake future candles.
Future Scenario is NOT Target Analyst.
Future Scenario is NOT Local Analyst.
Future Scenario is NOT Telegram.
Future Scenario does NOT execute trades.

Future represents the most likely conditional market path supported by the Full Analyst evidence.

The system must always treat the Future as a scenario, not a certainty.

CORE FLOW

Future Analyst
      ↓
BUY / SELL / WAIT
      ↓
If WAIT
      ↓
NO FUTURE

If BUY / SELL
      ↓
Future Scenario Builder
      ↓
Expected Path
      ↓
Future Monitoring
      ↓
Path Progress / Re-evaluation / Invalidation / Completion / Expiry / Cancellation


1. FUTURE CREATION RULE

Future Scenario can only be created when Full Analyst produces:

🟢 BUY
or
🔴 SELL

If Full Analyst produces:

⚪ WAIT

then:

NO FUTURE SCENARIO
NO EXPECTED PATH

Do not create a Future object for WAIT.

Do not force a scenario when evidence is weak, conflicting, stale, invalid, or insufficient.


2. FUTURE INDEPENDENCE

Future Scenario must remain independent from:

- Local Analyst
- Target Analyst
- Telegram Scanner
- Trade Execution

Local Analyst must never determine Future.

Target Analyst must never determine Future.

Telegram signals must never determine Future.

Future must only use the validated Full Analyst result and its supporting evidence.

Example:

LOCAL
🟢 BUY

TARGET
🎯 4,205

FULL
🔴 SELL

FUTURE
🔮 Bearish Expected Path

This is valid.

Future follows Full Analyst only.


3. FUTURE PURPOSE

Future Scenario answers:

“What is the expected market path if the current Full Analyst decision remains valid?”

The path may describe:

- Pullback
- Rejection
- Support reaction
- Resistance reaction
- Liquidity interaction
- Breakout
- Breakdown
- Continuation
- Structure development
- Momentum continuation
- Expected reaction zones

The system must not claim:

- guaranteed direction
- guaranteed profit
- guaranteed target
- guaranteed future price
- guaranteed timing
- guaranteed market behavior


4. EXPECTED PATH

Future must be represented as a structured sequence of conditional stages.

BUY example:

NOW
↓
Pullback
↓
Support reaction
↓
Bullish continuation
↓
Resistance test

SELL example:

NOW
↓
Rejection
↓
Lower-high formation
↓
Bearish continuation
↓
Support / liquidity test

Each path stage should contain:

- Sequence
- Stage
- ExpectedBehavior
- TriggerCondition
- InvalidationCondition
- OptionalPriceZone
- Confidence
- Status


5. DO NOT GENERATE FAKE FUTURE CANDLES

Do not generate artificial OHLC candles such as:

13:40 → 4,180
13:45 → 4,185
13:50 → 4,191
13:55 → 4,198

Do not present predicted candles as if they are real future market data.

Instead use a structured expected path:

NOW
↓
Expected Pullback
↓
Support Reaction
↓
Continuation
↓
Resistance Test

The UI may draw a visual path or scenario overlay, but it must clearly represent an expected scenario rather than actual future candles.


6. PRICE ZONES

Future may use validated market zones such as:

- Current Price
- Support Zone
- Resistance Zone
- Liquidity Zone
- Expected Reaction Zone
- Invalidation Zone

Example:

Current Price: 4,180
Support Zone: 4,170–4,174
Liquidity Zone: 4,190–4,194
Resistance Zone: 4,198–4,202
Invalidation: below 4,165

These values must come from validated evidence or deterministic calculations.

Never invent levels simply to make the Future look complete.


7. FUTURE INPUT

Future Scenario should receive the validated Full Analyst result.

Required information:

- FullAnalysisId
- Symbol
- Timeframe
- Decision
- Confidence
- CurrentPrice
- AnalysisTime
- Entry reference if available
- Invalidation reference
- EvidenceIds
- SpecialistResults
- MasterResult
- Data versions
- Prompt version
- Provider information

Future should reuse the validated Full Analyst evidence.

Do not unnecessarily rerun the complete Full Analyst.


8. FUTURE SCENARIO TYPES

Support configurable scenario types:

- CONTINUATION
- PULLBACK_CONTINUATION
- BREAKOUT
- BREAKDOWN
- REVERSAL
- RANGE
- CUSTOM

The system must not force a scenario type when evidence does not support one.


9. FUTURE DATA MODEL

Create a dedicated FutureScenario entity:

FutureScenario
├── Id
├── FullAnalysisId
├── Symbol
├── Timeframe
├── Direction
├── CurrentPrice
├── AnalysisTime
├── ValidUntil
├── Confidence
├── ScenarioType
├── Status
├── InvalidationPrice
├── EntryReference
├── CreatedAt
├── UpdatedAt
└── CompletedAt


Direction:

- BUY
- SELL

Status:

- PENDING
- ANALYZING
- ACTIVE
- INVALIDATED
- COMPLETED
- EXPIRED
- CANCELLED


10. FUTURE PATH MODEL

Create a separate FuturePathStage model:

FuturePathStage
├── Id
├── FutureScenarioId
├── Sequence
├── Stage
├── ExpectedBehavior
├── ReferencePrice
├── PriceZone
├── TriggerCondition
├── InvalidationCondition
├── Confidence
└── Status

Example:

1. PULLBACK
2. SUPPORT_REACTION
3. CONTINUATION
4. RESISTANCE_TEST

The stages must remain conditional.


11. CONDITIONAL LOGIC

Every expected path must define the conditions required for the path to remain valid.

Example:

Expected:

Pullback → Support Hold → Continuation

Required conditions:

- Support remains valid
- Bullish momentum returns
- Bullish structure remains valid
- No major bearish invalidation
- Higher-timeframe context remains supportive

If conditions fail:

Future
↓
Condition Failure
↓
Re-evaluate or Invalidate


12. FUTURE INVALIDATION

Future invalidation must be separate from Local Analyst STOP.

Do not use:

🟡 STOP

for Future.

That state belongs only to Local Analyst.

Future should use:

⚠️ FUTURE INVALIDATED

Example:

🔮 FUTURE
🟢 BULLISH

Pullback
↓
Support Reaction
↓
Continuation

Then support breaks:

⚠️ FUTURE PATH INVALIDATED

Reason:
Expected support failed and bearish confirmation was detected.

Invalidation must remove the active Future representation while preserving the historical record.


13. FUTURE LIFECYCLE

Lifecycle:

FULL ANALYST BUY/SELL
        ↓
CREATE FUTURE
        ↓
PENDING
        ↓
ANALYZING
        ↓
ACTIVE
        ↓
Monitor Expected Path
        ↓
┌───────────────┬───────────────┬──────────────┬──────────────┐
↓               ↓               ↓              ↓
COMPLETED    INVALIDATED      EXPIRED      CANCELLED
        ↓
Historical Record


14. FUTURE MONITORING

Future must be monitored continuously while active.

Do not run AI on every tick.

Use deterministic monitoring first.

New Market Data
      ↓
Future Monitor
      ↓
State Change Detection
      ↓
┌───────────────┬────────────────┬─────────────────┐
↓               ↓                ↓
No Change    Stage Completed   Important Change
↓               ↓                ↓
Continue      Advance Stage    Re-evaluate
                                      ↓
                                AI only if needed

The system must monitor:

- Current price
- Price zones
- Structure
- Liquidity
- Momentum
- Volatility
- Invalidation conditions
- Relevant news state
- Time validity


15. RE-EVALUATION

Future should be re-evaluated only when meaningful changes occur.

Examples:

- Major structure change
- Liquidity sweep
- Breakout
- Breakdown
- Strong volatility change
- Major news release
- Important support/resistance failure
- Invalidation condition approaching
- Full Analyst re-analysis
- Validity threshold reached

Do not repeatedly call AI without a meaningful reason.


16. FUTURE AI

Future may use AI when scenario interpretation requires it.

Preferred flow:

Full Analyst Master Result
        ↓
Validated Evidence
        ↓
Future Scenario Builder
        ↓
Optional Future AI
        ↓
Structured Future Scenario
        ↓
Validation
        ↓
Active Future

Future AI must not independently replace Full Analyst.

Future AI must not generate BUY/SELL.

The BUY/SELL decision already comes from Full Analyst.


17. FUTURE AI CONFIGURATION

Future AI must have its own independent provider configuration.

Example:

FUTURE_AI_ENABLED=true

FUTURE_AI_PROVIDER=
FUTURE_AI_API_KEY=
FUTURE_AI_MODEL=
FUTURE_AI_BASE_URL=
FUTURE_AI_TIMEOUT_SECONDS=
FUTURE_AI_MAX_TOKENS=
FUTURE_AI_TEMPERATURE=

Do not hardcode OpenAI.

The provider must remain configurable.


18. FUTURE AI OUTPUT

AI output must be structured.

Example:

{
  "scenarioType": "PULLBACK_CONTINUATION",
  "direction": "BUY",
  "summary": "Pullback toward support followed by bullish continuation if support holds.",
  "confidence": 84,
  "validUntil": "...",
  "paths": [
    {
      "sequence": 1,
      "stage": "PULLBACK",
      "expectedBehavior": "Price retraces toward the identified support zone.",
      "triggerCondition": "Support remains intact."
    },
    {
      "sequence": 2,
      "stage": "SUPPORT_REACTION",
      "expectedBehavior": "Bullish reaction develops near support.",
      "triggerCondition": "Bullish confirmation appears."
    },
    {
      "sequence": 3,
      "stage": "CONTINUATION",
      "expectedBehavior": "Price resumes upward momentum.",
      "triggerCondition": "Bullish structure remains valid."
    }
  ],
  "invalidation": {
    "condition": "Confirmed support breakdown with bearish momentum."
  }
}

The application must validate this response before storing it.


19. FUTURE VALIDATION

Validate:

- Symbol matches
- Timeframe matches
- Direction matches Full Analyst
- FullAnalysisId exists
- Scenario type is valid
- At least one path stage exists
- Sequence numbers are valid
- ValidUntil is valid
- Invalidation condition exists
- Confidence is within configured range
- No fabricated market data
- No guaranteed outcome
- No automatic trading instruction

If AI output is malformed:

AI Output
↓
Validation Failed
↓
No Active Future

Never fabricate missing fields.


20. FUTURE CONFIDENCE

Confidence represents confidence in the expected scenario based on evidence.

Example:

Confidence: 84/100

means:

“The available evidence strongly supports this scenario.”

It does NOT mean:

“84% guaranteed winning trade.”

Do not represent confidence as guaranteed profit probability.


21. FUTURE UI

Future should appear only after Full Analyst produces BUY or SELL.

Example:

┌──────────────────────────────────────┐
│ 🔮 FUTURE SCENARIO                   │
│ XAUUSD · 15M                         │
│                                      │
│ 🟢 BULLISH EXPECTED PATH             │
│ Confidence: 84/100                   │
│                                      │
│ NOW                                  │
│  │                                   │
│  ▼                                   │
│ Pullback                             │
│  │                                   │
│  ▼                                   │
│ Support Reaction                     │
│  │                                   │
│  ▼                                   │
│ Bullish Continuation                 │
│  │                                   │
│  ▼                                   │
│ Resistance Test                      │
│                                      │
│ ⚠️ Invalid if support breaks        │
│                                      │
│ Valid Until: ...                     │
│                                      │
│        [ CANCEL FUTURE ]             │
└──────────────────────────────────────┘

SELL example:

┌──────────────────────────────────────┐
│ 🔮 FUTURE SCENARIO                   │
│ XAUUSD · 15M                         │
│                                      │
│ 🔴 BEARISH EXPECTED PATH             │
│                                      │
│ NOW                                  │
│  ↓                                   │
│ Rejection                            │
│  ↓                                   │
│ Lower High                           │
│  ↓                                   │
│ Bearish Continuation                 │
│  ↓                                   │
│ Support / Liquidity Test             │
│                                      │
│ ⚠️ Invalid if bearish structure fails│
│                                      │
│        [ CANCEL FUTURE ]             │
└──────────────────────────────────────┘


22. CHART REPRESENTATION

Future must be displayed as an expected path overlay.

Example:

Current Price
     ●
      \
       \ Expected Pullback
        \
         ● Support
          \
           ↗
            ↗ Expected Continuation
             ↗
              ● Resistance

The visual path must clearly be different from:

Local BUY/SELL signals
Target objects
Actual market candles

Do not make predicted candles look like real historical candles.


23. FUTURE AND TARGET SEPARATION

Target Analyst and Future Scenario are different systems.

Target:

🎯 ONE TARGET

Future:

🔮 EXPECTED MARKET PATH

Example:

TARGET ANALYST
🎯 Target: 4,205

FULL ANALYST
🟢 BUY

FUTURE
🔮 Pullback
→ Support reaction
→ Continuation
→ Resistance test

Do not automatically convert the Target into the Future endpoint.

Do not use Target Analyst conclusions to create Future.


24. FUTURE AND LOCAL SEPARATION

Local Analyst and Future are independent.

Example:

LOCAL
🟢 BUY
🎯 Local Target

FULL
🔴 SELL

FUTURE
🔮 Bearish Path

This must be supported.

Future must never modify Local signals.


25. FUTURE AND TELEGRAM SEPARATION

Telegram Scanner remains completely independent.

Example:

TELEGRAM
⚡ FAST BUY

FULL
🔴 SELL

FUTURE
🔮 Bearish Expected Path

This is valid.

Telegram must never modify Future.


26. ACTIVE FUTURE RULE

Only one active Future Scenario should exist for each active Full Analyst analysis.

Do not create:

Full Analysis
├── Future 1
├── Future 2
└── Future 3

for the same active analysis.

Historical revisions are allowed, but only one current Future should be ACTIVE.


27. FUTURE REVISIONS

Do not destroy historical Future information when the scenario changes.

Use revisions or versioning.

Example:

Future V1
↓
Market changes
↓
Future V2

Store the previous version for historical analysis.

This is important for later:

Phase 17 — Trade Journal & Backtesting

Phase 18 — Performance, Accuracy & AI Calibration


28. FUTURE EVENTS

Create FutureScenarioEvent records.

Example:

FutureScenarioEvent
├── Id
├── FutureScenarioId
├── EventType
├── ActualPrice
├── ActualTime
├── ExpectedStage
├── Result
└── CreatedAt

Possible event types:

- PATH_STARTED
- PULLBACK_DETECTED
- ZONE_REACHED
- REACTION_CONFIRMED
- CONTINUATION_CONFIRMED
- RESISTANCE_REACHED
- SUPPORT_REACHED
- INVALIDATION_TRIGGERED
- EXPIRATION
- CANCELLED
- REEVALUATION


29. EXPECTED VS ACTUAL

The system must preserve enough information to compare:

EXPECTED
↓
ACTUAL
↓
RESULT

Example:

Expected:
Pullback → Support → Continuation

Actual:
Pullback → Support → Continuation

Result:
PATH ALIGNED

Another:

Expected:
Pullback → Support → Continuation

Actual:
Support breaks → Reversal

Result:
PATH INVALIDATED

This information will later support backtesting and calibration.


30. NEWS IMPACT

Future monitoring must be aware of important economic events.

Example:

ACTIVE FUTURE
↓
FOMC RELEASE
↓
Extreme volatility
↓
Existing scenario may no longer be valid
↓
Re-evaluation

Do not assume:

CPI = BUY

or:

FOMC = SELL

The system must consider the actual XAUUSD reaction and evidence.

Future may be:

- paused
- re-evaluated
- invalidated

depending on configured rules.


31. DATA QUALITY

Future must handle:

- Missing candles
- Duplicate candles
- Delayed data
- Out-of-order data
- Provider switching
- Stale data
- Price gaps
- Temporary provider failure
- Timezone differences

If market data becomes temporarily unreliable:

ACTIVE FUTURE
↓
DATA INVALID / STALE
↓
PAUSE MONITORING

Do not automatically invalidate a Future only because the provider temporarily failed.


32. PERFORMANCE

Future monitoring must be lightweight.

Do not implement:

Every Tick
→ Full Database Query
→ All Indicators
→ All AI Specialists
→ New AI Future

Instead:

Incoming Market Data
↓
State Change Detection
↓
Does Future-relevant state change?
├── NO → Continue
└── YES → Re-evaluate

Use caching for:

- Active Future
- Current path stage
- Relevant price zones
- Invalidation conditions
- Last processed market state
- Evidence state


33. DATABASE

Create dedicated tables:

- FutureScenarios
- FuturePathStages
- FutureScenarioEvents
- FutureScenarioRevisions

Relationship:

FullAnalysis
    ↓
FutureScenario
    ├── FuturePathStages
    ├── FutureScenarioRevisions
    └── FutureScenarioEvents


34. APPLICATION SERVICES

Create focused services/interfaces:

- IFutureScenarioService
- IFuturePathBuilder
- IFutureScenarioValidator
- IFutureScenarioMonitor
- IFutureScenarioLifecycle
- IFutureScenarioReevaluationService
- IFutureScenarioEventRecorder
- IFutureScenarioRepository

Keep responsibilities separated.

Do not create one giant FutureScenarioService containing the entire system.


35. API

Suggested endpoints:

POST   /api/future-scenarios
GET    /api/future-scenarios/{id}
GET    /api/future-scenarios/active
POST   /api/future-scenarios/{id}/cancel
POST   /api/future-scenarios/{id}/reevaluate
GET    /api/future-scenarios/{id}/events

Future creation should normally be triggered internally from a successful Full Analyst result.


36. CANCELLATION

Provide:

[ CANCEL FUTURE ]

When cancelled:

ACTIVE FUTURE
↓
CANCELLED
↓
Remove active Future UI/path
↓
Keep historical record

Cancellation must not:

- Cancel Full Analyst
- Cancel Target Analyst
- Modify Local Analyst
- Modify Telegram
- Execute a trade


37. SECURITY

Validate:

- User authentication
- User ownership/access
- FullAnalysis ownership
- Valid Future state transitions
- Authorized cancellation
- Authorized re-evaluation

Never allow one user to modify another user's Future Scenario.


38. TESTING

Create tests for:

Creation:

- BUY → Future created
- SELL → Future created
- WAIT → Future not created

Independence:

- Local BUY + Full SELL → Future follows Full SELL
- Target BUY + Full SELL → Future follows Full SELL
- Telegram BUY + Full SELL → Future follows Full SELL

Lifecycle:

- ACTIVE → COMPLETED
- ACTIVE → INVALIDATED
- ACTIVE → EXPIRED
- ACTIVE → CANCELLED

Path:

- Support holds → stage can advance
- Support breaks → Future invalidates
- Valid condition → continue
- Important state change → re-evaluate

AI:

- Valid response → Future created
- Malformed response → no active Future
- Timeout → no fabricated Future

Data:

- Duplicate candle → no duplicate event
- Out-of-order data → safely handled
- Missing data → monitoring paused
- Provider failure → no false invalidation


39. BACKTESTING COMPATIBILITY

Design Future Scenario so historical data can replay the scenario.

The system should eventually be able to answer:

At analysis time:

- What did Full Analyst decide?
- What Future Scenario was created?
- What path was expected?
- What conditions were active?
- What happened afterward?
- Which stages completed?
- Where did the scenario fail?
- Was the path aligned with actual market behavior?

Do not design Future as a temporary UI-only feature.

Persist the necessary information for future backtesting and calibration.


40. OBSERVABILITY

Log important lifecycle events:

- FutureScenarioCreated
- FutureScenarioActivated
- FutureStageChanged
- FutureConditionChanged
- FutureReevaluationRequested
- FutureInvalidated
- FutureCompleted
- FutureExpired
- FutureCancelled
- FutureAIRequested
- FutureAICompleted
- FutureAIRejected

Track:

- AnalysisId
- FutureScenarioId
- Symbol
- Timeframe
- Direction
- Provider
- Model
- PromptVersion
- Latency
- Status
- Reason

Never log API keys or secrets.


41. FINAL ARCHITECTURE

                         XAUUSD DATA
                              │
          ┌───────────────────┼────────────────────┐
          │                   │                    │
          ▼                   ▼                    ▼
       LOCAL               TARGET                FULL
       ANALYST              ANALYST              ANALYST
          │                   │                    │
          │              🎯 Target             BUY/SELL
          │                                        │
          │                                        ▼
          │                                  🔮 FUTURE
          │                                        │
          │                              Expected Path
          │                                        │
          └────────────── INDEPENDENT ─────────────┘

                         TELEGRAM
                    Completely independent


42. PHASE 15 DELIVERABLES

By the end of Phase 15:

- Future Scenario domain model
- Future Path model
- Future lifecycle
- Full Analyst → Future integration
- BUY/SELL-only Future creation
- WAIT → no Future
- Structured expected path
- Conditional path stages
- Price-zone support
- Invalidation conditions
- Validity and expiry
- Deterministic Future monitoring
- State-change detection
- Optional Future AI
- Independent Future AI provider configuration
- AI output validation
- Future revisions/versioning
- Future event history
- Expected-vs-actual tracking
- Cancel Future functionality
- API endpoints
- Database persistence
- Data-quality protection
- News-aware re-evaluation
- Provider failure handling
- Security/authorization
- Unit tests
- Integration tests
- Performance tests
- Backtesting-compatible history
- No automatic trading
- No dependency on Local Analyst conclusions
- No dependency on Target Analyst conclusions
- No dependency on Telegram signals


PHASE 15 COMPLETION CRITERIA

The phase is complete when:

FULL ANALYST
      │
      ├── ⚪ WAIT
      │      ↓
      │   NO FUTURE
      │
      ├── 🟢 BUY
      │      ↓
      │   🔮 BULLISH EXPECTED PATH
      │
      └── 🔴 SELL
             ↓
          🔮 BEARISH EXPECTED PATH

The Future Scenario can then:

MONITOR
↓
ADVANCE PATH
↓
DETECT IMPORTANT CHANGES
↓
RE-EVALUATE WHEN NECESSARY
↓
COMPLETE / INVALIDATE / EXPIRE / CANCEL

while preserving the complete historical record for Phase 17 Backtesting and Phase 18 Performance & AI Calibration.

After Phase 15 is completed, continue to:

PHASE 16 — ANALYSIS & TARGET LIFECYCLE MONITORING