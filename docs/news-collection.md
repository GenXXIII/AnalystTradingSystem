# Phase 7 news collection

Phase 7 collects provider news as traceable evidence for XAUUSD analysis. It
does not ask AI to interpret news, calculate sentiment, predict price, create a
trade signal, or place an order. NewsData.io is an Infrastructure provider;
the application owns normalization, relevance rules, storage, state, and HTTP
contracts.

## Flow and boundaries

```text
NewsData.io /latest or /archive
             |
       INewsProvider
             |
 bounded collection + retry + paging
             |
 normalize -> classify -> deduplicate
             |
 SQL Server evidence/article/content + collection state/run
             |
 application-owned /api/news endpoints
```

Only the NewsData adapter knows provider field names and request syntax. The
adapter maps the provider's documented response fields, including
`article_id`, `title`, `link`, source metadata, publication time, country,
category, and pagination token, into `ProviderNewsArticle`. See the official
[response object](https://newsdata.io/blog/news-api-response-object/) and
[latest-news endpoint](https://newsdata.io/blog/latest-news-endpoint/).

The application sends one composite bounded query per page. NewsData documents
`AND`, `OR`, `NOT`, and parentheses in its
[query guide](https://newsdata.io/blog/how-do-q-qintitle-qinmeta-works/).
The adapter validates a conservative 100-character maximum because the live
configured endpoint enforces that request limit; provider plan limits still
apply. Archive requests use `from_date` and
`to_date` only when `NEWS_ARCHIVE_ENABLED=true`; archive availability depends
on the selected plan, as described by the official
[archive endpoint guide](https://newsdata.io/blog/all-about-news-archive-endpoint/).

## Normalized article

Each accepted article records:

- provider and provider article ID;
- normalized title, description, optional permitted content, author/publisher;
- canonical article URL, source name/URL, and optional image URL;
- UTC publication and collection timestamps;
- normalized language, countries, provider categories;
- application categories, extracted named entities, and an explicit relevance level;
- source traceability through `EvidenceRecords` and `DataProviders`.

URLs are required to use HTTP or HTTPS. Fragments and common tracking
parameters are removed, query parameters are ordered, whitespace is collapsed,
language/country/category values are normalized, and impossible future times or
records missing title/source/time/URL are rejected. Paid-only provider request
flags are omitted for plan compatibility; content is persisted only when a
provider response actually supplies permitted content.

## Deterministic relevance

Relevance is transparent, rule-based, and configurable. It is not an AI score
and not a trading recommendation.

| Level | Rule summary |
| --- | --- |
| `VeryHigh` | Gold appears in the title, or Federal Reserve and rate terms both appear in the title |
| `High` | USD, Fed, inflation, employment, or rates appears in the title; or gold appears in supporting text |
| `Medium` | A configured XAUUSD-supporting topic appears in description/content/keywords/categories |
| `Low` | Only a broad provider macro category is present |
| `Irrelevant` | No supported topic or macro context matches |

Application categories cover Gold, USD, Federal Reserve, Inflation,
Employment, Interest Rates, Economy, Central Bank, Geopolitics, and Commodity.
Keyword groups are configured through `NEWS_*_KEYWORDS`. Classification reasons
and matched entities are retained in the normalized application record; no
arbitrary truth or impact score is produced.

## Deduplication and incrementality

Deduplication is applied in memory and again against SQL Server using:

1. provider + provider article ID;
2. normalized canonical URL hash;
3. normalized title + source + UTC publication date fingerprint.

The source is part of the title fingerprint, so separate publishers reporting
the same event remain separate evidence. Unique SQL indexes protect each
stable identity under concurrent or repeated collection.

The first automatic run requests the configured initial lookback. Later runs
start from the last successful collection minus a small overlap. Page tokens
are followed only to the configured maximum, repeated tokens stop the run, and
articles outside the requested range are skipped. Collection state and each
run are durable, so a run left `Running` by a restart is marked `Interrupted`
when the next run begins.

## Retry, limits, cost, and failure behavior

- `NEWS_PAGE_SIZE` and `NEWS_MAXIMUM_PAGES_PER_COLLECTION` cap each run.
- `NEWS_RATE_LIMIT_PER_MINUTE` optionally throttles local requests.
- `NEWS_USE_TIMEFRAME_PARAMETER=false` supports plans that deny server-side
  timeframe filtering; UTC window filtering still occurs locally. Enable it
  only when the provider plan grants that parameter.
- transient timeouts, HTTP 429, and server failures use bounded exponential
  retry; provider `Retry-After` takes precedence.
- authentication, invalid request, invalid payload, timeout, unavailable, and
  rate-limit failures map to stable safe error codes.
- credentials and complete request URIs are never logged.
- duplicate filtering is application-owned, so it does not require the
  provider's paid duplicate-removal parameter.
- list/detail/status reads use SQL and make no NewsData API call.
- the provider health check validates configuration without spending an API request.
- existing news remains queryable after a collection failure.

## Configuration

Keep committed defaults disabled. In the ignored `.env`, provide a real key and
then enable collection:

```text
NEWS_ENABLED=true
NEWS_PROVIDER=NewsData
NEWS_API_KEY=YOUR_NEWSDATA_API_KEY
NEWS_BASE_URL=https://newsdata.io/api/1/
```

The complete bounded/relevance settings are documented in
[configuration](development/configuration.md) and represented with safe guide
values in `.env.example`. Do not enable archive collection unless the provider
plan includes it.

## API

| Endpoint | Behavior |
| --- | --- |
| `GET /api/news/` | Paginated SQL query with time/category/source/relevance/search filters |
| `GET /api/news/latest` | Relevant articles from the last 24 hours |
| `GET /api/news/relevant` | Paginated minimum-relevance query |
| `GET /api/news/{id}` | One normalized article with source traceability |
| `GET /api/news/status` | Safe provider configuration, collection state, and stored count |
| `POST /api/news/collect` | One bounded manual collection run |

All responses use the existing success/error envelope and correlation ID.
Page size is bounded by `NEWS_MAXIMUM_PAGE_SIZE`. Results are ordered newest
first by indexed UTC publication time.

## Verification

Normal tests use deterministic fake provider responses and do not spend API
quota. SQL tests create a disposable database and verify migration, 1,000-row
persistence, duplicate-safe replay, restart recovery, filtered pagination, and
query timing.

The real NewsData test is deliberately opt-in:

```powershell
$env:XAUAI_RUN_NEWSDATA_INTEGRATION = "true"
$env:NEWS_API_KEY = "YOUR_NEWSDATA_API_KEY"
$env:XAUAI_TEST_SQLSERVER_CONNECTION_STRING = "YOUR_SQL_ADMIN_TEST_CONNECTION"
dotnet test tests/XauAi.IntegrationTests `
  --configuration Release `
  --filter FullyQualifiedName~NewsDataLiveIntegrationTests
```

It creates and removes an isolated database and makes at most one provider page
request. Without all three variables it is reported as skipped, not passed.

## Troubleshooting

- `NEWS_DISABLED`: set a real key, verify settings, then set `NEWS_ENABLED=true`.
- `NEWS_AUTHENTICATION_FAILED`: verify the key and provider account.
- `NEWS_RATE_LIMITED`: wait for reset, lower page frequency, or configure the
  plan-appropriate local rate.
- archive request rejected: keep `NEWS_ARCHIVE_ENABLED=false` unless the plan
  grants archive access.
- empty query: inspect `/api/news/status`, collection run metrics, minimum
  relevance, date range, and keyword settings.
- SQL-disabled error: Phase 7 persistence and queries require the database.

Phase 8 consumes this stored evidence only after adding its own economic-data
pipeline. Phase 7 does not implement FRED, economic-event correlation, AI news
analysis, sentiment, causal attribution, signals, backtests, or execution.
