"use client";

import { useMarketData } from "@/hooks/use-market-data";
import type { MarketCandle } from "@/types/market";

const priceFormatter = new Intl.NumberFormat("en-US", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 3,
});

const integerFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 0,
});

const utcFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "UTC",
  timeZoneName: "short",
});

export function MarketWorkspace() {
  const { state, provider, quote, candles, error, refresh } = useMarketData();
  const recentCandles = candles.slice(-8).reverse();
  const spread = quote ? quote.ask - quote.bid : null;

  return (
    <section className="market-workspace" id="market" aria-labelledby="market-title">
      <div className="workspace-heading">
        <div>
          <p className="section-label">Market workspace</p>
          <h2 id="market-title">XAUUSD evidence desk</h2>
          <p>
            Monitor the current normalized quote and inspect recent H1 evidence.
          </p>
        </div>
        <button
          className="refresh-button"
          type="button"
          onClick={refresh}
          disabled={state === "loading"}
        >
          {state === "loading" ? "Refreshing…" : "Refresh data"}
        </button>
      </div>

      <div className="market-grid" aria-live="polite">
        <article className="market-card connection-card">
          <p className="card-kicker">Market feed</p>
          <div className="connection-heading">
            <span className="live-dot" data-state={provider?.connected ? "online" : "offline"} />
            <strong>{provider?.connected ? "Connected" : state === "loading" ? "Checking" : "Unavailable"}</strong>
          </div>
          <p className="provider-message">
            {provider?.message ?? "Checking the market-data adapter."}
          </p>
          <dl className="detail-list">
            <div>
              <dt>Application instrument</dt>
              <dd>{provider?.applicationSymbol ?? "XAUUSD"}</dd>
            </div>
            <div>
              <dt>Data provider</dt>
              <dd>{provider?.provider ?? "MT5 adapter"}</dd>
            </div>
            <div>
              <dt>Provider mapping</dt>
              <dd>{provider?.providerSymbol ?? "—"}</dd>
            </div>
            <div>
              <dt>Account</dt>
              <dd>{provider?.accountLogin ?? "Not exposed"}</dd>
            </div>
          </dl>
        </article>

        <article className="market-card quote-card">
          <div className="quote-header">
            <div>
              <p className="card-kicker">Live quote</p>
              <p className="quote-symbol">{quote?.symbol ?? "XAUUSD"}</p>
            </div>
            <span className="read-only-badge">Read only</span>
          </div>
          <div className="quote-pair">
            <div>
              <span>Bid</span>
              <strong>{quote ? priceFormatter.format(quote.bid) : "—"}</strong>
            </div>
            <div>
              <span>Ask</span>
              <strong>{quote ? priceFormatter.format(quote.ask) : "—"}</strong>
            </div>
          </div>
          <div className="quote-meta">
            <span>Spread {spread === null ? "—" : priceFormatter.format(spread)}</span>
            <span>{quote ? formatUtc(quote.timestampUtc) : "Waiting for quote"}</span>
          </div>
        </article>
      </div>

      <article className="candle-panel">
        <div className="candle-heading">
          <div>
            <p className="card-kicker">Recent evidence</p>
            <h3>Hourly price candles</h3>
          </div>
          <span>H1 · UTC · latest 24 hours</span>
        </div>
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th scope="col">Open time</th>
                <th scope="col">Open</th>
                <th scope="col">High</th>
                <th scope="col">Low</th>
                <th scope="col">Close</th>
                <th scope="col">Ticks</th>
                <th scope="col">State</th>
              </tr>
            </thead>
            <tbody>
              {recentCandles.map((candle) => (
                <CandleRow key={candle.openTimeUtc} candle={candle} />
              ))}
              {recentCandles.length === 0 ? (
                <tr>
                  <td className="empty-row" colSpan={7}>
                    {state === "loading" ? "Loading market candles…" : error ?? "No candles are available."}
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </article>

      {error && recentCandles.length > 0 ? <p className="market-error">{error}</p> : null}
    </section>
  );
}

function CandleRow({ candle }: Readonly<{ candle: MarketCandle }>) {
  const direction = candle.close >= candle.open ? "price-up" : "price-down";

  return (
    <tr>
      <td>{formatUtc(candle.openTimeUtc)}</td>
      <td>{priceFormatter.format(candle.open)}</td>
      <td>{priceFormatter.format(candle.high)}</td>
      <td>{priceFormatter.format(candle.low)}</td>
      <td className={direction}>{priceFormatter.format(candle.close)}</td>
      <td>{candle.tickVolume === null ? "—" : integerFormatter.format(candle.tickVolume)}</td>
      <td><span className="candle-state">{candle.isComplete ? "Closed" : "Forming"}</span></td>
    </tr>
  );
}

function formatUtc(value: string): string {
  return utcFormatter.format(new Date(value));
}
