============================================================
PHASE 14 — FULL ANALYST & AI WORKSPACES
============================================================

Project: AnalystTradingSystem
Phase: 14

IMPORTANT ARCHITECTURE RULE
------------------------------------------------------------

This phase implements the FULL ANALYST.

Full Analyst is part of the main AnalystTradingSystem.

The Full Analyst is:

- AI-assisted
- real-time
- evidence-driven
- multi-timeframe
- independent from Local Analyst
- independent from Target Analyst
- independent from Telegram Scanner

The Full Analyst must NOT depend on another analyst's final
BUY/SELL/WAIT decision.

The systems may consume the same normalized market-data and
evidence infrastructure, but their conclusions must remain
independent.

The independent XAUUSD Telegram Scanner is a separate project.

Do NOT import or depend on Telegram Scanner decisions.


============================================================
1. OBJECTIVE
============================================================

Build a real-time Full Analyst that evaluates the current
XAUUSD market and produces exactly one primary state:

🟢 BUY
🔴 SELL
⚪ WAIT

The Full Analyst must prioritize evidence quality over
forcing a signal.

If evidence is:

- weak
- conflicting
- stale
- incomplete
- invalid
- insufficient

then:

→ WAIT


============================================================
2. FULL ANALYST RESPONSIBILITY
============================================================

Full Analyst answers:

"What is the strongest current market interpretation?"

It evaluates:

- current price
- market structure
- multi-timeframe structure
- liquidity
- candle behavior
- momentum / flow
- KTR
- important levels
- volatility
- economic conditions
- relevant news
- analyst information
- market regime
- invalidation conditions
- uncertainty

Target Analyst has a different responsibility.

Target Analyst:
→ determines ONE forward target.

Full Analyst:
→ determines CURRENT market direction/state.


============================================================
3. CORE FLOW
============================================================

User requests:

[FULL ANALYST]

        ↓

Create Analysis Job

        ↓

Capture Current Market Snapshot

        ↓

Validate Data

        ↓

Collect Relevant Evidence

        ↓

Evidence Selection

        ↓

Evidence Validation

        ↓

Evidence Compression

        ↓

Run Full AI Specialists

        ↓

Compare Specialist Interpretations

        ↓

Conflict Detection

        ↓

Full Master AI

        ↓

Validate Master Decision

        ↓

BUY / SELL / WAIT

        ↓

If BUY or SELL
        ↓

Generate Future / Expected Path

        ↓

Store Result

        ↓

Display Full Analyst Panel


============================================================
4. MULTI-TIMEFRAME ANALYSIS
============================================================

Supported:

1M
5M
15M
30M
1H
4H
1D

Recommended hierarchy:

1D
 ↓
4H
 ↓
1H
 ↓
30M
 ↓
15M
 ↓
5M
 ↓
1M

Higher timeframes provide broader context.

Lower timeframes provide current market behavior.

The system must identify:

- higher-timeframe trend
- intermediate structure
- current structure
- short-term momentum
- timeframe conflicts
- regime transitions


============================================================
5. AI SPECIALIST WORKSPACES
============================================================

Create independent Full Analyst workspaces:

1. Full Structure AI
2. Full Liquidity AI
3. Full Candle AI
4. Full Flow AI
5. Full KTR AI
6. Full News AI
7. Full Risk AI
8. Full Master AI

Each specialist must have a specific responsibility.

Do not make every specialist independently analyze the entire
market.


============================================================
6. FULL STRUCTURE AI
============================================================

Analyze:

- HH
- HL
- LH
- LL
- BOS
- CHoCH
- trend
- range
- transition
- higher-timeframe structure
- lower-timeframe structure
- structural strength
- structural invalidation

Return structured evidence interpretation.

Do NOT directly create the final BUY/SELL/WAIT decision.


============================================================
7. FULL LIQUIDITY AI
============================================================

Analyze:

- previous highs
- previous lows
- equal highs
- equal lows
- swing liquidity
- liquidity pools
- liquidity sweeps
- rejection after sweep
- likely liquidity attraction
- liquidity conflicts

Do not claim order-book liquidity unless actual order-book
data exists.


============================================================
8. FULL CANDLE AI
============================================================

Analyze relevant candle behavior:

- rejection
- engulfing
- momentum
- breakout
- failed breakout
- continuation
- exhaustion
- reversal
- multi-candle patterns

Candle analysis must consider:

PRICE LOCATION
+
STRUCTURE
+
LIQUIDITY
+
MOMENTUM

Do not generate a signal from a candle pattern alone.


============================================================
9. FULL FLOW AI
============================================================

Analyze available:

- price momentum
- directional pressure
- acceleration
- deceleration
- impulse
- pullback
- continuation
- exhaustion
- volume/tick-volume where available
- volatility expansion/contraction

Never fabricate unavailable order-flow information.


============================================================
10. FULL KTR AI
============================================================

Analyze:

- KTR
- important price levels
- previous highs/lows
- session levels where available
- breakout/retest areas
- volatility-adjusted levels
- relevant reaction zones

Use the project's established KTR definitions.

Do not invent unsupported KTR values.


============================================================
11. FULL NEWS AI
============================================================

Analyze relevant:

- economic events
- USD events
- macro conditions
- central-bank information
- important financial news
- analyst information
- actual vs forecast
- expected reaction
- observed reaction
- current relevance

Separate:

FACT

from:

INTERPRETATION

Do not treat AI interpretation as raw factual data.


============================================================
12. FULL RISK AI
============================================================

Risk AI evaluates the quality and risk of the current market
setup.

Consider:

- volatility
- market regime
- news risk
- conflicting evidence
- structural invalidation
- liquidity conflicts
- data quality
- uncertainty
- abnormal market conditions

Risk AI can recommend:

INSUFFICIENT EVIDENCE

which contributes toward WAIT.

Risk AI must NOT independently execute trades.


============================================================
13. FULL MASTER AI
============================================================

Full Master AI is the final synthesis layer.

It receives structured results from:

- Structure AI
- Liquidity AI
- Candle AI
- Flow AI
- KTR AI
- News AI
- Risk AI

It must NOT simply count votes.

Example:

Structure → BUY
Liquidity → BUY
Candle → SELL
Flow → BUY
KTR → BUY
News → MIXED
Risk → HIGH RISK

Master must determine:

- why specialists disagree
- which evidence is stronger
- which evidence is stale
- which evidence is independent
- whether conflicts are meaningful
- whether current conditions justify BUY/SELL
- whether uncertainty requires WAIT

Final states:

🟢 BUY
🔴 SELL
⚪ WAIT


============================================================
14. DECISION CONFIDENCE
============================================================

Confidence must represent evidence quality.

It must NOT mean:

"probability of guaranteed profit."

Confidence should consider:

- evidence agreement
- evidence quality
- evidence freshness
- market structure clarity
- timeframe alignment
- news clarity
- risk conditions
- data quality
- uncertainty

If confidence is insufficient:

→ WAIT


============================================================
15. BUY DECISION
============================================================

BUY should only be produced when evidence supports a current
bullish interpretation.

Potential supporting evidence:

- bullish higher-timeframe structure
- bullish current structure
- liquidity behavior
- bullish candle confirmation
- positive momentum
- supportive KTR/levels
- acceptable risk
- no critical contradictory evidence

These are examples, not mandatory hardcoded conditions.

Master AI must evaluate context rather than blindly checking
a fixed checklist.


============================================================
16. SELL DECISION
============================================================

SELL should only be produced when evidence supports a current
bearish interpretation.

Potential supporting evidence:

- bearish higher-timeframe structure
- bearish current structure
- liquidity behavior
- bearish candle confirmation
- negative momentum
- supportive KTR/levels
- acceptable risk
- no critical contradictory evidence

Again, context matters.

Do not force SELL because one specialist is bearish.


============================================================
17. WAIT DECISION
============================================================

WAIT is a valid and important result.

Return WAIT when:

- evidence conflicts strongly
- structure is unclear
- market is transitioning
- news reaction is uncertain
- data is incomplete
- volatility is abnormal
- risk is too high
- AI specialists fail
- Master cannot establish sufficient evidence

Example:

┌──────────────────────────────────────┐
│ FULL ANALYST             XAUUSD · 15M│
│                                      │
│ ⚪ WAIT                              │
│                                      │
│ Evidence is currently conflicting.  │
│ No sufficiently strong direction.   │
│                                      │
│ 🔮 FUTURE: NONE                      │
└──────────────────────────────────────┘


============================================================
18. FUTURE / EXPECTED PATH
============================================================

IMPORTANT:

Future is generated only after:

🟢 BUY

or:

🔴 SELL

Future is NOT generated when:

⚪ WAIT

Future is an expected short-term market path.

It is NOT:

- guaranteed future price
- guaranteed prediction
- automatic trade
- Target Analyst output
- Telegram signal


Example:

FULL ANALYST
🟢 BUY

🔮 FUTURE

Current price
      ↓
Pullback
      ↓
Support holds
      ↓
Continuation
      ↓
Expansion toward next area


The Future system itself is implemented in Phase 15.

Phase 14 should expose the necessary structured context for
Phase 15.


============================================================
19. NO TARGET OBJECT
============================================================

Full Analyst must NOT create:

- Target 1
- Target 2
- Target price object
- Target Analyst result

Target belongs to:

PHASE 13 — TARGET ANALYST

Future belongs to:

PHASE 15 — FUTURE SCENARIO & EXPECTED PATH


============================================================
20. FULL ANALYST RESULT
============================================================

BUY example:

┌──────────────────────────────────────┐
│ FULL ANALYST             XAUUSD · 15M│
│                                      │
│ 🟢 BUY                               │
│                                      │
│ 🔮 FUTURE                            │
│ Pullback → continuation → expansion  │
│                                      │
│ Entry Context: ...                   │
│ Invalidation: ...                    │
│ Confidence: ...                      │
│ Valid Until: ...                     │
│                                      │
│       [ CANCEL ANALYST ]             │
└──────────────────────────────────────┘


SELL example:

┌──────────────────────────────────────┐
│ FULL ANALYST             XAUUSD · 15M│
│                                      │
│ 🔴 SELL                              │
│                                      │
│ 🔮 FUTURE                            │
│ Rejection → continuation → decline    │
│                                      │
│ Entry Context: ...                   │
│ Invalidation: ...                    │
│ Confidence: ...                      │
│ Valid Until: ...                     │
│                                      │
│       [ CANCEL ANALYST ]             │
└──────────────────────────────────────┘


============================================================
21. CANCEL ANALYST
============================================================

The result panel must provide:

[ CANCEL ANALYST ]

When cancelled:

- stop active Full Analyst monitoring
- remove active analyst panel
- remove active Future representation if present
- mark analysis as CANCELLED
- preserve historical analysis
- preserve specialist results
- preserve evidence
- preserve Master result


============================================================
22. ANALYSIS SNAPSHOT
============================================================

Every Full Analyst job must capture a snapshot containing:

- Symbol
- Current price
- Analysis time
- Requested timeframe
- Available timeframes
- Current candle state
- Market state
- Data versions
- Evidence IDs
- Provider information
- AI configuration versions
- Prompt versions

The snapshot represents what was known at the time of analysis.


============================================================
23. LOOK-AHEAD PROTECTION
============================================================

Enforce:

Evidence.AvailableAt <= AnalysisTime

No future:

- candles
- news
- economic releases
- revised information
- analyst information

may enter the current analysis snapshot if it was unavailable
at AnalysisTime.


============================================================
24. EVIDENCE MANAGEMENT
============================================================

Use the Phase 11 evidence foundation.

Pipeline:

Raw Evidence
      ↓
Validate
      ↓
Deduplicate
      ↓
Rank
      ↓
Select
      ↓
Compress
      ↓
Specialist AI
      ↓
Master AI

Do not send unlimited raw market data.

Use meaningful evidence.


============================================================
25. AI PROVIDER CONFIGURATION
============================================================

Every Full AI workspace must have independent configuration.

Required:

FULL_STRUCTURE_AI_PROVIDER=
FULL_STRUCTURE_AI_API_KEY=
FULL_STRUCTURE_AI_MODEL=
FULL_STRUCTURE_AI_BASE_URL=

FULL_LIQUIDITY_AI_PROVIDER=
FULL_LIQUIDITY_AI_API_KEY=
FULL_LIQUIDITY_AI_MODEL=
FULL_LIQUIDITY_AI_BASE_URL=

FULL_CANDLE_AI_PROVIDER=
FULL_CANDLE_AI_API_KEY=
FULL_CANDLE_AI_MODEL=
FULL_CANDLE_AI_BASE_URL=

FULL_FLOW_AI_PROVIDER=
FULL_FLOW_AI_API_KEY=
FULL_FLOW_AI_MODEL=
FULL_FLOW_AI_BASE_URL=

FULL_KTR_AI_PROVIDER=
FULL_KTR_AI_API_KEY=
FULL_KTR_AI_MODEL=
FULL_KTR_AI_BASE_URL=

FULL_NEWS_AI_PROVIDER=
FULL_NEWS_AI_API_KEY=
FULL_NEWS_AI_MODEL=
FULL_NEWS_AI_BASE_URL=

FULL_RISK_AI_PROVIDER=
FULL_RISK_AI_API_KEY=
FULL_RISK_AI_MODEL=
FULL_RISK_AI_BASE_URL=

FULL_MASTER_AI_PROVIDER=
FULL_MASTER_AI_API_KEY=
FULL_MASTER_AI_MODEL=
FULL_MASTER_AI_BASE_URL=

Also support per workspace:

- Timeout
- Max Tokens
- Temperature
- Enabled

Do not hardcode OpenAI.

Do not automatically reuse Target Analyst credentials.

Target AI and Full AI configurations must remain separate.


============================================================
26. AI EXECUTION EFFICIENCY
============================================================

Full Analyst must not unnecessarily call every AI workspace
for every tiny market-data update.

Use:

- evidence hashing
- state-change detection
- cached interpretations
- relevance ranking
- evidence compression
- conditional specialist execution
- prompt versioning
- token limits

Example:

If only an irrelevant candle update occurs, do not rerun every
specialist unnecessarily.

If major structure or news changes, the relevant specialists
may need to run again.


============================================================
27. SPECIALIST RESULT FORMAT
============================================================

Specialist outputs should be structured.

Example:

SpecialistResult

├── Id
├── Workspace
├── Direction
├── EvidenceIds[]
├── KeyFindings[]
├── Impact
├── Confidence
├── Uncertainty
├── Invalidation
├── Summary
├── Provider
├── Model
├── PromptVersion
├── AnalysisTime
├── CreatedAt
└── Status


============================================================
28. MASTER RESULT FORMAT
============================================================

MasterResult

├── Id
├── SpecialistResultIds[]
├── Decision
├── Confidence
├── Agreement
├── Conflicts[]
├── KeyEvidenceIds[]
├── Reasoning
├── Invalidation
├── Uncertainty
├── AnalysisTime
├── Provider
├── Model
├── PromptVersion
├── CreatedAt
└── Status


Decision:

BUY
SELL
WAIT


============================================================
29. FULL ANALYST LIFECYCLE
============================================================

ANALYSIS REQUEST
        ↓
ANALYZING
        ↓
MASTER DECISION
        ↓
BUY / SELL / WAIT
        ↓
If BUY/SELL:
        ↓
Future becomes available
        ↓
ACTIVE
        ↓
Monitor validity
        ↓
CANCELLED / EXPIRED / INVALIDATED / COMPLETED
        ↓
HISTORY

WAIT:

ANALYZING
    ↓
WAIT
    ↓
No active Future
    ↓
History


============================================================
30. DATA QUALITY FAILURE
============================================================

If required market data is unavailable:

Do not guess.

Possible result:

⚪ WAIT

with:

Reason:
Insufficient or invalid market data.

If a specialist fails:

- retry where configured
- use valid cached interpretation when allowed
- reduce confidence
- or return WAIT

If Master AI fails:

→ WAIT

Never fabricate a decision.


============================================================
31. SEPARATION FROM LOCAL ANALYST
============================================================

Local Analyst is deterministic.

Full Analyst is AI-assisted.

Example:

LOCAL
🟢 BUY

FULL
🔴 SELL

This is valid.

Do not synchronize their conclusions.

Do not allow Full Analyst to overwrite Local Analyst.

Do not allow Local Analyst to overwrite Full Analyst.


============================================================
32. SEPARATION FROM TARGET ANALYST
============================================================

Target Analyst:

🎯 ONE TARGET

Full Analyst:

🟢 BUY / 🔴 SELL / ⚪ WAIT

Do not pass the Target Analyst's target as a decision input.

Do not make Full Analyst depend on Target Analyst.


============================================================
33. SEPARATION FROM TELEGRAM SCANNER
============================================================

The XAUUSD Telegram Scanner is a completely separate project.

It is:

- 24/7
- M5-focused
- deterministic
- rule-based
- no AI decision
- economic-news aware
- Telegram output

Full Analyst must NOT:

- consume Telegram signals
- send Telegram signals
- depend on Telegram
- use Telegram BUY/SELL decisions
- generate Telegram alerts

The two systems remain independent.


============================================================
34. NO AUTOMATIC TRADING
============================================================

Phase 14 must NOT implement:

- automatic order execution
- broker order placement
- trade execution
- account management
- position management

The Full Analyst only produces analysis.


============================================================
35. TESTING
============================================================

Test:

- analysis job creation
- snapshot creation
- evidence selection
- evidence validation
- look-ahead protection
- evidence compression
- specialist execution
- specialist failure
- Master synthesis
- conflict detection
- BUY
- SELL
- WAIT
- confidence
- uncertainty
- lifecycle
- cancellation
- expiration
- invalidation
- history preservation
- provider configuration
- prompt versioning
- caching
- token management
- malformed AI responses
- stale evidence
- missing market data
- multi-timeframe analysis


============================================================
36. PERFORMANCE
============================================================

Optimize for:

- low unnecessary token usage
- asynchronous AI execution
- parallel specialist execution where appropriate
- caching
- incremental evidence processing
- deduplication
- efficient database queries
- controlled concurrency
- timeout handling
- provider rate limits

Do not continuously run expensive AI analysis every second.

AI should run when:

- the user requests Full Analyst
- meaningful market state changes require re-analysis
- configured monitoring conditions require it

Do not confuse market-data updates with mandatory AI calls.


============================================================
37. COMPLETION CRITERIA
============================================================

Phase 14 is complete when:

1. Full Analyst exists.
2. Full Analyst is user-triggered.
3. Current market snapshot is captured.
4. Multi-timeframe analysis works.
5. Relevant evidence is selected.
6. Evidence is validated.
7. Look-ahead protection works.
8. Evidence is compressed.
9. Eight Full AI workspaces exist.
10. Each workspace has independent provider configuration.
11. Specialist results are structured.
12. Master AI synthesizes specialist evidence.
13. Master does not simply majority-vote.
14. Conflict detection works.
15. BUY works.
16. SELL works.
17. WAIT works.
18. Confidence and uncertainty are recorded.
19. No forced signal is generated.
20. No Target object is created.
21. Future is only available for BUY/SELL.
22. WAIT produces no Future.
23. Cancel Analyst works.
24. Analysis lifecycle works.
25. Historical results are preserved.
26. AI failures are handled safely.
27. Token usage is controlled.
28. Tests pass.
29. Full Analyst remains independent from Local Analyst.
30. Full Analyst remains independent from Target Analyst.
31. Full Analyst remains independent from Telegram Scanner.
32. No automatic trading is implemented.


============================================================
38. NEXT PHASE
============================================================

After Phase 14 is fully implemented and tested:

→ PHASE 15 — FUTURE SCENARIO & EXPECTED PATH

Phase 15 will implement the structured Future/Expected Path
generated from the Full Analyst's BUY/SELL result.

Future must remain separate from:

- Target Analyst target
- Local Analyst signal
- Telegram Scanner signal


============================================================
END OF PHASE 14
============================================================