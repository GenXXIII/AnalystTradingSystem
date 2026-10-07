============================================================
PHASE 12 — LOCAL ANALYST & SIGNAL ENGINE
============================================================

Project: AnalystTradingSystem
Phase: 12

IMPORTANT ARCHITECTURE RULE
------------------------------------------------------------

This phase implements the LOCAL ANALYST.

The Local Analyst is:

- deterministic
- rule-based
- continuously running
- AI-free
- explainable
- configurable
- independent from AI conclusions

The Local Analyst must NOT depend on:

- Target Analyst
- Full Analyst
- Future / Expected Path
- AI workspace conclusions
- Telegram Scanner signals
- automatic trade execution

The Local Analyst may consume the project's normalized market
data and technical evidence infrastructure, but its decision
must be calculated independently.


============================================================
1. OBJECTIVE
============================================================

Build a deterministic XAUUSD Local Analyst that continuously
analyzes market data and produces local technical setups.

Possible states:

NOTHING
🟢 BUY
🔴 SELL
🟡 STOP

The Local Analyst must not force a signal.

If conditions are insufficient:

→ NOTHING

If an existing setup becomes invalid:

→ 🟡 STOP


============================================================
2. CORE FLOW
============================================================

XAUUSD Market Data
        ↓
Data Validation
        ↓
Multi-Timeframe Candles
        ↓
Technical Calculations
        ↓
Market Structure
        ↓
Liquidity
        ↓
Candle Analysis
        ↓
Momentum / Flow
        ↓
KTR / Important Levels
        ↓
Volatility
        ↓
Rule Evaluation
        ↓
Score / Confirmation
        ↓
Local Signal
        ↓
Lifecycle Monitoring


============================================================
3. SUPPORTED TIMEFRAMES
============================================================

The Local Analyst must support:

1M
5M
15M
30M
1H
4H
1D

The implementation must be configurable so that individual
timeframes can be enabled or disabled.

The system must understand relationships between timeframes.

Example:

1D → macro structure
4H → major structure
1H → intermediate structure
15M → setup context
5M → local confirmation
1M → micro behavior


============================================================
4. DETERMINISTIC ONLY
============================================================

No AI calls are allowed for Local Analyst decisions.

Do not call:

- OpenAI
- Target AI
- Full Analyst AI
- Master AI
- external LLM
- AI-generated signal

The same market-data snapshot and same configuration must
produce the same result.

Example:

Same candles
+
Same configuration
=
Same signal


============================================================
5. MARKET STRUCTURE
============================================================

Implement deterministic structure detection.

Support:

- swing highs
- swing lows
- HH
- HL
- LH
- LL
- Break of Structure
- Change of Character
- trend
- range
- transition
- structural invalidation

Structure detection must use configurable parameters.

Do not hardcode arbitrary thresholds throughout the code.


============================================================
6. TECHNICAL INDICATORS
============================================================

Support at minimum:

- EMA 20
- EMA 50
- EMA 200
- RSI
- ATR

Indicator periods and thresholds must be configurable.

Indicators must be calculated locally from validated candle
data.

Do not request an AI interpretation for indicator values.


============================================================
7. LIQUIDITY
============================================================

Detect deterministic liquidity-related structures such as:

- equal highs
- equal lows
- previous swing highs
- previous swing lows
- session highs/lows where available
- liquidity sweep
- rejection after sweep

Liquidity detection must be based on explicit rules.

Do not claim actual order-book liquidity when order-book data
is unavailable.


============================================================
8. SUPPORT / RESISTANCE
============================================================

Detect relevant levels using deterministic methods such as:

- swing highs
- swing lows
- previous highs/lows
- repeated reaction levels
- breakout/retest levels
- configured session levels
- important KTR levels

Levels should have:

- price
- source
- timeframe
- strength
- created time
- status

Avoid creating excessive duplicate levels.


============================================================
9. CANDLE ANALYSIS
============================================================

Implement deterministic candle classification.

Possible patterns:

- bullish rejection
- bearish rejection
- bullish engulfing
- bearish engulfing
- momentum candle
- indecision
- breakout candle
- failed breakout
- continuation candle

Patterns must be evaluated in context.

Do not treat a single candle pattern as sufficient for a
signal by itself.


============================================================
10. MOMENTUM / FLOW
============================================================

Calculate deterministic momentum information using available
market data.

Consider:

- price momentum
- candle momentum
- directional movement
- acceleration
- deceleration
- ATR expansion/contraction
- volume/tick-volume when available

Never fabricate unavailable order-flow information.


============================================================
11. KTR / IMPORTANT LEVELS
============================================================

Integrate the project's existing KTR and important-level
calculations.

The Local Analyst may use:

- KTR
- previous session levels
- previous highs/lows
- important price levels
- volatility-adjusted levels

All calculations must use the project's established
definitions.


============================================================
12. SIGNAL SCORING
============================================================

Implement configurable deterministic scoring.

Example LONG conditions:

1. Price > EMA200
2. EMA20 > EMA50
3. Bullish market structure
4. RSI confirmation
5. Support / resistance retest
6. Bullish candle / momentum confirmation

Example SHORT conditions:

1. Price < EMA200
2. EMA20 < EMA50
3. Bearish market structure
4. RSI confirmation
5. Resistance / support retest
6. Bearish candle / momentum confirmation

Each condition contributes to a configurable score.

Example:

5 / 6 conditions
→ potential valid setup

The exact threshold must be configurable.

Do not hardcode 5/6 as an immutable rule.


============================================================
13. CONFIRMATION
============================================================

A signal should require appropriate confirmation.

Possible confirmation methods:

- candle close
- breakout of confirmation candle
- retest
- structure confirmation
- momentum confirmation

Do not trigger signals from incomplete candles unless the
configuration explicitly allows it.

Prefer confirmed candle-close conditions to reduce false
signals.


============================================================
14. SIGNAL STATES
============================================================

Supported states:

NOTHING
🟢 BUY
🔴 SELL
🟡 STOP

Meaning:

NOTHING
→ no active valid setup.

🟢 BUY
→ active bullish local setup.

🔴 SELL
→ active bearish local setup.

🟡 STOP
→ an existing local setup has ended or become invalid.


============================================================
15. SIGNAL OBJECT
============================================================

Create a structured LocalSignal model.

Suggested fields:

LocalSignal
├── Id
├── Symbol
├── Timeframe
├── SignalCandleId
├── SignalCandleTime
├── Direction
├── SignalPrice
├── Score
├── MaxScore
├── Confidence
├── StructureState
├── LiquidityState
├── CandleState
├── MomentumState
├── KtrState
├── VolatilityState
├── InvalidationReason
├── ValidUntil
├── Status
├── CreatedAt
├── UpdatedAt
└── EndedAt

Do not treat database IDs as market signal identifiers.


============================================================
16. SIGNAL CANDLE AND LIFECYCLE
============================================================

A signal belongs to the relevant setup/origin candle.

Do NOT create a new independent signal object for every
subsequent candle.

Example:

🟢 BUY
───────────────
          │
          │ monitoring
          │
          ▼
      🟡 STOP

The BUY setup remains active while its conditions remain valid.

New candles update the existing setup rather than creating
duplicate BUY signals.


============================================================
17. SIGNAL LIFECYCLE
============================================================

Lifecycle:

NO SIGNAL
    ↓
SETUP DETECTED
    ↓
🟢 BUY / 🔴 SELL
    ↓
ACTIVE
    ↓
Monitor conditions
    ↓
┌──────────────┬──────────────┬──────────────┐
│              │              │
▼              ▼              ▼
TP/Target    Invalidated   Time Expired
│              │              │
└──────────────┴──────────────┘
               ↓
            🟡 STOP
               ↓
        Historical Record


============================================================
18. INVALIDATION
============================================================

Invalidation must be based on actual setup conditions.

Examples:

- structural invalidation
- price breaks invalidation level
- opposite structure confirmation
- setup conditions disappear
- configured validity expires
- abnormal data condition

Do not use random fixed invalidation rules.

Every STOP should have a reason.


============================================================
19. VALIDITY
============================================================

Signal validity must be configurable by timeframe.

Example:

1M → short validity
5M → short validity
15M → medium validity
1H → longer validity

Do not hardcode a universal expiration time for every
timeframe.

Store:

ValidUntil

and evaluate it deterministically.


============================================================
20. ACTIVE VS HISTORY
============================================================

Active signals should be visible in the active-analysis
state.

When a signal ends:

- remove it from active signal state
- preserve its historical record
- preserve lifecycle events
- preserve reason for termination

Do not delete completed signal history merely because it is
no longer active.


============================================================
21. LOCAL ANALYST UI DATA
============================================================

The Local Analyst should expose structured information for
the UI.

Example:

┌──────────────────────────────────────┐
│ LOCAL ANALYST            XAUUSD · 5M │
│                                      │
│ 🟢 BUY                               │
│ Score: 5 / 6                         │
│                                      │
│ Structure: Bullish                   │
│ Liquidity: Support retest            │
│ Momentum: Bullish                     │
│ Candle: Confirmation                 │
│                                      │
│ Signal: 4,xxx.xx                     │
│ Valid Until: ...                     │
└──────────────────────────────────────┘

When invalidated:

┌──────────────────────────────────────┐
│ LOCAL ANALYST            XAUUSD · 5M │
│                                      │
│ 🟡 STOP                              │
│                                      │
│ Reason: Structural invalidation      │
└──────────────────────────────────────┘

The exact UI belongs to the frontend implementation.


============================================================
22. INDEPENDENCE FROM TARGET ANALYST
============================================================

Target Analyst may produce:

🎯 Target

Local Analyst must NOT use that target.

Example:

Local:
🟢 BUY

Target:
🎯 4,190

The Local BUY was calculated without knowing the Target result.


============================================================
23. INDEPENDENCE FROM FULL ANALYST
============================================================

Full Analyst may produce:

🔴 SELL

Local Analyst may simultaneously produce:

🟢 BUY

This is valid.

Do not override Local Analyst because Full Analyst disagrees.

Do not feed Full Analyst conclusions into Local Analyst.


============================================================
24. INDEPENDENCE FROM TELEGRAM SCANNER
============================================================

The 24/7 Telegram Scanner is a separate project.

It must NOT provide signals to Local Analyst.

Local Analyst must NOT provide signals to Telegram Scanner.

They may use compatible market-data concepts, but their
decision engines remain independent.

Example:

Local:
🟢 BUY

Telegram:
🔴 SELL

This must be allowed.


============================================================
25. DATA QUALITY
============================================================

Handle:

- missing candles
- duplicate candles
- out-of-order candles
- stale data
- incomplete candles
- provider interruptions
- invalid OHLC values
- timeframe gaps

Never generate a signal from invalid market data.

If required data is unavailable:

→ NOTHING

and record the data-quality reason.


============================================================
26. INCREMENTAL PROCESSING
============================================================

Do not recalculate the entire historical dataset every time a
new candle arrives.

Use:

- incremental calculations
- cached indicators
- cached structure
- cached levels
- changed-candle detection
- stateful signal monitoring

Only recompute what is necessary.


============================================================
27. CONFIGURATION
============================================================

Signal parameters must be configurable.

Examples:

- EMA periods
- RSI period
- RSI thresholds
- ATR period
- swing detection
- structure thresholds
- scoring weights
- minimum score
- confirmation rules
- validity duration
- invalidation buffers
- volatility filters

Avoid scattering constants throughout the code.

Centralize configuration.


============================================================
28. EXPLAINABILITY
============================================================

Every signal must be explainable.

Store the conditions that caused the signal.

Example:

BUY because:

✓ Price > EMA200
✓ EMA20 > EMA50
✓ Bullish HH/HL structure
✓ Support retest
✓ Bullish confirmation candle

Score:

5 / 6

The explanation must come from actual deterministic
conditions, not generated AI text.


============================================================
29. TESTING
============================================================

Test at minimum:

- indicator calculations
- EMA
- RSI
- ATR
- swing detection
- HH/HL/LH/LL
- BOS
- CHoCH
- liquidity detection
- support/resistance
- candle detection
- scoring
- BUY conditions
- SELL conditions
- confirmation
- invalidation
- STOP
- expiration
- signal lifecycle
- duplicate prevention
- missing data
- out-of-order data
- incomplete candles
- multi-timeframe relationships
- incremental processing

Use deterministic test datasets.

The same input must produce the same result.


============================================================
30. PERFORMANCE
============================================================

The Local Analyst is intended for continuous monitoring.

Optimize for:

- low CPU usage
- low memory usage
- incremental processing
- minimal recalculation
- fast signal evaluation
- stable long-running operation

Do not introduce AI calls or unnecessary external API calls
into the Local Analyst.


============================================================
31. COMPLETION CRITERIA
============================================================

Phase 12 is complete when:

1. Local Analyst runs without AI.
2. XAUUSD multi-timeframe data is consumed correctly.
3. Indicators are calculated deterministically.
4. Market structure is detected.
5. Liquidity is detected.
6. Support/resistance is detected.
7. Candle conditions are detected.
8. Momentum/flow conditions are calculated.
9. KTR/important levels are integrated.
10. Configurable scoring works.
11. BUY signals work.
12. SELL signals work.
13. NOTHING state works.
14. STOP state works.
15. Signal lifecycle works.
16. Signal history is preserved.
17. Duplicate signals are prevented.
18. Data-quality failures are handled.
19. Incremental processing works.
20. Tests pass.
21. Performance is acceptable for continuous monitoring.
22. Local Analyst remains completely independent from
    Target Analyst, Full Analyst, Future, and Telegram Scanner.


============================================================
32. NEXT PHASE
============================================================

After Phase 12 is fully implemented and tested:

→ Phase 13 — Target Analyst & AI Workspaces

Target Analyst will be a separate AI-driven predictive
target system.

It must not change the Local Analyst's decisions.


============================================================
END OF PHASE 12
============================================================