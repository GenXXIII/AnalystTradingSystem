# XAUUSD SIGNAL ANALYST — MASTER ANALYSIS FLOW

## Core Objective

Analyze XAUUSD as accurately as possible using all available relevant evidence.

The system must **not try to generate a signal just because a signal is expected**.

If the evidence is strong enough → produce a READY BUY/SELL setup.

If the evidence is incomplete, conflicting, uncertain, or not sufficiently confirmed → produce WAIT.

The goal is not perfection or guaranteed predictions.

The goal is:

**Maximum analytical accuracy + strict evidence validation + disciplined WAIT decisions.**

---

# MASTER FLOW

```text
                         XAU/USD SIGNAL ANALYST
                                  │
                                  ▼
                         1. MARKET DATA
                                  │
                    Price / Volume / Time
                                  │
                                  ▼
                         2. MARKET REGIME
                                  │
                    Trend / Range / Transition
                                  │
                                  ▼
                    3. MULTI-TIMEFRAME ANALYSIS
                                  │
              1D → 4H → 1H → 30M → 15M → 5M → M1
                                  │
                                  ▼
                    4. HIGHER-TIMEFRAME STRUCTURE
                                  │
                           1D → 4H → 1H
                                  │
                                  ▼
                         5. LIQUIDITY MAP
                                  │
             Previous High/Low / Equal Highs/Lows
             Swing Points / Session Levels / Key Zones
                                  │
                                  ▼
                         6. CANDLE ANALYSIS
                                  │
              Candle Type / Context / Sequence
                                  │
                                  ▼
                          7. CANDLE FLOW
                                  │
       Push / Rejection / Absorption / Exhaustion
       Compression / Expansion / Failed Breakout
                                  │
                                  ▼
                         8. ORDER FLOW*
                                  │
             Delta / Footprint / OI / CVD / Volume
                                  │
                                  ▼
                         9. SESSION FLOW
                                  │
                   Asia → London → New York
                                  │
                                  ▼
                        10. MACRO / NEWS
                                  │
          USD / Yields / Fed / CPI / NFP / FOMC
                   Geopolitics / Risk Sentiment
                                  │
                                  ▼
                       11. STRATEGY ANALYSIS
                                  │
        Candlestick / Trend / Breakout / Pullback
        Reversal / Momentum / Support-Resistance
        Multi-Timeframe / News / Macro / Combined
                                  │
                                  ▼
                       12. CONFLUENCE ENGINE
                                  │
              Supporting / Conflicting / Neutral
                                  │
                                  ▼
                        13. SETUP DETECTOR
                                  │
                    15M Primary Setup
                    5M Setup Development
                                  │
                                  ▼
                         14. ENTRY ENGINE
                                  │
              Entry Condition / Confirmation
                    Invalidation / Timing
                                  │
                                  ▼
                          15. RISK ENGINE
                                  │
                SL / TP / R:R / Volatility
                News Risk / Setup Risk
                                  │
                                  ▼
                       16. EVIDENCE CHECK
                                  │
               "Is the evidence strong enough?"
                                  │
                    ┌─────────────┴─────────────┐
                    ▼                           ▼
                  YES                           NO
                    │                           │
                    ▼                           ▼
               READY SIGNAL                    WAIT
                    │
              BUY or SELL
                    │
                    ▼
              17. SIGNAL RECORD
                    │
                    ▼
              18. TRADE JOURNAL
                    │
                    ▼
              19. OUTCOME ENGINE
                    │
             TP / SL / EXPIRED
                    │
                    ▼
            20. PERFORMANCE LOOP
                    │
      Historical Accuracy / Win Rate / Expectancy
      Strategy / Timeframe / Regime / Session
                    │
                    ▼
             HISTORICAL LEARNING
```

---

# TIMEFRAME HIERARCHY

The timeframes have different responsibilities.

```text
1D
 ↓
Major Market Environment

4H
 ↓
Major Swing Structure + Important Liquidity

1H
 ↓
Directional Market Context

30M
 ↓
Intraday Context

15M
 ↓
PRIMARY SETUP TIMEFRAME

5M
 ↓
SETUP DEVELOPMENT + ENTRY CONFIRMATION

M1
 ↓
OPTIONAL FINE TIMING
```

### Important relationship between 15M and 5M

A 15-minute candle contains **three 5-minute candles**.

Therefore, 5M is not an unrelated analysis.

```text
15M
│
├── 5M candle #1
├── 5M candle #2
└── 5M candle #3
```

The system should use this relationship to understand what is happening **inside the 15M setup**.

### Example

```text
15M
Bearish Setup
      ↓
Look deeper into 5M
      ↓
5M correction
      ↓
5M rejection
      ↓
5M bearish structure
      ↓
5M bearish expansion
      ↓
SELL confirmation
```

However:

**5M confirmation is not mandatory agreement with every higher timeframe.**

A 15M setup can temporarily conflict with 5M because the 5M may be making a correction.

In that situation:

```text
15M → SELL setup
5M  → temporary bullish correction
       ↓
     WAIT
       ↓
5M bearish confirmation
       ↓
SELL READY
```

---

# CANDLE FLOW ANALYSIS

The analyst must understand the **sequence of candles**, not simply identify individual candle patterns.

Example SELL flow:

```text
Bullish Push
      ↓
Liquidity Sweep
      ↓
Rejection
      ↓
Failed Breakout
      ↓
Bearish Engulfing
      ↓
Lower High
      ↓
Bearish Expansion
      ↓
SELL FLOW
```

Example BUY flow:

```text
Bearish Push
      ↓
Liquidity Sweep
      ↓
Bullish Rejection
      ↓
Failed Breakdown
      ↓
Bullish Engulfing
      ↓
Higher Low
      ↓
Bullish Expansion
      ↓
BUY FLOW
```

The analyst should explain **why the candle sequence supports the direction**.

---

# STRATEGY ANALYSIS

Each strategy should be analyzed independently.

```text
Candlestick       → Bullish / Bearish / Neutral
Trend             → Bullish / Bearish / Neutral
Breakout          → Valid / Failed / Neutral
Pullback          → Valid / Invalid / Neutral
Reversal          → Valid / Invalid / Neutral
Momentum          → Bullish / Bearish / Neutral
Support/Resistance→ Bullish / Bearish / Neutral
Multi-Timeframe   → Aligned / Conflicting
News              → Bullish / Bearish / Neutral
Macro             → Bullish / Bearish / Neutral
Combined          → Overall setup analysis
```

Do **not** simply count:

```text
7 bullish
3 bearish
→ BUY
```

The importance and context of each piece of evidence must be considered.

---

# CONFLUENCE AND CONFLICT

The analyst must explicitly separate:

### Supporting Evidence

```text
✓ Bearish market structure
✓ Liquidity sweep
✓ Bearish rejection
✓ Failed breakout
✓ Bearish candle flow
✓ 5M bearish confirmation
```

### Conflicting Evidence

```text
⚠ 1D bullish
⚠ 4H bullish
⚠ Major news approaching
⚠ Macro conditions conflicting
```

### Neutral Evidence

```text
○ Volume inconclusive
○ Breakout strategy not applicable
○ No meaningful analyst consensus
```

The system must **never hide conflicting evidence just to produce a BUY or SELL signal**.

---

# ENTRY DECISION

A setup is not automatically an entry.

```text
SETUP DETECTED
      ↓
ENTRY CONDITIONS
      ↓
CONFIRMATION
      ↓
INVALIDATION CHECK
      ↓
RISK CHECK
      ↓
EVIDENCE CHECK
      ↓
READY / WAIT
```

Example:

```text
15M:
Bearish setup ✓

5M:
Bearish confirmation ✓

Liquidity:
Confirmed ✓

Candle flow:
Bearish ✓

Structure:
Bearish ✓

Major news:
No immediate high-risk event ✓

Invalidation:
Not triggered ✓

Risk:
Acceptable ✓

        ↓

🔴 SELL — READY
```

---

# WAIT RULE

WAIT is a **valid analytical result**.

The analyst must return WAIT when:

```text
• Required confirmation is missing
• Timeframes have unresolved conflict
• Candle flow is unclear
• Structure is unclear
• Liquidity reaction is incomplete
• Entry conditions are incomplete
• Major news creates excessive uncertainty
• Data is missing or unreliable
• Setup has been invalidated
• Risk conditions are unacceptable
• Evidence is not strong enough
```

Example:

```text
15M → SELL setup ✓
5M  → Bullish correction
Candle flow → Mixed
Macro → Conflicting
News → High risk

             ↓

          🟡 WAIT
```

The system must **never manufacture certainty**.

---

# ACCURACY PRINCIPLE

The analyst should always attempt to reach the **most accurate conclusion possible from the available evidence**.

It must not claim:

```text
"95% accurate"
"Guaranteed"
"Certain"
"Cannot lose"
```

Instead, it should distinguish:

```text
CURRENT ANALYSIS
        +
HISTORICAL EVIDENCE
        +
CURRENT UNCERTAINTY
```

For example:

```text
Current Setup:
Strong bearish evidence

Historical Comparable Setups:
247 samples

Historical Successful Outcomes:
161

Historical Success Rate:
65.2%

Current Status:
READY

Current Conflicts:
1D/4H bullish

Current News Risk:
Medium
```

Historical statistics must come from **actual recorded outcomes**, not from the AI's opinion.

---

# FINAL SIGNAL STATES

The analyst should be able to produce:

```text
🟢 BUY — READY

🔴 SELL — READY

🟡 BUY SETUP — WAITING

🟡 SELL SETUP — WAITING

⚪ INVALIDATED

⚪ NO VALID SETUP
```

The system should not force a signal.

---

# FINAL ANALYST RESULT

Every final result should answer:

```text
1. WHAT is the market doing?

2. WHAT is the current market regime?

3. WHAT is the higher-timeframe structure?

4. WHERE is the liquidity?

5. WHAT is the candle flow?

6. WHAT strategies support the setup?

7. WHAT strategies conflict with it?

8. WHAT does the 15M setup show?

9. WHAT is happening inside the setup on 5M?

10. IS the entry confirmed?

11. WHAT would invalidate the setup?

12. WHAT is the risk condition?

13. WHAT evidence supports the decision?

14. WHAT evidence contradicts it?

15. SHOULD the system say BUY, SELL, or WAIT?

16. WHAT actually happened afterward?

17. HOW accurate has this type of setup historically been?
```

---

# MOST IMPORTANT RULE

```text
                DO NOT FORCE A SIGNAL

                      ↓

        If evidence is strong enough
                      ↓
              BUY / SELL — READY

        If evidence is insufficient
                      ↓
                    WAIT

        If evidence conflicts
                      ↓
                    WAIT

        If data is unreliable
                      ↓
                    WAIT

        If setup is invalidated
                      ↓
                 INVALIDATED
```

**The analyst's job is not to produce many signals.**

**The analyst's job is to produce the best-supported analysis possible and remain in WAIT whenever the evidence does not justify a confident decision.**

* Order-flow information must only be used when genuine data is available. The system must never fabricate Delta, Footprint, CVD, Open Interest, or other unavailable order-flow measurements.
