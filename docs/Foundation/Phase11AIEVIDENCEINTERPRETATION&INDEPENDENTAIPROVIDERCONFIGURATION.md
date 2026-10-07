============================================================
PHASE 11 — AI EVIDENCE INTERPRETATION & INDEPENDENT AI
PROVIDER CONFIGURATION
============================================================

Project: AnalystTradingSystem
Phase: 11

IMPORTANT
------------------------------------------------------------

This phase builds the AI evidence-interpretation foundation.

It does NOT implement:

- Local BUY/SELL signals
- Target Analyst
- Full Analyst
- Future scenarios
- Telegram Scanner
- Automatic trading

Those are separate phases/systems.

The independent XAUUSD Telegram Scanner is a completely
separate project and must NOT depend on this phase.


============================================================
1. OBJECTIVE
============================================================

Create a reusable AI evidence interpretation layer that can
take normalized evidence from the project's Evidence Foundation
and convert it into structured, traceable interpretations.

The system must support multiple independent AI workspaces.

AI must interpret evidence.

AI must NOT directly execute trades.

AI must NOT fabricate missing data.


============================================================
2. CORE FLOW
============================================================

Normalized Evidence
        ↓
Evidence Selection
        ↓
Evidence Validation
        ↓
Evidence Compression
        ↓
AI Interpretation
        ↓
Structured Interpretation
        ↓
Evidence Traceability
        ↓
Interpretation Storage
        ↓
Available to later analysts


============================================================
3. AI WORKSPACES
============================================================

Create independent AI workspaces:

1. Structure AI
2. Liquidity AI
3. Candle AI
4. Flow AI
5. KTR AI
6. News AI
7. Risk / Context AI
8. Master AI

Each workspace must have its own responsibility.

Do not make every workspace analyze everything.

Master AI is responsible for synthesizing specialist
interpretations later.


============================================================
4. INDEPENDENT AI PROVIDER CONFIGURATION
============================================================

Every AI workspace must have independent configuration.

Required settings:

- Provider
- API Key
- Model
- Base URL
- Timeout
- Max Tokens
- Temperature
- Enabled

Example:

STRUCTURE_AI_PROVIDER=
STRUCTURE_AI_API_KEY=
STRUCTURE_AI_MODEL=
STRUCTURE_AI_BASE_URL=

LIQUIDITY_AI_PROVIDER=
LIQUIDITY_AI_API_KEY=
LIQUIDITY_AI_MODEL=
LIQUIDITY_AI_BASE_URL=

CANDLE_AI_PROVIDER=
CANDLE_AI_API_KEY=
CANDLE_AI_MODEL=
CANDLE_AI_BASE_URL=

FLOW_AI_PROVIDER=
FLOW_AI_API_KEY=
FLOW_AI_MODEL=
FLOW_AI_BASE_URL=

KTR_AI_PROVIDER=
KTR_AI_API_KEY=
KTR_AI_MODEL=
KTR_AI_BASE_URL=

NEWS_AI_PROVIDER=
NEWS_AI_API_KEY=
NEWS_AI_MODEL=
NEWS_AI_BASE_URL=

RISK_AI_PROVIDER=
RISK_AI_API_KEY=
RISK_AI_MODEL=
RISK_AI_BASE_URL=

MASTER_AI_PROVIDER=
MASTER_AI_API_KEY=
MASTER_AI_MODEL=
MASTER_AI_BASE_URL=

Do not hardcode a specific AI provider.

Do not automatically share credentials between workspaces.

Do not automatically fall back to another workspace's
credentials.


============================================================
5. EVIDENCE SELECTION
============================================================

Never send unlimited raw data to AI.

Select evidence based on:

- relevance
- freshness
- timeframe
- market state
- event importance
- evidence type
- current analysis time

Prefer meaningful evidence over large quantities of data.


============================================================
6. EVIDENCE VALIDATION
============================================================

Before AI receives evidence, validate:

- timestamp
- source
- symbol
- timeframe
- completeness
- freshness
- data quality
- availability time

Critical rule:

Evidence.AvailableAt <= AnalysisTime

This prevents look-ahead bias.

Never allow future information to influence a current or
historical analysis.


============================================================
7. EVIDENCE COMPRESSION
============================================================

Reduce token usage while preserving important information.

Pipeline:

Raw Evidence
    ↓
Normalize
    ↓
Deduplicate
    ↓
Rank
    ↓
Select
    ↓
Compress
    ↓
AI

Do not send thousands of unchanged candles when a structured
summary can represent the relevant information.

Preserve important raw evidence IDs so the interpretation
remains traceable.


============================================================
8. FACT VS INTERPRETATION
============================================================

Clearly separate:

FACT

from

AI INTERPRETATION

Example:

FACT:
US CPI actual = X
Forecast = Y

INTERPRETATION:
The release created stronger-than-expected USD pressure.

The AI must never present its interpretation as if it were
raw market data.


============================================================
9. NEWS REACTION ANALYSIS
============================================================

When analyzing economic/news evidence, support:

- Expected
- Actual
- Previous
- Market reaction
- Expected effect
- Observed effect

Reaction alignment:

- Aligned
- Opposite
- Mixed
- Weak
- Unknown

Do not assume every economic release has a simple
"better data = bearish Gold" relationship.

The interpretation must consider the actual event and
observed XAUUSD reaction.


============================================================
10. INTERPRETATION MODEL
============================================================

Create a structured model similar to:

EvidenceInterpretation

├── Id
├── EvidenceIds[]
├── InterpretationType
├── Direction
├── Impact
├── AffectedAssets[]
├── Mechanism
├── ExpectedEffect
├── ObservedReaction
├── ReactionAlignment
├── CurrentRelevance
├── Confidence
├── Uncertainty
├── Summary
├── Provider
├── Model
├── PromptVersion
├── CreatedAt
└── AnalysisTime


============================================================
11. INTERPRETATION STATES
============================================================

Support lifecycle states:

CURRENT
STALE
SUPERSEDED
INVALID

An interpretation must not remain permanently valid.

Current market conditions can invalidate older interpretations.


============================================================
12. TRACEABILITY
============================================================

Every AI interpretation must be traceable back to its evidence.

Example:

Interpretation
      ↓
Evidence IDs
      ↓
News / Market / Macro / Analyst data
      ↓
Original source
      ↓
Timestamp

Also record:

- AI provider
- AI model
- prompt version
- analysis time
- configuration version


============================================================
13. TOKEN MANAGEMENT
============================================================

The system must minimize unnecessary AI usage.

Implement support for:

- evidence compression
- deduplication
- relevance ranking
- caching
- interpretation reuse
- prompt versioning
- configurable token limits
- deterministic preprocessing

Do not call AI simply because new raw data arrived.

AI interpretation should be based on meaningful evidence
changes.


============================================================
14. FAILURE HANDLING
============================================================

If an AI provider fails:

- do not fabricate an interpretation
- record the failure
- preserve the original evidence
- allow retry where appropriate
- optionally use a still-valid cached interpretation
- mark unavailable results clearly

AI failure must never become fake certainty.


============================================================
15. SEPARATION FROM ANALYSTS
============================================================

Phase 11 produces reusable evidence interpretations.

Later systems consume them.

Example:

Phase 11
    ↓
Structured Evidence Interpretations
    ↓
┌──────────────┬───────────────┐
│              │               │
▼              ▼               ▼
Target       Full            Other
Analyst      Analyst         analysis