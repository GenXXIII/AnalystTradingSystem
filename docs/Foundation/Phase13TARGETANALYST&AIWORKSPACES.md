============================================================
PHASE 13 — TARGET ANALYST & AI WORKSPACES
============================================================

Project: AnalystTradingSystem
Phase: 13

IMPORTANT ARCHITECTURE RULE
------------------------------------------------------------

This phase implements the TARGET ANALYST.

Target Analyst is:

- AI-assisted
- user-triggered
- predictive
- forward-looking
- evidence-driven
- independent from Local Analyst
- independent from Full Analyst
- independent from Future
- independent from Telegram Scanner

The Target Analyst's purpose is to determine ONE strong
forward target based on the available evidence.

It does NOT produce:

- BUY
- SELL
- WAIT
- automatic trade execution
- Telegram signals

The Target Analyst produces:

🎯 ONE TARGET

If evidence is insufficient:

→ NO VALID TARGET


============================================================
1. OBJECTIVE
============================================================

Build a deep predictive Target Analyst that analyzes the
current XAUUSD market and determines the most defensible
forward target.

The analysis should use all relevant available evidence,
including:

- current market data
- historical market data
- multi-timeframe structure
- liquidity
- candles
- momentum / flow
- KTR
- important levels
- volatility
- economic data
- macro conditions
- relevant news
- analyst information
- previously interpreted evidence

"All evidence" means all RELEVANT evidence.

Do not send unlimited raw data to AI.


============================================================
2. CORE FLOW
============================================================

User clicks:

[TARGET ANALYST]

        ↓

Create Target Analysis Job

        ↓

Capture Current Market Snapshot

        ↓

Validate Market Data

        ↓

Collect Relevant Evidence

        ↓

Evidence Selection

        ↓

Evidence Validation

        ↓

Evidence Compression

        ↓

Run Target AI Specialists

        ↓

Compare Specialist Interpretations

        ↓

Conflict Detection

        ↓

Target Master AI

        ↓

Target Validation

        ↓

ONE TARGET

        ↓

Create Target Result

        ↓

Display Target Panel

        ↓

Monitor Target Lifecycle


============================================================
3. ONE CLICK = ONE TARGET
============================================================

Every user click creates one independent Target Analysis job.

One successful analysis must produce exactly:

🎯 ONE TARGET

Do NOT produce:

- Target 1
- Target 2
- Target 3
- primary target
- secondary target
- alternative target

The system must select the single strongest target.

If no target is sufficiently supported:

→ NO VALID TARGET


============================================================
4. TARGET ANALYST DOES NOT DECIDE DIRECTION
============================================================

Target Analyst is not the Local Analyst or Full Analyst.

Do not require:

🟢 BUY
or
🔴 SELL

as the primary output.

The target analysis determines the strongest forward target
based on evidence.

Direction/context may be part of the reasoning internally,
but the final Target Analyst result must remain focused on
ONE target.


============================================================
5. TARGET AI WORKSPACES
============================================================

Create independent AI workspaces:

1. Target Structure AI
2. Target Liquidity AI
3. Target Candle AI
4. Target Flow AI
5. Target KTR AI
6. Target News AI
7. Target Risk AI
8. Target Master AI

Each workspace must have a specific responsibility.

Do not make every specialist analyze the entire market.


============================================================
6. TARGET STRUCTURE AI
============================================================

Analyze:

- 1D structure
- 4H structure
- 1H structure
- 30M structure
- 15M structure
- 5M structure
- 1M structure where relevant
- HH / HL
- LH / LL
- BOS
- CHoCH
- trend
- range
- transition
- structural invalidation

Identify price areas that are structurally meaningful as
potential forward targets.

Do not force a target if structure is unclear.


============================================================
7. TARGET LIQUIDITY AI
============================================================

Analyze:

- previous highs
- previous lows
- equal highs
- equal lows
- swing liquidity
- liquidity pools
- liquidity sweeps
- reaction after sweep
- likely liquidity attraction
- liquidity invalidation

Identify the strongest relevant forward liquidity destination.

Do not claim order-book liquidity unless actual order-book
data exists.


============================================================
8. TARGET CANDLE AI
============================================================

Analyze relevant candle behavior:

- rejection
- engulfing
- momentum
- breakout
- failed breakout
- continuation
- exhaustion
- reversal characteristics
- multi-candle behavior

Candle evidence must be interpreted together with structure
and price location.

Do not select a target from a candle pattern alone.


============================================================
9. TARGET FLOW AI
============================================================

Analyze:

- momentum
- directional pressure
- acceleration
- deceleration
- impulse
- pullback
- continuation
- exhaustion
- volume/tick-volume where available
- volatility expansion/contraction

Do not fabricate order-flow information that the data source
does not provide.


============================================================
10. TARGET KTR AI
============================================================

Analyze:

- KTR
- important price levels
- previous session levels
- previous highs/lows
- volatility-adjusted levels
- breakout/retest levels
- relevant technical zones

Use the project's existing KTR definitions.

Do not invent unsupported levels.


============================================================
11. TARGET NEWS AI
============================================================

Analyze relevant:

- economic events
- USD events
- macro conditions
- central-bank information
- financial news
- analyst information
- expected vs actual news reaction
- current relevance
- market reaction

Separate:

FACT

from

INTERPRETATION.

Do not treat an AI interpretation as raw factual data.


============================================================
12. TARGET RISK AI
============================================================

Risk AI evaluates whether the proposed target is sufficiently
supported.

Consider:

- volatility
- distance to target
- structural obstacles
- liquidity obstacles
- important levels between current price and target
- major news risk
- conflicting evidence
- market regime
- data quality
- invalidation conditions
- uncertainty

Risk AI must be allowed to reject a target.

Possible result:

NO VALID TARGET


============================================================
13. TARGET MASTER AI
============================================================

Target Master is the final synthesis layer.

It receives structured outputs from:

- Structure AI
- Liquidity AI
- Candle AI
- Flow AI
- KTR AI
- News AI
- Risk AI

It must NOT simply count votes.

Example:

Structure → Target area A
Liquidity → Target area A
Candle → Weak support
Flow → Target area A
KTR → Target area B
News → Uncertain
Risk → Target A acceptable

Master must determine:

- which evidence is strongest
- which evidence is independent
- which evidence is stale
- why specialists disagree
- whether obstacles invalidate the target
- whether the target is realistically supported
- whether uncertainty is too high

Final result:

🎯 ONE TARGET

OR

NO VALID TARGET


============================================================
14. TARGET EVIDENCE PACKAGE
============================================================

Target Analyst should use a structured evidence package.

Potential sources:

Market:
- AllTick
- Twelve Data
- normalized candles
- multi-timeframe candles
- technical indicators
- market structure
- liquidity
- volatility
- KTR

Information:
- economic data
- news
- macro data
- analyst information

Historical:
- relevant historical market context
- previous reactions
- previous levels
- previous structural behavior

Do not blindly send all historical data.

Evidence must be selected by relevance.


============================================================
15. SNAPSHOT
============================================================

Every Target Analysis job must create a snapshot.

Snapshot should include:

- Symbol
- CurrentPrice
- AnalysisTime
- RequestedTimeframe
- AvailableTimeframes
- Current market state
- Candle state
- Data versions
- Evidence IDs
- Provider information
- AI configuration versions
- Prompt versions

The snapshot must represent the information available at the
time the user requested the analysis.


============================================================
16. LOOK-AHEAD PROTECTION
============================================================

Target analysis must not use information that was unavailable
at the analysis time.

Enforce:

Evidence.AvailableAt <= AnalysisTime

Do not allow future market data, future news, or future
revisions to contaminate the analysis snapshot.


============================================================
17. TARGET VALIDATION
============================================================

Before creating the final target, validate:

- target is a valid price
- target is meaningfully different from current price
- target is supported by evidence
- target does not contradict critical structure without
  sufficient justification
- target is not based on stale evidence
- target is not based on fabricated information
- target has acceptable confidence
- target has acceptable risk
- target has valid invalidation conditions

If validation fails:

→ NO VALID TARGET


============================================================
18. TARGET RESULT
============================================================

Successful example:

┌──────────────────────────────────────┐
│ TARGET ANALYST           XAUUSD      │
│                                      │
│ 🎯 TARGET                            │
│ 4,190.00                             │
│                                      │
│ 🛡 INVALIDATION                      │
│ 4,145.00                             │
│                                      │
│ Confidence: High                     │
│ Valid Until: ...                     │
│                                      │
│ Basis:                               │
│ • Liquidity destination              │
│ • Higher-TF structure                │
│ • KTR level                          │
│ • Momentum alignment                 │
│                                      │
│        [ CANCEL TARGET ]             │
└──────────────────────────────────────┘

The exact UI implementation is handled separately.


============================================================
19. NO VALID TARGET
============================================================

When evidence is insufficient:

┌──────────────────────────────────────┐
│ TARGET ANALYST           XAUUSD      │
│                                      │
│ ⚪ NO VALID TARGET                   │
│                                      │
│ Reason: Conflicting evidence         │
│                                      │
│ Target analysis was not activated.   │
└──────────────────────────────────────┘

Never fabricate a target merely because the user clicked the
button.


============================================================
20. ACTIVE TARGET
============================================================

After a valid target is produced:

Status:

ANALYZING
    ↓
SUCCESS
    ↓
ACTIVE

Then:

ACTIVE
    ↓
TARGET HIT
    OR
INVALIDATED
    OR
EXPIRED
    OR
CANCELLED


============================================================
21. TARGET MODEL
============================================================

Create a structured TargetAnalysis model.

Suggested:

TargetAnalysis
├── Id
├── Symbol
├── AnalysisTime
├── CurrentPrice
├── Timeframe
├── TargetPrice
├── InvalidationPrice
├── DirectionContext
├── Confidence
├── ValidUntil
├── EvidenceIds[]
├── SpecialistResultIds[]
├── MasterResultId
├── ReasoningSummary
├── Uncertainty
├── Status
├── Provider
├── Model
├── PromptVersion
├── CreatedAt
├── UpdatedAt
└── EndedAt

DirectionContext is contextual information only.

Do not convert TargetAnalysis into a Full Analyst signal.


============================================================
22. TARGET LIFECYCLE
============================================================

Target lifecycle:

USER CLICK
    ↓
ANALYZING
    ↓
SUCCESS
    ↓
ACTIVE
    ↓
┌───────────────┬───────────────┬───────────────┐
│               │               │
▼               ▼               ▼
TARGET HIT   INVALIDATED     EXPIRED
│               │               │
└───────────────┴───────────────┘
                ↓
             HISTORY


User can also:

ACTIVE
   ↓
[CANCEL TARGET]
   ↓
CANCELLED
   ↓
HISTORY


============================================================
23. CANCEL TARGET
============================================================

The result panel must provide:

[ CANCEL TARGET ]

When cancelled:

- stop active target monitoring
- remove active target UI representation
- remove active chart object
- mark target CANCELLED
- preserve historical record
- preserve evidence
- preserve specialist results
- preserve Master result

Cancellation must not delete history.


============================================================
24. TARGET AND LOCAL ANALYST INDEPENDENCE
============================================================

Local Analyst:

🟢 BUY

Target Analyst:

🎯 4,190

The Target Analyst did not receive the Local BUY as a
decision input.

Likewise:

Local Analyst:

🔴 SELL

Target Analyst:

🎯 4,250

This is valid.

Do not force agreement.


============================================================
25. TARGET AND FULL ANALYST INDEPENDENCE
============================================================

Full Analyst may later produce:

🔴 SELL

Target Analyst may produce:

🎯 4,190

These are separate analytical functions.

Do not allow Full Analyst to overwrite Target Analyst.

Do not allow Target Analyst to overwrite Full Analyst.


============================================================
26. TELEGRAM SCANNER INDEPENDENCE
============================================================

The XAUUSD Telegram Scanner is a separate project.

Target Analyst must NOT:

- send Telegram signals
- consume Telegram signals
- depend on Telegram
- use Telegram's BUY/SELL decision
- generate Telegram alerts

Telegram Scanner remains:

24/7
deterministic
M5
rule-based
no AI decision

It is not part of this phase.


============================================================
27. AI PROVIDER CONFIGURATION
============================================================

Every Target AI workspace requires independent configuration.

Required:

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

Also support:

- Timeout
- Max Tokens
- Temperature
- Enabled


============================================================
28. AI FAILURE HANDLING
============================================================

If a specialist AI fails:

- do not fabricate output
- record failure
- retry according to configuration
- use cached interpretation only when still valid
- reduce confidence if appropriate
- or reject the target

If Master AI fails:

→ NO VALID TARGET

Never generate a fake target.


============================================================
29. TOKEN MANAGEMENT
============================================================

Target Analyst is deep, but it must still be efficient.

Use:

- evidence selection
- deduplication
- compression
- relevance ranking
- cached interpretations
- changed-evidence detection
- structured prompts
- token limits

"All-in analysis" means:

ALL RELEVANT EVIDENCE

not:

ALL RAW DATA.


============================================================
30. TESTING
============================================================

Test:

- target job creation
- snapshot creation
- evidence selection
- evidence validation
- look-ahead protection
- evidence compression
- specialist execution
- specialist failure
- Master synthesis
- conflict detection
- target validation
- no-valid-target state
- one-target-only rule
- target lifecycle
- target hit
- invalidation
- expiration
- cancellation
- history preservation
- provider configuration
- prompt versioning
- token limits
- malformed AI responses
- stale evidence
- missing market data


============================================================
31. COMPLETION CRITERIA
============================================================

Phase 13 is complete when:

1. Target Analyst is user-triggered.
2. One click creates one analysis job.
3. Snapshot is created correctly.
4. Relevant evidence is collected.
5. Evidence is validated.
6. Look-ahead protection works.
7. Evidence is compressed.
8. Eight Target AI workspaces exist.
9. Each workspace has independent configuration.
10. Specialist results are structured.
11. Master AI synthesizes evidence.
12. Master does not simply majority-vote.
13. Conflict detection works.
14. Target validation works.
15. Exactly ONE target can be produced.
16. NO VALID TARGET is supported.
17. Target lifecycle works.
18. Cancel Target works.
19. History is preserved.
20. No BUY/SELL/WAIT decision is generated by Target Analyst.
21. No automatic trading exists.
22. Telegram Scanner remains completely independent.
23. Tests pass.
24. No fabricated target is produced.


============================================================
32. NEXT PHASE
============================================================

After Phase 13 is fully implemented and tested:

→ Complete the independent XAUUSD Telegram Scanner project.

After the Telegram Scanner project is completed:

→ Return to AnalystTradingSystem.

Then begin:

PHASE 14 — FULL ANALYST & AI WORKSPACES


============================================================
END OF PHASE 13
============================================================