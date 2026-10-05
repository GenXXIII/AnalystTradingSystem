# PHASE 12 — LOCAL ANALYST & SIGNAL ENGINE

## OBJECTIVE

Build the fully local, deterministic market-analysis engine that continuously observes XAUUSD market data and produces fast local signals without using AI.

The Local Analyst must be independent from both:

* 🎯 Target Analyst
* 🔮 Full Analyst

It must never consume their conclusions as input.

---

## CORE FLOW

AllTick + Twelve Data
↓
Data Normalizer
↓
Candle Builder
↓
Multi-Timeframe Market Data
↓
Local Evidence Engine
↓
Technical Calculations
↓
Local Signal Rules
↓
Signal Validator
↓
🟢 BUY / 🔴 SELL / 🟡 STOP / NOTHING

---

## DATA TIMEFRAMES

Support:

* 1M
* 5M
* 15M
* 30M
* 1H
* 4H
* 1D

The Local Analyst must be able to use higher timeframes for context while maintaining fast lower-timeframe signal detection.

Primary short-term signal timeframes should support:

* 5M
* 15M
* 30M

---

## LOCAL ANALYSIS COMPONENTS

Build deterministic local calculations for:

### Market Structure

* Higher High
* Higher Low
* Lower High
* Lower Low
* Trend
* Range
* Transition
* Breakout
* Breakdown
* Structure break
* Failed breakout

### Liquidity

* Previous highs/lows
* Equal highs
* Equal lows
* Liquidity zones
* Liquidity sweeps
* Rejection after sweep
* Break-and-retest areas

### Candle Analysis

Detect configurable candle conditions such as:

* Strong bullish candle
* Strong bearish candle
* Rejection
* Engulfing
* Expansion
* Compression
* Breakout candle
* Failed breakout
* Momentum weakening
* Indecision

Do not treat one candle pattern as a guaranteed signal.

### Momentum / Flow

Calculate local market behavior such as:

* Momentum
* Expansion
* Compression
* Directional strength
* Acceleration
* Deceleration
* Possible exhaustion
* Continuation behavior
* Reversal behavior

### KTR / Important Levels

Calculate and track:

* Key Trading Ranges
* Important price levels
* Session highs/lows
* Previous day levels
* Previous session levels
* Relevant support/resistance
* Range boundaries

### Volatility

Support:

* ATR
* Candle range
* Average movement
* Volatility expansion
* Volatility contraction

---

## LOCAL SIGNAL ENGINE

The Local Signal Engine must combine deterministic evidence.

Example:

Structure
+
Liquidity
+
Candle
+
Flow
+
KTR
+
Momentum
+
Volatility
↓
Signal Score
↓
Signal Validation
↓
Signal State

The scoring system must be configurable.

Do not hard-code a single BUY/SELL formula that cannot be adjusted.

---

## SIGNAL STATES

Supported states:

### NOTHING

No valid local setup exists.

Do not render a chart object.

### 🟢 BUY

A valid local bullish setup exists.

### 🔴 SELL

A valid local bearish setup exists.

### 🟡 STOP

The previously detected local setup is no longer valid and the local engine must stop displaying it.

STOP is a local-state concept only.

It is NOT:

* WAIT
* AI decision
* Full Analyst result
* Target result

---

## CHART SIGNAL OBJECTS

The Local Analyst should not place an object under every candle.

Only render an object when a valid local signal is detected.

Example:

```text
Candle 1   Candle 2   Candle 3   Candle 4   Candle 5
   │          │          │          │          │
   │          │       🟢 BUY       │          │
   │          │          │          │          │
```

The signal object must be attached to the relevant signal candle.

Do not clutter the chart with objects for candles that have no valid signal.

---

## SIGNAL LIFECYCLE

```text
NO SIGNAL
   ↓
SIGNAL DETECTED
   ↓
🟢 BUY / 🔴 SELL
   ↓
MONITOR
   ↓
┌─────────────┬─────────────┬─────────────┐
↓             ↓             ↓
VALID         INVALID       TIME EXPIRED
↓             ↓             ↓
CONTINUE      🟡 STOP       🟡 STOP
```

A local signal must not remain active forever.

---

## LOCAL SIGNAL EXPIRATION

Every local signal must have a configurable validity period.

Examples:

* 5M signal → short validity
* 15M signal → short/medium validity
* 30M signal → longer validity

The exact duration must be configurable by timeframe and strategy.

When the validity period expires:

```text
Active Signal
↓
Time Expired
↓
🟡 STOP
```

---

## PRICE INVALIDATION

A BUY signal must be invalidated when the market violates its defined bullish conditions.

A SELL signal must be invalidated when the market violates its defined bearish conditions.

The invalidation logic must be based on the actual setup conditions rather than an arbitrary fixed number.

---

## SIGNAL CONFIRMATION

Avoid creating a signal from one weak condition.

The engine should support configurable confirmation requirements such as:

* Minimum evidence score
* Structure alignment
* Liquidity condition
* Candle confirmation
* Flow confirmation
* Volatility condition

If evidence is insufficient:

```text
NO SIGNAL
```

Do not force BUY or SELL.

---

## REAL-TIME PROCESSING

The Local Analyst must react to newly available market/candle data efficiently.

Do not recalculate unnecessary historical information on every update.

Use:

* Incremental calculations
* Cached indicators
* Cached structure
* Cached liquidity
* Previous signal state
* Only-new-candle processing where possible

---

## LOCAL ANALYST INDEPENDENCE

The Local Analyst MUST NOT:

* Call Target Analyst
* Call Full Analyst
* Read Target conclusions
* Read Full conclusions
* Use AI
* Inherit AI BUY/SELL decisions
* Change its signal because Full Analyst disagrees
* Change its signal because Target Analyst disagrees

Example:

```text
LOCAL  → BUY
TARGET → 4,190 target
FULL   → WAIT
```

This is valid.

Each system remains independent.

---

## SIGNAL DATA MODEL

Create a structured LocalSignal model containing at minimum:

* Id
* Symbol
* Timeframe
* SignalType
* SignalCandleTime
* SignalPrice
* CurrentPrice
* Direction
* Score
* Confidence
* StructureState
* LiquidityState
* CandleState
* FlowState
* KTRState
* VolatilityState
* InvalidationPrice
* ValidUntil
* Status
* CreatedAt
* UpdatedAt
* InvalidatedAt
* ExpiredAt

Do not use database IDs as chart/public signal identifiers.

---

## CHART OBJECT DATA

The chart object should contain enough information to render:

* Signal type
* Signal candle
* Price
* Time
* Timeframe
* Status
* Invalidation
* Valid-until information

The chart layer must not contain analysis logic.

---

## UI REQUIREMENTS

The Local Analyst should remain lightweight.

The chart should show:

```text
🟢 BUY
```

or

```text
🔴 SELL
```

under the relevant candle.

When invalidated or expired:

```text
🟡 STOP
```

The UI must not create unnecessary visual clutter.

---

## LOCAL SIGNAL HISTORY

Store historical local signals for:

* Backtesting
* Accuracy measurement
* Debugging
* Performance analysis
* Strategy improvement

Do not delete historical signals when they expire.

Only remove the active chart representation when the lifecycle requires it.

---

## PERFORMANCE REQUIREMENTS

The Local Analyst must be designed for continuous operation.

Avoid:

* AI calls
* Heavy database queries on every tick
* Rebuilding all timeframes unnecessarily
* Recalculating unchanged indicators
* Reprocessing unchanged candles

Use cached/incremental processing where possible.

---

## ERROR HANDLING

Handle:

* Missing candles
* Duplicate candles
* Out-of-order candles
* Provider gaps
* Invalid prices
* Invalid timestamps
* Timeframe synchronization problems
* Data-source disagreement

If market data is unreliable:

```text
NO SIGNAL
```

Do not generate a false signal from corrupted data.

---

## TESTING

Test:

1. BUY detection
2. SELL detection
3. No-signal conditions
4. Signal invalidation
5. Signal expiration
6. STOP state
7. Candle attachment
8. Multi-timeframe calculations
9. Duplicate data
10. Missing data
11. Out-of-order data
12. Provider switching
13. Real-time updates
14. Historical replay
15. Signal persistence
16. Chart-object lifecycle
17. Performance under continuous updates

---

## PHASE 12 BOUNDARY

Phase 12 produces:

```text
🟢 BUY
🔴 SELL
🟡 STOP
NOTHING
```

Phase 12 does NOT produce:

* 🎯 Target Analyst results
* 🔮 Full Analyst results
* AI analysis
* BUY/SELL decisions from AI
* Future scenarios
* Trade execution

The Local Analyst is the independent, fast, deterministic signal layer.

---

## DELIVERABLES

1. Local Analysis Engine
2. Multi-timeframe local analysis
3. Structure analyzer
4. Liquidity analyzer
5. Candle analyzer
6. Flow/momentum analyzer
7. KTR analyzer
8. Volatility analyzer
9. Local signal scoring
10. Signal validator
11. Signal lifecycle manager
12. Signal expiration
13. Signal invalidation
14. STOP state
15. Chart signal-object integration
16. Local signal persistence
17. Incremental processing
18. Local signal history
19. Comprehensive tests
20. Performance optimization

---

## COMPLETION CRITERIA

Phase 12 is complete when:

* Local analysis runs without AI.
* Local analysis runs independently from Target and Full.
* Multi-timeframe data is available.
* Valid BUY/SELL setups can be detected.
* Weak conditions produce no signal.
* Signals are attached to the correct candle.
* The chart does not display an object under every candle.
* Invalid signals become 🟡 STOP.
* Expired signals become 🟡 STOP.
* Signals cannot run forever.
* Historical signals remain available for testing.
* The engine handles missing/invalid market data safely.
* Real-time processing is efficient.
* Target and Full Analyst conclusions cannot influence Local signals.
