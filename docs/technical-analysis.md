# Technical analysis engine

Phase 6 is an application-owned, deterministic analysis layer over Phase 5's
normalized SQL candles. The selected market provider supplies raw evidence; it does not calculate
or own the analytical result. The engine does not call OpenAI, create final
BUY/SELL signals, place orders, or claim profitability.

## Data flow and ownership

```text
completed normalized SQL candles
              |
              v
indicator calculators + price analyzers
              |
              v
typed single-timeframe result
              |
              +--> multi-timeframe comparison
              |
              v
read-only API and analyst console
```

The Application project owns contracts, formulas, readiness, market structure,
support/resistance, price action, confluence, and conflict rules. Infrastructure
only reads stored candles and binds validated settings. The API exposes typed
results. The web console renders evidence without adding strategy logic.

Analysis is calculated on demand and is not persisted. This avoids storing
intermediate values whose meaning changes when configuration changes. The
existing immutable candle rows remain the reproducible source. A later audited
signal or backtest phase may persist versioned snapshots if it needs them.

## Candle eligibility and time safety

For a requested cutoff `T`, the query and analysis both require:

- `IsComplete = true`;
- candle close time less than or equal to `T`;
- valid positive OHLC values and a valid high/low envelope;
- one latest-fetched row for each open time;
- chronological calculation order.

Forming, future, invalid, and duplicate candles are excluded and counted in
diagnostics. Every result carries its data cutoff and last used candle close.
This is the no-look-ahead contract: a historical call sees only evidence that
was complete at that historical cutoff. The same service accepts an explicit
cutoff, so Phase 15 can step through history without a separate calculation
path.

## Indicators and formulas

Defaults are configuration, not trading recommendations.

| Indicator | Default | Calculation | Minimum / ready guidance |
| --- | --- | --- | --- |
| SMA | 20, 50, 200 | arithmetic mean of the last `n` closes | `n` / `n` |
| EMA | 9, 20, 50, 100, 200 | SMA seed, then `EMA = previous + 2/(n+1) * (close - previous)` | `n` / `3n` |
| RSI | 14 | Wilder average gains/losses; `100 - 100/(1 + RS)` | `n+1` / `3n` |
| MACD | 12, 26, 9 | fast EMA minus slow EMA; signal is EMA of MACD; histogram is MACD minus signal | `slow+signal-1` / `3*slow` |
| ATR | 14 | max of range and previous-close gaps, Wilder smoothed | `n` / `3n` |
| ADX/+DI/-DI | 14 | Wilder-smoothed true range and directional movement, then smoothed DX | `2n` / `3n` |
| Bollinger Bands | 20, 2 | SMA middle; population standard deviation; upper/lower `middle +/- k*SD` | `n` / `3n` |
| Stochastic | 14, 3 | `%K = 100*(close-lowest)/(highest-lowest)`; `%D` is the 3-value SMA of `%K` | `K+D-1` / `3K` |

Readiness is explicit: `InsufficientData`, `WarmingUp`, or `Ready`. A flat RSI
series returns 50, a zero stochastic range returns 50, and zero directional
range produces zero DI/DX. Values are never invented to hide missing history.

Formula behavior follows the conventional definitions documented by
[TA-Lib's indicator references](https://ta-lib.github.io/ta-doc/) for
[RSI](https://ta-lib.github.io/ta-doc/indicator/RSI.htm),
[MACD](https://ta-lib.github.io/ta-doc/indicator/MACD.htm),
[ATR](https://ta-lib.github.io/ta-doc/indicator/ATR.htm),
[ADX](https://ta-lib.github.io/ta-doc/indicator/ADX.htm), and
[stochastic](https://ta-lib.github.io/ta-doc/indicator/STOCH.htm). Bollinger
calculations follow John Bollinger's published formulas and use a 20-period
middle band with two population standard deviations by default; bands describe
relative price levels, not standalone signals
([formula sheet](https://www.bollingerbands.com/_files/ugd/58be43_b120ddf0184540608baf19e2c0ae2019.pdf),
[official rules](https://www.bollingerbands.com/bollinger-band-rules)).

### Volume decision

VWAP is intentionally not implemented in Phase 6. OTC XAUUSD volume fields are
provider-specific and do not make tick counts equivalent to centralized
exchange-traded gold volume. AllTick may return zero volume for precious-metal
bars, while market providers expose source-specific volume fields
([MQL5 rate fields](https://www.mql5.com/en/book/advanced/python/python_copyrates)).
Using that data as precise global gold volume would create false precision.

## Candles, structure, and price action

The candle analyzer reports body, wicks, range, body/range, wick/body, and
relative range. It detects Doji, Hammer, Inverted Hammer, Shooting Star,
Hanging Man, bullish/bearish engulfing, Morning/Evening Star, Inside Bar, and
Outside Bar. Each result includes direction, candle time, timeframe, a bounded
0-1 geometry quality, and the conditions that matched. Pattern quality is not
a probability.

Market structure uses a configurable symmetric swing window. New swing highs
and lows are classified as HH, HL, LH, LL, or equal. Recent HH+HL evidence is
bullish structure; LH+LL is bearish; mixed sequences are reported as conflicts.

Support/resistance groups swing prices within a configurable percentage of the
current price. A zone must have the configured minimum touches and includes
lower/upper bounds, center, support/resistance/pivot type, touch count, strength,
timeframe, and distance from price. Zones are ranked by touches and proximity;
they are not exact-price promises.

Price action reports range expansion/contraction, breakouts beyond the prior
lookback, wick rejection, momentum candles, inside ranges, and consolidation.
Every boolean is paired with descriptive evidence in the result.

## Volatility, multi-timeframe context, and confluence

Volatility compares ATR with its recent ATR baseline and also reports the
current range versus its recent average plus Bollinger width. Default ATR
ratios classify `VeryLow <= 0.60`, `Low <= 0.80`, `Normal < 1.20`,
`High < 1.50`, and `VeryHigh >= 1.50`. All thresholds are configurable.

The multi-timeframe endpoint evaluates enabled M1, M5, M15, M30, H1, H4, and
D1 independently at one cutoff. It preserves every available individual result,
then describes alignment as fully bullish, fully bearish, conflicting, or mixed.
Missing timeframe data is skipped; it is never copied from another timeframe.

Confluence is grouped evidence, not a magic score. The result keeps separate
trend, momentum, structure, level, pattern, and volatility lists. Explicit
conflicts include trend versus momentum, trend versus structure, a latest
candlestick pattern opposing trend, RSI versus MACD, and cross-timeframe trend
or trend/momentum disagreement. Consumers can therefore explain both agreement
and disagreement without hiding them behind one arbitrary number.

## API

- `GET /api/analysis/{symbol}/{timeframe}` calculates one timeframe.
- `GET /api/analysis/{symbol}/multi-timeframe` compares enabled timeframes.
- Optional `atUtc` on either route creates a historical cutoff.

Supported timeframes are M1, M5, M15, M30, H1, H4, and D1. Invalid requests
return 400, unavailable history returns 404, and a disabled engine returns 503.
Responses use the standard success/error envelope and correlation ID. They do
not contain execution commands or credentials.

## Configuration

The engine uses `TECHNICAL_ANALYSIS_ENABLED`, symbol, timeframe, history-limit,
indicator-period, structure, level, price-action, and volatility settings in
`.env.example`. Startup validation rejects unsupported timeframes, non-positive
periods, unsafe history limits, invalid MACD ordering, bad zone thresholds, and
non-increasing volatility thresholds. Docker injects the same variables.

## Determinism, caching, and performance

The implementation uses decimal arithmetic for price calculations, stable UTC
ordering, deterministic de-duplication, and no random inputs. Given the same
candles, cutoff, and settings it returns the same analytical evidence (apart
from measured execution duration). Calculators are linear over the bounded
history; no loop performs network or provider calls.

No result cache is enabled in Phase 6. The SQL query is bounded and indexed,
and on-demand calculation avoids stale analysis after a new candle arrives.
Caching may be added later with keys containing symbol, timeframe, last candle,
cutoff, and a configuration/version fingerprint.

The automated performance tests exercise 10,000 in-memory candles and a
10,000-row SQL path. On the local Windows/Docker verification environment on
2026-10-02, the SQL test measured 6,560 ms to batch-sync 10,000 rows, 132 ms to
query them, and 214 ms to calculate the full technical result; total test CPU
was 6,641 ms and working-set delta was 26.3 MB. The in-memory calculator also
passed its two-second and 64 MB bounds. These are engineering measurements, not
profit or market-performance claims.

## Testing and limitations

Unit coverage includes known-value indicators, warm-up behavior, all required
candlestick patterns, structure, grouped levels, price action, volatility,
determinism, no-look-ahead filtering, duplicates, invalid/forming/future data,
empty history, unknown symbol, disabled configuration, and 10,000-candle load.
Integration coverage includes API envelopes, safe errors, historical cutoffs,
and the real SQL query path.

Technical analysis is descriptive and can be wrong. Broker pricing, spreads,
session behavior, history depth, gaps, and configuration all affect results.
Pattern labels and confluence are evidence for later system phases, not advice,
orders, guarantees, or a substitute for independent risk controls.
