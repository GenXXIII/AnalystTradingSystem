# Economic-data architecture

## Scope and boundary

Phase 8 collects and normalizes macroeconomic series. It does not interpret the
data, correlate it with price movement, call AI, generate signals, or trade.
Application owns every contract consumed by the API and future analysis:

```text
FRED series + observations
          |
FredEconomicDataProvider (Infrastructure-only JSON and request syntax)
          |
IEconomicDataProvider (Application-owned records)
          |
bounded synchronization + retry + revision detection
          |
SQL Server current observations + append-only revision history + run state
          |
/api/economic-data
```

FRED-specific response classes are private to the adapter. Domain and
Application do not reference a FRED package or response type. SQL Server is the
source of truth; Phase 8 adds no Redis dependency.

## Series and release events are different

`EconomicSeries` and `EconomicObservation` represent measured time series such
as CPI, the federal funds effective rate, or a Treasury constant-maturity yield.
The pre-existing `EconomicEvent` represents a scheduled release with optional
previous, forecast, and actual fields. FRED series responses do not provide all
of those release-calendar fields, so the Phase 8 adapter does not fabricate or
populate release events.

## Persistence model

| Table | Purpose |
| --- | --- |
| `EconomicSeries` | Provider metadata, frequency, units, category, geography, and active state |
| `EconomicObservations` | One current normalized value per series and observation date |
| `EconomicObservationRevisions` | Append-only prior versions superseded by a later provider fetch |
| `EconomicSyncStates` | Durable per-series cursor, health, error, and latest metrics |
| `EconomicSyncRuns` | Per-attempt range, counts, duration, status, and safe failure detail |

The database unique key `EconomicSeriesId + ObservationDate` protects current
observation identity. Values use `decimal(28,8)` and remain nullable. FRED's
`.` marker is stored as `OriginalValue = "."`, `Value = null`, and
`Status = "Missing"`; it is never converted to zero.

Each current observation also has an `EvidenceRecord`. Its observed timestamp
is the fetch that made the current value available locally. A revision copies
the complete prior value, raw value, status, real-time dates, and fetch time to
`EconomicObservationRevisions` before replacing the current row. This preserves
what the application previously knew and when it knew it.

## Incremental synchronization

The first synchronization starts at the configured historical lookback. Later
runs begin at `LastObservationDate - RevisionLookbackDays`, so recent provider
revisions are detected rather than requesting the full history. Pages are
persisted individually, and both provider page size and maximum pages per
series are bounded. One configured series failing does not prevent subsequent
series from synchronizing.

The overlap detects revisions inside its configured window. Older revisions
require an explicit historical synchronization range; Phase 8 does not ingest
the complete ALFRED vintage archive.

Timeouts, HTTP 429, and server failures receive bounded exponential retry.
`Retry-After` takes precedence when supplied. Authentication and invalid-request
failures are permanent for that run. Existing SQL data is never deleted on a
provider failure. One in-process synchronization gate prevents overlapping
manual and hosted runs; unique SQL indexes remain the integrity boundary.

## Initial XAUUSD macro set

The configured IDs are verified FRED series IDs and remain configuration, not
business logic: `CPIAUCSL`, `CPILFESL`, `PCEPI`, `PCEPILFE`, `UNRATE`, `PAYEMS`,
`FEDFUNDS`, `DGS2`, `DGS5`, `DGS10`, `DGS30`, `GDP`, `GDPC1`, `RSAFS`, `INDPRO`,
and `UMCSENT`. Metadata such as title, frequency, units, adjustment, and provider
update time comes from FRED instead of being duplicated locally.

Official references: [series metadata API](https://fred.stlouisfed.org/docs/api/fred/series.html),
[series observations API](https://fred.stlouisfed.org/docs/api/fred/series_observations.html),
and [FRED API errors and limits](https://fred.stlouisfed.org/docs/api/fred/errors.html).

## Time and look-ahead limitations

`ObservationDate` is the provider's economic period/date. `FetchedAtUtc`,
`CreatedAtUtc`, `UpdatedAtUtc`, and sync timestamps are UTC system times. FRED
observation responses also supply real-time start/end dates, which are retained.

FRED's standard observation endpoint does not give a precise intraday public
release timestamp for every value. Therefore `FetchedAtUtc` is a conservative
local availability bound, not a claim that the value was publicly known at the
start of `ObservationDate`. Future backtests must use revision/fetch or a
dedicated release-calendar source and must not align values to their observation
period as though they were known then.
