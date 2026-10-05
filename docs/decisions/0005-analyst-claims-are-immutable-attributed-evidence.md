# ADR 0005: Analyst claims are immutable attributed evidence

## Status

Accepted for Phase 9.

## Decision

Store analyst publications and structured predictions separately from market
facts and future AI interpretations. Preserve publisher, optional analyst,
publication time, collection time, original URL, claim text, direction, target,
and horizon. A changed publication creates a version; it never rewrites an
earlier prediction. Exact republications are linked and do not become duplicate
independent predictions.

All historical reads require `PublishedAtUtc <= AnalysisTime`. Provider-specific
transport models remain in Infrastructure behind `IAnalystDataProvider`.

## Consequences

Future evaluation can reproduce what information was available at a historical
instant and measure outcomes without hindsight. Phase 9 cannot claim analyst
accuracy or semantic independence for paraphrased publications; those require
later outcome and evidence-normalization work.
