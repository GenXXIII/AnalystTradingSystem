# Evidence normalization and API

## Normalization rules

The application normalizer accepts provider-neutral `EvidenceInput` records.
It trims and collapses text, converts timestamps to UTC, validates required
source/instrument/time fields, rejects unsupported aliases and timeframes,
checks numeric and JSON values, validates optional URLs and currency codes, and
preserves each original representation beside its normalized value.

| Input | Normalized value |
| --- | --- |
| `XAUUSD`, `XAU/USD`, `GOLD`, `Gold`, `GOLDUSD`, `XAU` | `XAUUSD` |
| `1m`, `5min`, `15m`, `30m`, `1h`, `4h`, `daily` | `M1`, `M5`, `M15`, `M30`, `H1`, `H4`, `D1` |
| `BUY`, `LONG`, `UP`, `POSITIVE` | `Bullish` |
| `SELL`, `SHORT`, `DOWN`, `NEGATIVE` | `Bearish` |
| `NEUTRAL`, `SIDEWAYS`, `RANGE` | `Neutral` |
| Ambiguous or missing direction | `Unknown` |
| `%`, `USD`, index, billions, trillions, thousands, millions, bps | Controlled unit |

W1 and MN1 are accepted for forward-compatible context. Categories map by
explicit terms into Fed, interest rates, inflation, employment, GDP, USD,
Treasury, geopolitical, central bank, commodity, market structure, liquidity,
technical, or session; unmatched input is `Other`. Original category,
importance, unit, direction, symbol, value, source URL, and provider metadata
remain available for audits.

The generic ingestion path rejects availability timestamps in the future.
Historical query endpoints independently reject future `asOf` or
`analysisTime` values. Invalid ingestion records are written to
`EvidenceQuarantineRecords` with a payload hash, safe error code/message,
source identity, and original timestamp instead of being silently discarded.

## Deduplication and batching

Ingestion processes configurable bounded batches. Identity is:

```text
stable external ID present: SHA-256(source key, external ID)
otherwise:                 SHA-256(source key, instrument, event time, content hash)
```

The content fingerprint includes normalized instrument, timeframe, event time,
title, summary, value, unit, direction, and category. Replaying an input leaves
the database unchanged. Same-source exact content is deduplicated; different
sources remain separately attributed and get a `Republished` relation.

## APIs

All routes return the standard API success/error envelope and correlation ID.

| Route | Purpose |
| --- | --- |
| `GET /api/evidence` | Paginated evidence with filters and an optional historical `asOf` |
| `GET /api/evidence/{id}` | One available record with relations and clusters |
| `GET /api/evidence/pack` | Look-ahead-safe, multi-source, multi-timeframe evidence pack |
| `GET /api/evidence/conflicts` | Deterministic directional disagreements without resolution |
| `GET /api/evidence/sources` | Paginated source coverage and latest availability |

`GET /api/evidence` accepts `instrument`, `evidenceType`, `sourceType`, `from`,
`to`, `timeframe`, `direction`, `importance`, `relevantOnly`, `asOf`, `page`,
and `pageSize`.

Example historical pack request:

```http
GET /api/evidence/pack?instrument=XAUUSD&primaryTimeframe=M15&confirmationTimeframe=M5&analysisTime=2026-10-04T01:00:00Z&lookbackDays=30
```

Conceptual response body inside the standard envelope:

```json
{
  "instrument": "XAUUSD",
  "analysisTimeUtc": "2026-10-04T01:00:00Z",
  "fromUtc": "2026-09-04T01:00:00Z",
  "primaryTimeframe": "M15",
  "confirmationTimeframe": "M5",
  "evidence": [
    { "evidenceType": "Market", "items": [] },
    { "evidenceType": "News", "items": [] },
    { "evidenceType": "Economic", "items": [] },
    { "evidenceType": "Analyst", "items": [] }
  ],
  "marketTimeframes": [
    { "timeframe": "H1", "role": "Context", "items": [] },
    { "timeframe": "M15", "role": "Primary", "items": [] },
    { "timeframe": "M5", "role": "Confirmation", "items": [] }
  ],
  "conflicts": [],
  "coverage": [
    { "evidenceType": "OrderFlow", "state": "Unavailable", "recordCount": 0 }
  ]
}
```

The concrete response contains all controlled evidence-type groups, even when
empty. JSON enum formatting follows the API's established serializer settings.
The response is evidence only; it contains no BUY/SELL/WAIT decision.

## Configuration

Defaults are startup-validated and can be overridden through native .NET
configuration or these friendly environment variables:

| Variable | Default | Valid range |
| --- | ---: | ---: |
| `EVIDENCE_MAXIMUM_PAGE_SIZE` | 200 | 1-1000 |
| `EVIDENCE_MAXIMUM_QUERY_RANGE_DAYS` | 365 | 1-3650 |
| `EVIDENCE_DEFAULT_PACK_LOOKBACK_DAYS` | 30 | 1-3650 and no more than maximum |
| `EVIDENCE_MAXIMUM_PACK_LOOKBACK_DAYS` | 730 | 1-3650 |
| `EVIDENCE_MAXIMUM_PACK_ITEMS_PER_TYPE` | 100 | 1-1000 |
| `EVIDENCE_INGESTION_BATCH_SIZE` | 250 | 1-5000 |
| `EVIDENCE_CONFLICT_WINDOW_HOURS` | 24 | 1-720 |
| `EVIDENCE_MAXIMUM_CONFLICTS` | 200 | 1-5000 |

No Phase 10 setting is secret. Existing provider credentials remain server-side
and are never included in evidence DTOs or logs.

## Verification

Run deterministic tests without live providers:

```powershell
dotnet test tests/XauAi.UnitTests --configuration Release
dotnet test tests/XauAi.ArchitectureTests --configuration Release
dotnet test tests/XauAi.IntegrationTests --configuration Release
```

For the SQL-backed end-to-end evidence test, set
`XAUAI_RUN_SQL_INTEGRATION=true` and `XAUAI_TEST_SQL_CONNECTION_STRING` to a
disposable migrated database, then run the integration project. The scenario
ingests market, news, economic, and analyst records; verifies quarantine,
idempotency, republication, conflict and cluster relationships; and proves a
future record is absent from a historical pack.

