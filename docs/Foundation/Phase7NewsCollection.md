# PHASE 7 — NEWS COLLECTION

Continue building my XAUUSD AI Trading Intelligence System.

Completed:

* Phase 1 — Project Foundation
* Phase 2 — Configuration & Secrets
* Phase 3 — Database Foundation
* Phase 4 — MT5 Integration
* Phase 5 — Market Data Pipeline
* Phase 6 — Technical Analysis Engine

Now implement:

# PHASE 7 — NEWS COLLECTION

The purpose of this phase is to build a reliable, application-owned financial-news collection system.

The external news provider is:

```text
NewsData.io
```

However, NewsData.io is ONLY an external data provider.

The application must NOT expose NewsData.io's interface throughout the project.

---

# 1. CORE ARCHITECTURE

Build:

```text
NewsData.io
     ↓
News Provider Adapter
     ↓
Your INewsProvider interface
     ↓
News Collection Service
     ↓
Normalization
     ↓
Relevance Filtering
     ↓
Deduplication
     ↓
Classification
     ↓
SQL Server
     ↓
Your News API
```

The rest of the application must use YOUR models and interfaces.

Correct:

```text
INewsProvider
      ↓
NewsDataProvider
      ↓
NewsData.io
```

Incorrect:

```text
Application
      ↓
NewsData.io SDK/API everywhere
```

If NewsData.io is replaced later, the rest of the system should not need to be rewritten.

---

# 2. MAIN OBJECTIVE

Collect news relevant to:

```text
XAUUSD
Gold
US Dollar
USD
Federal Reserve
Interest rates
US economy
Inflation
CPI
PCE
Jobs
NFP
Treasury yields
Geopolitical events
Central banks
Major global economic events
```

Do not collect the entire internet.

The system should prioritize information that can reasonably affect gold/XAUUSD.

---

# 3. NEWS PROVIDER

Use the configured NewsData.io API credential from Phase 2.

Configuration should look conceptually like:

```env
NEWSDATA_API_KEY=
```

Do not hardcode the key.

Do not expose the key through:

* frontend
* API response
* logs
* Git
* database
* documentation

The backend alone communicates with NewsData.io.

---

# 4. YOUR OWN PROVIDER INTERFACE

Create an application-owned interface.

For example:

```text
INewsProvider
```

It should support operations such as:

```text
SearchNews
GetLatestNews
GetNewsByDateRange
```

The exact method names should follow the existing architecture.

The provider interface should return YOUR normalized provider-independent models.

Do not return raw NewsData.io response objects throughout the application.

---

# 5. NEWS COLLECTION SERVICE

Create your own:

```text
NewsCollectionService
```

Responsibilities:

```text
Request
 ↓
Receive
 ↓
Validate
 ↓
Normalize
 ↓
Classify
 ↓
Filter
 ↓
Deduplicate
 ↓
Persist
```

Do not put all of this into the API controller.

The controller should remain thin.

---

# 6. NEWS MODEL

Create an application-owned normalized news model.

At minimum support:

```text
NewsArticle
-----------
Id
Provider
ProviderArticleId
Title
Description
Content/Summary
Url
SourceName
Author
PublishedAtUtc
CollectedAtUtc
Language
Country
Category
ImageUrl
```

Only include fields that are actually available and useful.

Do not blindly copy every provider-specific field.

---

# 7. SOURCE INFORMATION

Preserve source information.

For each article, where available:

```text
SourceName
SourceUrl
OriginalArticleUrl
Publisher
```

The purpose is traceability.

We should be able to determine where a piece of information came from.

Do not claim that our system independently verified the article merely because it was collected.

---

# 8. PROVIDER METADATA

Preserve enough provider metadata to identify the original record.

For example:

```text
Provider = NewsData
ProviderArticleId = ...
```

This helps prevent duplicates.

Do not make the entire application dependent on provider-specific identifiers.

---

# 9. UTC TIME

Normalize all news timestamps to UTC.

Store:

```text
PublishedAtUtc
CollectedAtUtc
```

Do not mix:

```text
local time
provider time
UTC
```

The application uses UTC internally.

---

# 10. RELEVANCE FILTERING

Do NOT store every article returned by the provider if it has no analytical value.

Create an application-owned relevance layer.

Potential relevance categories:

```text
Gold
USD
Federal Reserve
Interest Rates
Inflation
Employment
Economic Growth
Treasury/Yields
Central Bank
Geopolitics
Energy
Commodity
Risk Sentiment
```

The filtering system should be configurable.

---

# 11. GOLD/XAUUSD RELEVANCE

An article may be relevant even if it does not literally contain:

```text
XAUUSD
```

For example:

```text
Federal Reserve changes rate expectations
```

may be highly relevant to gold.

Therefore use multiple relevance signals:

```text
Title keywords
Description keywords
Category
Source
Country
Economic topic
Market topic
```

Do not rely on one keyword such as `gold`.

---

# 12. RELEVANCE LEVEL

Create a structured relevance classification.

For example:

```text
VeryHigh
High
Medium
Low
Irrelevant
```

This is a classification for data processing.

It is NOT a trading signal.

Do not say:

```text
VeryHigh = BUY
```

or:

```text
High = SELL
```

---

# 13. KEYWORD SYSTEM

Create configurable keyword groups.

For example:

```text
GOLD:
gold
bullion
precious metals
XAU
XAUUSD

USD:
US dollar
USD
dollar

FED:
Federal Reserve
Fed
FOMC
Powell

INFLATION:
CPI
PCE
inflation

EMPLOYMENT:
NFP
nonfarm payrolls
jobs
unemployment

RATES:
interest rate
rate cut
rate hike
yield
Treasury
```

Do not hardcode all relevance rules inside one huge method.

Keep keyword groups maintainable.

---

# 14. SOURCE QUALITY

Do not automatically assume all news sources have equal quality.

Preserve:

```text
SourceName
Publisher
```

Allow the application to maintain configurable source metadata if useful.

For example:

```text
SourceProfile
----------------
Source
Category
ReliabilityMetadata
Enabled
```

Do NOT create an arbitrary "truth score" without validation.

The purpose is filtering and organization, not pretending we can mathematically determine whether an article is true.

---

# 15. DUPLICATE DETECTION

News providers may return the same story repeatedly.

Implement deduplication.

Use multiple signals where appropriate:

```text
ProviderArticleId
Canonical URL
Normalized URL
Title similarity
Source
Published timestamp
```

Do not rely only on the title.

Two different articles can have similar titles.

---

# 16. DUPLICATE STORIES FROM DIFFERENT SOURCES

The same event may appear in several news articles.

For example:

```text
Source A → Fed announcement
Source B → Fed announcement
Source C → Fed announcement
```

Do NOT automatically delete all three.

There are two different concepts:

```text
Duplicate article
```

versus:

```text
Same underlying event
```

Phase 7 should primarily solve duplicate ARTICLE storage.

Event clustering can be introduced carefully for later analysis.

---

# 17. NEWS EVENT CONCEPT

Prepare the architecture so later phases can group articles around an event.

Conceptually:

```text
Article A ─┐
Article B ─┼──→ Event Cluster
Article C ─┘
```

Do not build an unnecessarily sophisticated event-intelligence system yet.

Create only the foundation needed for future phases.

---

# 18. COLLECTION WINDOWS

Support collection by time range.

For example:

```text
FromUtc
ToUtc
```

The system should be able to request:

```text
Recent news
```

and:

```text
Historical news
```

where the provider supports it.

Do not request unlimited historical data.

---

# 19. INCREMENTAL COLLECTION

Implement incremental collection.

Conceptually:

```text
Last successful collection
          ↓
Determine collection window
          ↓
Request new articles
          ↓
Normalize
          ↓
Deduplicate
          ↓
Store
```

Do not repeatedly download the entire news history.

---

# 20. COLLECTION STATE

Track collection state.

For example:

```text
NewsCollectionState
-------------------
Provider
LastSuccessfulCollectionUtc
LastAttemptUtc
Status
ArticlesReceived
ArticlesInserted
ArticlesSkipped
ArticlesRejected
Error
```

This makes the system recoverable.

Do not rely only on logs.

---

# 21. SCHEDULING

Implement a background collection mechanism using the existing application architecture.

Conceptually:

```text
Application
   ↓
News Collection Worker
   ↓
NewsData.io
   ↓
Normalize
   ↓
Store
   ↓
Wait
   ↓
Repeat
```

The interval must be configurable.

Do not create a distributed scheduling system for the current project.

---

# 22. API LIMIT AWARENESS

NewsData.io usage is limited by the user's plan.

The system must avoid wasteful API requests.

Important:

```text
DO NOT
request the same news repeatedly
request huge unnecessary ranges
request every possible keyword independently
send duplicate requests
```

Use efficient queries and local filtering where practical.

This is especially important because the project is designed to keep external API costs low.

---

# 23. API FAILURE HANDLING

Handle:

```text
Timeout
Rate limit
Authentication failure
Provider unavailable
Invalid request
Network error
Malformed response
```

The application must not crash because NewsData.io is temporarily unavailable.

Existing news data remains available.

---

# 24. RATE LIMIT HANDLING

When the provider indicates rate limiting:

```text
Do not immediately retry continuously.
```

Implement bounded retry/backoff.

Respect provider rate-limit information when available.

Do not create retry storms.

---

# 25. NEWS STORAGE

Store normalized news in SQL Server.

The database should support efficient queries by:

```text
PublishedAtUtc
CollectedAtUtc
Symbol/relevance
Category
Source
```

Create appropriate indexes.

Do not create excessive indexes without measuring query patterns.

---

# 26. NEWS SEARCH

Provide application-owned queries such as:

```text
Get latest relevant news
Get news by date range
Get news by category
Get news by source
Get news related to gold
Get news related to USD
Get high-relevance news
```

These are data-access operations.

They are NOT AI analysis.

---

# 27. API

Create your own API.

For example:

```text
GET /api/news
GET /api/news/latest
GET /api/news/relevant
GET /api/news/{id}
GET /api/news/status
```

Use the project's existing API conventions.

Do not expose NewsData.io's raw API structure.

---

# 28. PAGINATION

News endpoints must use pagination.

Do not return thousands of articles in one response.

Support:

```text
page
pageSize
```

or cursor pagination if that fits the architecture better.

Enforce a maximum page size.

---

# 29. SORTING

Default ordering should generally be:

```text
PublishedAtUtc DESC
```

Allow only safe, supported sort fields.

Do not dynamically concatenate user input into SQL.

Use the existing data-access architecture.

---

# 30. CONTENT STORAGE

Be careful with full article content.

Do not assume that every provider response gives full article text.

Store only content that the provider legally/technically provides for our use.

Prefer:

```text
Title
Description
Available summary
Source URL
```

when full content is not available.

Do not scrape arbitrary websites to bypass provider limitations.

---

# 31. COPYRIGHT / SOURCE TRACEABILITY

Preserve the original source URL.

Do not transform the system into a database of copied articles unnecessarily.

The system is collecting information for analysis and traceability.

Keep provider/source attribution where appropriate.

---

# 32. LANGUAGE

Prioritize English financial news initially.

Keep the architecture extensible for additional languages.

Do not implement expensive translation infrastructure in Phase 7 unless there is a concrete requirement.

---

# 33. NEWS NORMALIZATION

Normalize:

```text
Whitespace
URLs
Timestamps
Source names
Categories
Country codes
Language codes
Keyword matching
```

Do not modify the actual meaning of the article.

---

# 34. NEWS CLASSIFICATION

Classify articles into useful categories.

For example:

```text
Gold
USD
Fed
Inflation
Employment
InterestRates
Economy
Geopolitics
Markets
Commodities
Other
```

An article can potentially belong to multiple categories.

Do not force an article into exactly one category if that loses useful information.

---

# 35. ENTITY TAGGING

Where practical, identify entities such as:

```text
Gold
USD
Federal Reserve
Jerome Powell
FOMC
CPI
NFP
Treasury
ECB
BOJ
China
```

Keep this rule-based initially.

Do NOT use OpenAI for every article.

AI-based entity extraction can be considered later if testing shows that it provides enough value to justify the cost.

---

# 36. IMPORTANT COST REQUIREMENT

This project is specifically designed to use as little paid AI/API usage as practical while maintaining strong analysis.

Therefore:

```text
NewsData.io
     ↓
Local filtering
     ↓
Local deduplication
     ↓
Local classification
     ↓
Only important articles
     ↓
Phase 11 AI analysis
```

Do NOT send every collected article to OpenAI.

Phase 11 will determine an efficient AI-selection strategy.

---

# 37. NEWS IMPORTANCE

Create a structured importance model based on deterministic information first.

Potential factors:

```text
Topic relevance
Source
Recency
Economic importance
Gold relationship
USD relationship
Fed relationship
Potential market impact
Duplicate status
```

Do not invent arbitrary weights that pretend to represent proven market impact.

Keep the factors transparent and configurable.

---

# 38. NEWS FRESHNESS

Create freshness information.

For example:

```text
VeryRecent
Recent
Older
Historical
```

Use timestamps.

Do not treat old news as breaking news.

---

# 39. MARKET SESSION CONTEXT

Prepare the news model so later phases can compare news with market conditions.

For example:

```text
NewsPublishedAtUtc
MarketCandleAtSameTime
```

Do not perform the correlation analysis yet.

That belongs to later analysis phases.

---

# 40. NEWS + MARKET DATA RELATIONSHIP

Do NOT yet determine:

```text
News caused XAUUSD to rise
```

That is a difficult causal claim.

Phase 7 should only collect and timestamp the information accurately.

Later phases can investigate relationships between:

```text
News
+
Market movement
```

using historical analysis.

---

# 41. HEALTH STATUS

Expose news-provider health.

For example:

```text
News Provider
Status: Healthy

Last successful collection:
2026-10-02 08:30 UTC

Last collection:
2026-10-02 08:31 UTC

Articles received:
50

Articles stored:
17
```

Do not expose API credentials.

---

# 42. OBSERVABILITY

Track:

```text
Requests
Successful requests
Failed requests
Rate-limit responses
Articles received
Articles inserted
Duplicates
Rejected articles
Collection duration
Last successful collection
```

Do not log complete article content unnecessarily.

---

# 43. CACHING

Do not cache the entire news database in Redis.

SQL Server remains the source of truth.

Redis may later cache:

```text
Latest relevant news
Small frequently requested result sets
News status
```

Only add caching where it improves measured performance.

---

# 44. TESTING

Create tests for:

## Provider

```text
[ ] Successful request
[ ] Authentication failure
[ ] Timeout
[ ] Rate limit
[ ] Invalid response
[ ] Provider unavailable
```

## Normalization

```text
[ ] Timestamp conversion
[ ] URL normalization
[ ] Source normalization
[ ] Category normalization
[ ] Missing fields
```

## Filtering

```text
[ ] Gold relevance
[ ] USD relevance
[ ] Fed relevance
[ ] Inflation relevance
[ ] Employment relevance
[ ] Irrelevant article
```

## Deduplication

```text
[ ] Same provider ID
[ ] Same URL
[ ] Normalized URL
[ ] Similar article
[ ] Same event from different sources
```

## Storage

```text
[ ] Insert
[ ] Duplicate protection
[ ] Query by date
[ ] Query by category
[ ] Pagination
```

## Synchronization

```text
[ ] Initial collection
[ ] Incremental collection
[ ] Restart
[ ] Provider failure
[ ] Retry
[ ] Rate limit
```

---

# 45. INTEGRATION TEST

Create a provider integration test that can run when the real NewsData.io API key is configured.

Normal unit tests must NOT require the real API key.

Use an explicit integration-test configuration.

Test:

```text
NewsData.io
   ↓
Your provider adapter
   ↓
Normalization
   ↓
Filtering
   ↓
Deduplication
   ↓
SQL Server
```

Do not send the collected articles to OpenAI.

---

# 46. PERFORMANCE TESTING

Measure:

```text
Collection duration
Database insert duration
Database query duration
Memory usage
Number of provider requests
Articles processed per request
```

Pay special attention to unnecessary provider requests.

The goal is:

```text
Maximum useful information
with minimum unnecessary API usage
```

---

# 47. RESTART TEST

Test:

```text
Application starts
 ↓
News collection starts
 ↓
Application stops
 ↓
Application starts
 ↓
Collection resumes
```

Verify:

* no massive duplicate collection
* existing news remains intact
* collection state remains valid
* new news is collected
* provider failure does not corrupt data

---

# 48. SECURITY

Never expose:

```text
NEWSDATA_API_KEY
MT5_PASSWORD
FRED_API_KEY
OPENAI_API_KEY
```

Do not put API keys in:

```text
frontend
Git
logs
database
API responses
documentation
```

---

# 49. DOCUMENTATION

Create/update:

```text
docs/news-collection.md
```

Document:

* NewsData.io integration
* application-owned interfaces
* configuration
* collection process
* relevance filtering
* deduplication
* categories
* synchronization state
* rate limits
* retry behavior
* database storage
* API endpoints
* troubleshooting
* cost-conscious design

---

# 50. DO NOT IMPLEMENT LATER PHASES

Do NOT implement:

* AI news analysis
* sentiment AI
* OpenAI calls
* master AI
* market prediction
* trading signals
* backtesting
* automatic trading
* final news impact scores
* causal claims about news and price

Those belong to later phases.

---

# 51. DEFINITION OF DONE

Phase 7 is complete only when:

* [ ] NewsData.io integration works
* [ ] Application-owned INewsProvider exists
* [ ] NewsData-specific implementation is isolated
* [ ] News is normalized
* [ ] UTC timestamps are consistent
* [ ] XAUUSD-relevant filtering exists
* [ ] Gold/USD/Fed/inflation/employment topics are supported
* [ ] News categories exist
* [ ] Duplicate articles are prevented
* [ ] Incremental collection works
* [ ] Collection state is tracked
* [ ] Retry/backoff exists
* [ ] Rate limits are respected
* [ ] News is persisted
* [ ] News search/query service exists
* [ ] API endpoints exist
* [ ] Pagination exists
* [ ] Provider health status exists
* [ ] Logging/observability exists
* [ ] Unit tests pass
* [ ] Integration tests exist
* [ ] Failure tests pass
* [ ] Restart test passes
* [ ] Performance testing is performed
* [ ] Documentation is updated
* [ ] OpenAI is NOT called
* [ ] No trading signals are generated

---

# 52. FINAL REPORT

After implementation, provide:

1. Files created
2. Files modified
3. News architecture
4. NewsData.io adapter
5. Normalized news model
6. Relevance filtering
7. Deduplication
8. Categories
9. Incremental collection
10. Rate-limit handling
11. Database changes
12. API endpoints
13. Tests executed
14. Test results
15. Performance measurements
16. API request-efficiency measurements
17. Any issue requiring my action
18. Confirmation that Phase 7 is complete
19. Short explanation of what Phase 8 will build

Do not claim a test passed unless it was actually executed.

Do not implement Phase 8 early.

# END OF PHASE 7
