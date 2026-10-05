# PHASE 13 — TARGET ANALYST & AI WORKSPACES

## OBJECTIVE

Build the Target Analyst as an independent, AI-powered analysis system whose purpose is to predict the **best-supported future price target** from available market, news, economic, macro, and analyst evidence.

Each user click on **[TARGET ANALYST]** creates **one new target analysis**.

The Target Analyst must remain completely independent from:

* 🟢 Local Analyst
* 🔮 Full Analyst

It must not copy or inherit their BUY/SELL/WAIT decisions.

---

## CORE FLOW

User clicks [TARGET ANALYST]
↓
Create Target Analysis Job
↓
Capture Analysis Snapshot
↓
Validate Available Evidence
↓
Collect Market + News + Economic + Analyst Evidence
↓
Compress / Normalize Evidence
↓
8 Independent AI Workspaces
↓
Compare Specialist Results
↓
Target Master AI
↓
Target Validation
↓
🎯 ONE TARGET
↓
Target Result Panel
↓
Lifecycle Monitoring

---

## TARGET ANALYST PURPOSE

The Target Analyst answers:

> "Based on the evidence available now, what is the most defensible price target the market could reach?"

It is predictive and forward-looking.

It is NOT a real-time BUY/SELL engine.

It does not need to continuously react to every market tick like the Full Analyst.

---

## ONE CLICK = ONE TARGET

Every successful Target Analyst run must produce exactly:

* One target price
* One invalidation/SL level or invalidation condition
* One analysis result
* One lifecycle

Do not generate:

* Target 1 + Target 2
* Multiple competing targets
* A list of possible targets

The system should select the single best-supported target.

---

## 8 AI WORKSPACES

Target Analyst must have 8 independently configurable AI workspaces.

### 1. TARGET STRUCTURE AI

Analyze:

* 1D structure
* 4H structure
* 1H structure
* 30M structure
* 15M structure
* 5M structure
* Trend
* Range
* Breakout
* Breakdown
* Structure transitions

Determine where the current structure provides support for a potential target.

---

### 2. TARGET LIQUIDITY AI

Analyze:

* Previous highs
* Previous lows
* Equal highs
* Equal lows
* Liquidity pools
* Liquidity sweeps
* Stop clusters
* Break-and-retest areas
* Likely liquidity destinations

Identify price areas that could reasonably become future destinations.

---

### 3. TARGET CANDLE AI

Analyze:

* Candle behavior
* Rejection
* Expansion
* Compression
* Engulfing
* Breakout candles
* Failed breakouts
* Momentum candles
* Exhaustion
* Multi-candle formations

Focus on what candle behavior suggests about potential future price movement.

---

### 4. TARGET FLOW AI

Analyze:

* Momentum
* Acceleration
* Deceleration
* Continuation
* Reversal behavior
* Absorption
* Exhaustion
* Expansion/contraction
* Short-term market flow

Determine whether current flow supports reaching a potential target.

---

### 5. TARGET KTR AI

Analyze:

* Key Trading Ranges
* Important price zones
* Session levels
* Previous-day levels
* Support/resistance
* Range boundaries
* Breakout/retest areas
* Relevant price clusters

Identify technically meaningful target areas.

---

### 6. TARGET NEWS AI

Analyze:

* Database news
* Online/recent news
* Upcoming events
* High-impact events
* Historical news
* News direction
* Market reaction
* Expected vs actual reaction
* USD/XAU-related information

News AI must distinguish:

```text
FACT
vs
ANALYST OPINION
vs
AI INTERPRETATION
```

---

### 7. TARGET RISK AI

Analyze:

* Target distance
* Volatility
* ATR
* Market conditions
* Invalidating conditions
* Risk/reward structure
* Target feasibility
* Excessive distance
* Conflicting evidence

Risk AI must be able to reject an unreasonable target.

---

### 8. TARGET MASTER AI

Receive the structured outputs from the seven specialist workspaces.

It must:

* Compare evidence
* Detect conflicts
* Weight stronger evidence
* Reject weak reasoning
* Consider uncertainty
* Determine the strongest target candidate
* Select exactly ONE target
* Produce structured reasoning
* Produce confidence
* Produce invalidation information

The Master AI must not simply vote by majority.

---

## AI PROVIDER CONFIGURATION

Every Target AI workspace must have its own independent configuration.

Support:

```text
Provider
API Key
Model
Base URL
Timeout
Max Tokens
Temperature
Enabled
```

Example:

```env
TARGET_STRUCTURE_AI_PROVIDER=
TARGET_STRUCTURE_AI_API_KEY=
TARGET_STRUCTURE_AI_MODEL=
TARGET_STRUCTURE_AI_BASE_URL=

TARGET_LIQUIDITY_AI_PROVIDER=
TARGET_LIQUIDITY_AI_API_KEY=
TARGET_LIQUIDITY_AI_MODEL=
TARGET_LIQUIDITY_AI_BASE_URL=

TARGET_CANDLE_AI_PROVIDER=
TARGET_CANDLE_AI_API_KEY=
TARGET_CANDLE_AI_MODEL=
TARGET_CANDLE_AI_BASE_URL=

TARGET_FLOW_AI_PROVIDER=
TARGET_FLOW_AI_API_KEY=
TARGET_FLOW_AI_MODEL=
TARGET_FLOW_AI_BASE_URL=

TARGET_KTR_AI_PROVIDER=
TARGET_KTR_AI_API_KEY=
TARGET_KTR_AI_MODEL=
TARGET_KTR_AI_BASE_URL=

TARGET_NEWS_AI_PROVIDER=
TARGET_NEWS_AI_API_KEY=
TARGET_NEWS_AI_MODEL=
TARGET_NEWS_AI_BASE_URL=

TARGET_RISK_AI_PROVIDER=
TARGET_RISK_AI_API_KEY=
TARGET_RISK_AI_MODEL=
TARGET_RISK_AI_BASE_URL=

TARGET_MASTER_AI_PROVIDER=
TARGET_MASTER_AI_API_KEY=
TARGET_MASTER_AI_MODEL=
TARGET_MASTER_AI_BASE_URL=
```

Do not hard-code a provider.

Do not use Full Analyst credentials as an automatic fallback.

---

## ANALYSIS SNAPSHOT

When the user clicks Target Analyst, immediately create an immutable analysis snapshot.

The snapshot must record:

* Symbol
* Current price
* Current time
* Timeframes
* Candle state
* Market-data versions
* News evidence
* Economic evidence
* Analyst evidence
* Evidence IDs
* Provider information
* Analysis configuration
* Prompt versions

The Target result must always be traceable to the evidence available at that moment.

---

## EVIDENCE COLLECTION

Target Analyst can use:

* AllTick market data
* Twelve Data historical/reference data
* Normalized candles
* Multi-timeframe data
* News database
* Recent online news
* Economic data
* Macro data
* Analyst information
* Local technical evidence
* Historical market context

Do not blindly send raw databases to AI.

Use the Phase 10/11 evidence foundation.

---

## LOCAL ANALYST INDEPENDENCE

The Target Analyst MUST NOT use the Local Analyst's final signal as an instruction.

Example:

```text
Local = BUY
Target = 4,190
```

Target AI must reach its target independently.

Likewise:

```text
Local = SELL
Target = 4,210
```

is technically allowed if the evidence supports it.

The systems must not be forced to agree.

---

## TARGET SELECTION

The Master AI should evaluate candidate price areas and select the strongest one.

Target selection should consider:

* Structure
* Liquidity
* KTR
* Candle evidence
* Flow
* News
* Macro
* Volatility
* Distance
* Invalidation
* Conflicts
* Confidence

The final result must contain exactly one target.

---

## TARGET RESULT

Successful analysis should produce:

```text
🎯 TARGET
XAUUSD
Target: 4,190.00

🛡 INVALIDATION
4,165.00

Confidence:
High / Medium / Low

Reason:
Short structured explanation

Valid Until:
Time-based validity
```

The exact UI can be refined during Phase 20.

---

## TARGET RESULT PANEL

While analysis is running:

```text
┌──────────────────────────────┐
│ TARGET ANALYST               │
│                              │
│ 🔄 Getting signal candle...  │
│ 🔄 Collecting evidence...    │
│ 🔄 Running AI workspaces...  │
│                              │
│          [ CANCEL ]          │
└──────────────────────────────┘
```

Do not show the final target before successful completion.

---

## SUCCESS STATE

After successful analysis:

```text
┌──────────────────────────────┐
│ TARGET ANALYST · XAUUSD      │
│                              │
│ 🎯 TARGET                    │
│ 4,190.00                     │
│                              │
│ 🛡 INVALIDATION              │
│ 4,165.00                     │
│                              │
│ Reason                       │
│ ...                          │
│                              │
│ Valid Until                  │
│ ...                          │
│                              │
│      [ CANCEL TARGET ]       │
└──────────────────────────────┘
```

---

## CANCEL BEHAVIOR

The user must be able to cancel the active Target result.

When cancellation succeeds:

```text
CANCEL TARGET
↓
Stop lifecycle monitoring
↓
Remove active chart target
↓
Remove active result panel
↓
Clear active Target state
↓
Return to [ANALYZE]
```

Historical analysis data must remain stored.

Cancellation must not delete historical records.

---

## TARGET CHART OBJECT

The successful Target Analyst should render one target object on the market chart.

Example:

```text
Current Price
     │
     │
     │
     └──────────────── 🎯 Target
```

The target must be visually distinct from:

* 🟢 Local BUY
* 🔴 Local SELL
* 🟡 Local STOP
* 🔮 Full Analyst Future

Do not place target objects under every candle.

---

## TARGET VALIDITY

Every target must have a validity period.

Examples:

* Short-term target
* Medium short-term target

The validity duration must be determined from the analysis context and configurable.

A target must never remain active forever.

---

## TARGET LIFECYCLE

```text
ANALYZING
   ↓
SUCCESS
   ↓
ACTIVE
   ↓
┌─────────────┬─────────────┬─────────────┐
↓             ↓             ↓
TARGET HIT   INVALIDATED   TIME EXPIRED
↓             ↓             ↓
REMOVE       REMOVE        REMOVE
```

The user can also manually cancel:

```text
ACTIVE
  ↓
CANCEL TARGET
  ↓
REMOVE
```

---

## TARGET INVALIDATION

A target becomes invalid when the conditions supporting it are no longer valid.

Invalidation can consider:

* Structure failure
* Liquidity failure
* Major market reversal
* News shock
* Price violation
* Risk condition
* Analysis validity expiration

Do not keep a target active simply because its original prediction has not been reached.

---

## NEWS AND MACRO

Target Analyst can perform deeper research because its purpose is predictive rather than continuous real-time decision-making.

It may use:

* Historical news
* Current news
* Online news
* Economic calendar
* Macro conditions
* Previous market reactions
* Analyst opinions

All evidence must remain timestamped.

---

## AI COST / TOKEN MANAGEMENT

Use the AI providers efficiently.

Before each workspace call:

* Remove duplicate evidence.
* Compress repetitive candles.
* Select relevant timeframes.
* Summarize historical context.
* Avoid sending unchanged data repeatedly.
* Limit unnecessary news articles.
* Use structured outputs.

However, Target Analyst may perform deeper research than the real-time Full Analyst because it is a user-triggered analysis rather than a continuously reactive system.

---

## FAILURE HANDLING

If one specialist fails:

* Record the failure.
* Do not fabricate its output.
* Continue only if the remaining evidence is sufficient.
* Mark missing evidence.
* Reduce confidence when appropriate.

If Master AI fails:

```text
ANALYSIS FAILED
```

Do not create a target.

---

## TESTING

Test:

1. Target analysis creation
2. Analysis snapshot
3. Evidence collection
4. Evidence timestamp protection
5. All 8 AI workspaces
6. Independent provider configuration
7. Specialist conflicts
8. Master synthesis
9. One-target enforcement
10. Invalid target rejection
11. Target result persistence
12. Target chart rendering
13. Cancel behavior
14. Target hit detection
15. Target invalidation
16. Target expiration
17. Provider failure
18. AI timeout
19. Invalid AI response
20. Historical reproducibility

---

## PHASE 13 BOUNDARY

Phase 13 produces:

🎯 ONE TARGET

It does NOT produce:

* Local BUY/SELL/STOP signals
* Full Analyst BUY/SELL/WAIT decisions
* 🔮 Future scenarios
* Trade execution
* Multiple competing targets

The Target Analyst is an independent predictive target system.

---

## DELIVERABLES

1. Target Analyst Engine
2. Analysis Snapshot
3. Target evidence collector
4. Target Structure AI
5. Target Liquidity AI
6. Target Candle AI
7. Target Flow AI
8. Target KTR AI
9. Target News AI
10. Target Risk AI
11. Target Master AI
12. Independent provider configuration
13. Target synthesis
14. One-target validator
15. Target result model
16. Target result panel
17. Target chart object
18. Target lifecycle manager
19. Target cancellation
20. Target hit detection
21. Target invalidation
22. Target expiration
23. Historical target storage
24. Comprehensive testing

---

## COMPLETION CRITERIA

Phase 13 is complete when:

* [TARGET ANALYST] starts a new analysis.
* Each click creates one independent analysis job.
* An immutable analysis snapshot is created.
* Relevant evidence is collected and validated.
* All 8 AI workspaces can operate independently.
* Each workspace can use its own provider/model/API configuration.
* Specialist conflicts are handled.
* Master AI selects exactly one target.
* No target is created when evidence is insufficient.
* The final target is traceable to its evidence.
* The target appears on the chart.
* The target result panel appears only after successful analysis.
* The panel contains a Cancel Target action.
* Cancellation removes the active target but preserves history.
* Targets can be hit, invalidated, or expired.
* Targets cannot run forever.
* Target Analyst remains independent from Local Analyst and Full Analyst.
* No final BUY/SELL/WAIT decision is generated by Target Analyst.
