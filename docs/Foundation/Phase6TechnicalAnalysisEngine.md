# PHASE 6 — TECHNICAL ANALYSIS ENGINE

Continue building my XAUUSD AI Trading Intelligence System.

Completed:

* Phase 1 — Project Foundation
* Phase 2 — Configuration & Secrets
* Phase 3 — Database Foundation
* Phase 4 — MT5 Integration
* Phase 5 — Market Data Pipeline

Now implement:

# PHASE 6 — TECHNICAL ANALYSIS ENGINE

The purpose of this phase is to build a **completely application-owned technical analysis engine** using the normalized market data produced by Phase 5.

External providers such as MT5 are data sources only.

The technical-analysis logic must belong to our application.

Do NOT call OpenAI in this phase.

Do NOT build trading signals yet.

Do NOT build AI reasoning yet.

Do NOT place trades.

---

# 1. CORE PRINCIPLE

The architecture must be:

```text
MT5
  ↓
Phase 5 Market Data Pipeline
  ↓
Normalized Market Data
  ↓
YOUR Technical Analysis Engine
  ↓
Technical Analysis Results
```

NOT:

```text
MT5
  ↓
MT5 indicator API
  ↓
Our application
```

We are building our own analysis layer.

External libraries may be used for low-level mathematical calculations if they are reliable and maintainable, but the application's analysis models, rules, interfaces, and interpretation must remain ours.

Do not make the application dependent on a third-party indicator API.

---

# 2. RESPONSIBILITY

The Technical Analysis Engine should answer questions such as:

```text
What is the current trend?

What is momentum doing?

Where are important support/resistance areas?

What is volatility doing?

Are there notable candlestick patterns?

What is the market structure?

What does each timeframe show?

Are different timeframes aligned or conflicting?
```

It must return structured data.

Do not return only strings such as:

```text
"Market looks bullish."
```

Instead return structured analytical results that later phases can interpret.

---

# 3. TIMEFRAMES

Use the market-data timeframes established in Phase 5:

```text
M1
M5
M15
M30
H1
H4
D1
```

The engine must support analysis independently for each timeframe.

Example:

```text
XAUUSD
 ├── M5 analysis
 ├── M15 analysis
 ├── M30 analysis
 ├── H1 analysis
 ├── H4 analysis
 └── D1 analysis
```

Do not assume every strategy should operate on every timeframe.

The strategy layer will decide that later.

---

# 4. DATA SOURCE

Technical analysis must read from the Phase 5 normalized market-data layer.

Do not directly retrieve candles from MT5 inside indicator classes.

Correct:

```text
TechnicalAnalysisService
        ↓
MarketDataRepository/Service
        ↓
SQL Server
```

Incorrect:

```text
RSIService
   ↓
MT5
```

Keep the architecture separated.

---

# 5. INDICATORS

Implement the core indicators needed for a serious XAUUSD analysis system.

At minimum implement:

## Trend

* SMA
* EMA
* Multiple EMA relationships
* Price vs EMA
* EMA slope
* Trend direction

Suggested periods:

```text
EMA 9
EMA 20
EMA 50
EMA 100
EMA 200
```

Do not assume every period must be used by every strategy.

Make indicator periods configurable.

---

# 6. MOMENTUM

Implement:

### RSI

Support configurable periods.

Default:

```text
RSI 14
```

Return:

```text
value
zone
momentum interpretation
```

For example, the engine may classify:

```text
Oversold
Neutral
Overbought
```

But do not treat:

```text
RSI > 70
```

as an automatic sell signal.

Technical indicators are evidence, not trading decisions.

---

# 7. MACD

Implement:

```text
MACD
Signal
Histogram
```

Use configurable parameters.

Default conventional configuration may be:

```text
Fast = 12
Slow = 26
Signal = 9
```

Return structured information about:

* MACD value
* signal value
* histogram
* crossover
* momentum direction

Do not automatically convert MACD into Buy/Sell.

---

# 8. ATR

Implement:

```text
ATR
```

Default:

```text
ATR 14
```

Use it later for:

* volatility analysis
* dynamic market conditions
* strategy rules
* risk calculations

Do not implement position sizing or trading execution in this phase.

---

# 9. ADX

Implement:

```text
ADX
+DI
-DI
```

Default:

```text
ADX 14
```

Use it to help describe:

```text
trend strength
directional bias
```

Do not automatically interpret high ADX as bullish or bearish.

ADX measures trend strength, not direction by itself.

---

# 10. BOLLINGER BANDS

Implement:

```text
Middle
Upper
Lower
Width
%B
```

Use configurable parameters.

Default:

```text
Period = 20
Standard deviation = 2
```

Return structured information.

Do not create automatic buy/sell rules.

---

# 11. STOCHASTIC

Implement:

```text
%K
%D
```

Use configurable settings.

Return:

* values
* crossover
* zone

Again:

Do not convert indicator conditions directly into trading signals.

---

# 12. VWAP / VOLUME CONSIDERATION

Investigate the meaning and limitations of MT5 volume data before using volume-based indicators.

Distinguish between:

```text
tick volume
real volume
```

Do not pretend tick volume is exchange-traded volume.

If VWAP is implemented, clearly document the data assumptions.

If the available XAUUSD broker data is insufficient for a meaningful volume interpretation, do not fabricate precision.

---

# 13. CANDLESTICK ANALYSIS

Build a dedicated candlestick-analysis component.

Do NOT mix candlestick logic into RSI/EMA classes.

Detect important patterns such as:

```text
Doji
Hammer
Inverted Hammer
Shooting Star
Hanging Man
Bullish Engulfing
Bearish Engulfing
Morning Star
Evening Star
Inside Bar
Outside Bar
```

Add additional patterns only when they provide genuine analytical value.

For every detected pattern return structured information:

```text
Pattern
Direction
Timeframe
CandleTime
Strength/quality metrics
Supporting conditions
```

Do not automatically create a trade signal.

---

# 14. CANDLE QUALITY

Do not simply detect a candlestick pattern from names.

Calculate useful candle characteristics:

```text
Body
Upper wick
Lower wick
Range
Body-to-range ratio
Wick-to-body ratio
Relative range
```

This allows later strategies to distinguish a meaningful pattern from a weak one.

---

# 15. MARKET STRUCTURE

Build a dedicated market-structure analyzer.

Identify meaningful:

```text
Swing High
Swing Low
Higher High
Higher Low
Lower High
Lower Low
```

Use configurable swing detection rules.

Avoid extremely sensitive logic that marks every tiny fluctuation as a major swing.

The goal is useful market structure, not noise.

---

# 16. TREND STRUCTURE

Combine price structure with trend indicators.

Return structured information such as:

```text
TrendDirection
TrendStrength
Structure
PriceVsEMA
EMAAlignment
```

Possible descriptive states:

```text
Bullish
Bearish
Neutral
Transition
Conflicting
```

These are analytical classifications, NOT trading recommendations.

---

# 17. SUPPORT AND RESISTANCE

Create a dedicated Support/Resistance analyzer.

Use evidence such as:

```text
Swing highs
Swing lows
Repeated reactions
Previous highs/lows
Important price areas
```

Do NOT create thousands of meaningless levels.

Levels should be grouped into meaningful zones where appropriate.

Return:

```text
Zone
Type
Strength
Touches
Timeframes
DistanceFromPrice
```

Do not automatically say:

```text
"BUY HERE"
```

---

# 18. PRICE ACTION

Create a price-action analysis layer.

Analyze:

```text
Recent candle behavior
Range expansion
Range contraction
Breakout behavior
Rejection
Momentum candles
Inside ranges
Consolidation
```

Keep this separate from individual candlestick-pattern detection.

---

# 19. VOLATILITY REGIME

Build a volatility analyzer using data such as:

```text
ATR
ATR relative to historical ATR
Candle range
Bollinger Band width
Range expansion/contraction
```

Classify market conditions descriptively, for example:

```text
Very Low
Low
Normal
High
Very High
```

The thresholds must be configurable and documented.

Do not claim these classifications predict future movement.

---

# 20. MULTI-TIMEFRAME ANALYSIS

Build a multi-timeframe analyzer.

Example:

```text
D1
 ↓
H4
 ↓
H1
 ↓
M30
 ↓
M15
 ↓
M5
```

The engine should compare:

```text
Trend
Structure
Momentum
Volatility
Support/Resistance
```

between timeframes.

Example structured output:

```text
D1: Bullish
H4: Bullish
H1: Bullish
M15: Pullback
M5: Bearish short-term
```

This does NOT mean:

```text
BUY
```

It means the system has identified timeframe relationships.

---

# 21. CONFLUENCE ENGINE

Create a technical-confluence layer.

It should combine independent technical observations.

Example:

```text
EMA trend
+
Market structure
+
RSI
+
MACD
+
Support/Resistance
+
Candlestick
+
Volatility
+
Multi-timeframe alignment
```

Return something like:

```text
TechnicalConfluence
-------------------
TrendEvidence
MomentumEvidence
StructureEvidence
LevelEvidence
PatternEvidence
VolatilityEvidence
TimeframeAlignment
Conflicts
```

Do not create a final trading signal here.

Phase 14 will handle the Signal Engine.

---

# 22. CONFLICT DETECTION

This is very important.

The engine must detect when indicators disagree.

Example:

```text
H4 trend = Bullish
H1 trend = Bullish
M15 momentum = Bearish
M5 structure = Bearish
```

Do not hide this conflict by producing:

```text
Bullish
```

Instead return the evidence and conflict.

A professional analytical system should preserve uncertainty.

---

# 23. NO MAGIC SCORE

Do not create something like:

```text
EMA = +20
RSI = +15
MACD = +20
Pattern = +30
Total = 85
```

unless there is a scientifically justified and later validated scoring model.

Do not invent arbitrary weights.

At this stage, preserve the underlying evidence.

Later phases can determine how evidence should be combined and validated.

---

# 24. ANALYSIS RESULT MODEL

Create application-owned models.

For example:

```text
TechnicalAnalysisResult
-----------------------
Symbol
Timeframe
AnalyzedAtUtc

Trend
Momentum
Volatility
MarketStructure
SupportResistance
CandlestickPatterns
PriceAction
IndicatorValues
Conflicts
```

Use proper typed models rather than unstructured JSON everywhere.

---

# 25. INDICATOR RESULT MODEL

Use reusable result structures where appropriate.

For example:

```text
IndicatorResult
---------------
Name
Period
Value
PreviousValue
SignalState
CalculatedAtUtc
```

Do not force every indicator into exactly the same structure if the indicator naturally has multiple values.

MACD, Bollinger Bands, and ADX require multiple values.

Use appropriate domain models.

---

# 26. CALCULATION ACCURACY

Technical indicators must be mathematically correct.

For every indicator:

* document formula/source
* test known values
* test edge cases
* test insufficient data
* test constant prices
* test missing candles
* test gaps
* test chronological ordering

Do not silently produce results when there is insufficient history.

Example:

If EMA 200 requires sufficient historical data, do not pretend a result is fully reliable using only a handful of candles.

---

# 27. WARM-UP PERIOD

Implement indicator warm-up handling.

For example:

```text
EMA 200
```

requires adequate historical data before the result should be considered fully initialized.

The result should indicate whether it is:

```text
Ready
WarmingUp
InsufficientData
```

Do not hide this state.

---

# 28. NO LOOK-AHEAD BIAS

This is critical.

Technical analysis must never use future candles when calculating a historical result.

For example:

When calculating analysis at:

```text
2026-01-01 10:00
```

the calculation may only use data available at or before that point.

Never use:

```text
10:05
10:10
10:15
```

to calculate the 10:00 result.

This requirement is essential for future backtesting.

---

# 29. REPRODUCIBILITY

Given the same historical candle dataset and same configuration:

```text
Input A
+
Configuration A
=
Result A
```

Repeated calculations should produce the same result.

Avoid hidden external dependencies inside technical calculations.

---

# 30. PERFORMANCE

Technical analysis will later be run frequently.

Avoid recalculating the entire historical dataset for every new candle.

Use an architecture that can support:

```text
Initial calculation
        ↓
Incremental update
        ↓
New candle
        ↓
Update only what is necessary
```

However, do not prematurely build a complicated streaming engine.

Start with a clean implementation that can later be optimized.

Measure before optimizing.

---

# 31. CACHING

Do not cache every indicator result blindly.

SQL Server remains the historical source of truth.

If performance testing shows repeated calculations are expensive, consider caching recent analysis results.

Keep caching behind the analysis service so the rest of the application does not depend on Redis.

---

# 32. DATABASE

Decide carefully which analysis data should be persisted.

Raw market candles remain the primary historical source.

Do not blindly store every intermediate mathematical calculation.

Persist only analysis results that provide real value for:

* historical analysis
* dashboard display
* backtesting
* debugging
* auditability

Do not create a database table for every individual indicator unless justified.

---

# 33. API

Expose your own API.

Example:

```text
GET /api/analysis/XAUUSD/M15
GET /api/analysis/XAUUSD/H1
GET /api/analysis/XAUUSD/multi-timeframe
```

The API must return YOUR models.

The frontend must not know:

```text
MT5 indicator object
Third-party indicator library object
```

The frontend only knows your API contracts.

---

# 34. YOUR OWN INTERFACE

Maintain this architecture:

```text
Application
    │
    ├── ITechnicalAnalysisService
    │
    ├── IIndicatorCalculator
    │
    ├── ICandlestickAnalyzer
    │
    ├── IMarketStructureAnalyzer
    │
    └── ISupportResistanceAnalyzer
             │
             ↓
        Your implementations
```

Do not make the entire application depend on a third-party technical-analysis interface.

A library may be used internally for mathematics if justified, but wrap it behind your own implementation.

---

# 35. LIBRARY USAGE

If using an external technical-analysis library:

```text
Your Application
      ↓
Your Indicator Calculator
      ↓
External mathematical library
```

NOT:

```text
Your Application
      ↓
External library API everywhere
```

This keeps the project replaceable and maintainable.

If a library is unnecessary, implement the calculations ourselves.

Do not add dependencies simply because they exist.

---

# 36. TEST DATA

Create deterministic test datasets.

Use known candle sequences where expected indicator results can be verified.

For example:

```text
Known OHLC dataset
       ↓
EMA
       ↓
Expected value
```

Compare against trusted mathematical/reference calculations.

Document any small floating-point differences.

---

# 37. TESTS

Create comprehensive tests for:

## Indicators

```text
[ ] SMA
[ ] EMA
[ ] RSI
[ ] MACD
[ ] ATR
[ ] ADX
[ ] Bollinger Bands
[ ] Stochastic
```

## Candlestick patterns

```text
[ ] Doji
[ ] Hammer
[ ] Inverted Hammer
[ ] Shooting Star
[ ] Hanging Man
[ ] Bullish Engulfing
[ ] Bearish Engulfing
[ ] Morning Star
[ ] Evening Star
[ ] Inside Bar
[ ] Outside Bar
```

## Market structure

```text
[ ] Swing high
[ ] Swing low
[ ] Higher high
[ ] Higher low
[ ] Lower high
[ ] Lower low
```

## Other analysis

```text
[ ] Support/resistance
[ ] Price action
[ ] Volatility
[ ] Multi-timeframe
[ ] Confluence
[ ] Conflict detection
```

---

# 38. EDGE CASE TESTING

Test:

```text
[ ] Empty dataset
[ ] One candle
[ ] Insufficient candles
[ ] Missing candles
[ ] Duplicate candles
[ ] Unordered candles
[ ] Zero/negative price
[ ] Extreme price movement
[ ] Constant price
[ ] Large historical dataset
[ ] Forming candle
```

The engine must fail safely.

---

# 39. BACKTEST COMPATIBILITY

Design the technical-analysis engine so it can later be called by Phase 15 Backtesting.

The engine must support analysis at a historical point in time.

Conceptually:

```text
Historical candles up to T
        ↓
Technical Analysis
        ↓
Result at T
```

It must NOT require today's data.

This is essential.

---

# 40. AI COMPATIBILITY

The output should later be usable by the AI layers.

Conceptually:

```text
Technical Analysis
       ↓
Structured Analysis
       ↓
Phase 12 AI Market Analysis
       ↓
Phase 13 Master AI Reasoning
```

Do not call OpenAI now.

Do not create AI prompts now unless absolutely necessary for documentation.

The technical engine must remain useful even without AI.

---

# 41. NO TRADING SIGNALS

Do NOT output:

```text
BUY
SELL
STRONG BUY
STRONG SELL
```

as final trading decisions.

You may output descriptive evidence such as:

```text
BullishTrend
BearishMomentum
StrongResistanceNearby
MultiTimeframeConflict
HighVolatility
```

Phase 14 will determine how technical evidence contributes to the final signal system.

---

# 42. NO PROFIT CLAIMS

Do not claim:

```text
90% accurate
95% accurate
high win rate
predicts gold
```

Technical analysis has not yet been statistically validated.

Accuracy belongs to Phase 16.

---

# 43. OBSERVABILITY

Provide useful diagnostics:

```text
Analysis duration
Candles used
Indicators calculated
Insufficient-data conditions
Calculation errors
Timeframe
Analysis timestamp
```

Do not log every calculation.

Do not log massive candle arrays.

---

# 44. CONFIGURATION

Use the existing configuration system.

Potential configuration:

```env
TECHNICAL_ANALYSIS_ENABLED=true

RSI_PERIOD=14

MACD_FAST_PERIOD=12
MACD_SLOW_PERIOD=26
MACD_SIGNAL_PERIOD=9

ATR_PERIOD=14
ADX_PERIOD=14

BOLLINGER_PERIOD=20
BOLLINGER_STANDARD_DEVIATION=2

STOCHASTIC_K_PERIOD=14
STOCHASTIC_D_PERIOD=3

EMA_PERIODS=9,20,50,100,200
```

Use configuration names consistent with the existing project.

Do not hardcode strategy-specific values.

---

# 45. SECURITY

Technical-analysis calculations contain no secrets.

Nevertheless:

* do not expose database credentials
* do not expose MT5 credentials
* do not expose FRED credentials
* do not expose NewsData credentials
* do not expose OpenAI credentials

The API returns analysis results only.

---

# 46. DOCUMENTATION

Create/update:

```text
docs/technical-analysis.md
```

Document:

* supported indicators
* formulas
* parameters
* warm-up requirements
* candle patterns
* market structure
* support/resistance
* volatility
* multi-timeframe analysis
* confluence
* conflicts
* look-ahead prevention
* backtesting compatibility
* API models

Document assumptions clearly.

---

# 47. DEFINITION OF DONE

Phase 6 is complete only when:

* [ ] Technical-analysis engine exists
* [ ] Application-owned interfaces exist
* [ ] MT5-specific code is not inside technical-analysis logic
* [ ] SMA works
* [ ] EMA works
* [ ] RSI works
* [ ] MACD works
* [ ] ATR works
* [ ] ADX works
* [ ] Bollinger Bands work
* [ ] Stochastic works
* [ ] Candlestick analysis works
* [ ] Market structure works
* [ ] Support/resistance analysis works
* [ ] Price-action analysis works
* [ ] Volatility analysis works
* [ ] Multi-timeframe analysis works
* [ ] Confluence analysis works
* [ ] Conflict detection works
* [ ] Warm-up states exist
* [ ] Insufficient-data handling exists
* [ ] Look-ahead bias is prevented
* [ ] Historical analysis is reproducible
* [ ] Backtesting compatibility exists
* [ ] API endpoints work
* [ ] Unit tests pass
* [ ] Integration tests pass
* [ ] Performance testing is performed
* [ ] Documentation is updated
* [ ] No OpenAI calls were added
* [ ] No trading execution was added
* [ ] No final trading signals were added

---

# 48. FINAL REPORT

After implementation, provide:

1. Files created
2. Files modified
3. Technical-analysis architecture
4. Indicators implemented
5. Candlestick patterns implemented
6. Market-structure implementation
7. Support/resistance implementation
8. Multi-timeframe implementation
9. Confluence implementation
10. Conflict detection
11. Look-ahead-bias protection
12. Backtesting compatibility
13. Tests executed
14. Test results
15. Performance measurements
16. Any issue requiring my action
17. Confirmation that Phase 6 is complete
18. Short explanation of what Phase 7 will build

Do not claim tests passed unless they were actually executed.

Do not implement Phase 7 early.

# END OF PHASE 6
