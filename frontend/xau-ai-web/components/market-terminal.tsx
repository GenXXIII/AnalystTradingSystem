"use client";

import { useEffect, useState, type ReactNode } from "react";
import { marketTimeframes, type MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import { useMarketTerminal } from "@/hooks/use-market-terminal";
import { useNewsModule } from "@/hooks/use-news-module";
import { TradingChart } from "@/components/trading-chart";
import { getXauUsdMarketSession } from "@/lib/market/xauusd-session";
import type { MultiTimeframeAnalysis, TechnicalAnalysis } from "@/types/analysis";
import type { StoredMarketCandle } from "@/types/market";
import type { NewsSystemStatus, PagedNewsArticles } from "@/types/news";

const price = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const integer = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 });
const compact = new Intl.NumberFormat("en-US", { notation: "compact", maximumFractionDigits: 1 });
const utcDate = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit", hour12: false, timeZone: "UTC",
});
const utcTime = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false, timeZone: "UTC",
});
const utcPlusSevenTime = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false, timeZone: "Asia/Bangkok",
});
const utcShortDate = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit", month: "short", timeZone: "UTC",
});
const MILLISECONDS_PER_DAY = 86_400_000;
const timeframeLabels: Record<MarketTimeframeCode, string> = {
  M1: "M1", M5: "M5", M15: "M15", M30: "M30", H1: "1H", H4: "4H", D1: "1D",
};
const moduleLaunchers = [
  { key: "analysis", eyebrow: "Analysis", title: "Application-owned engine" },
  { key: "news", eyebrow: "Intelligence", title: "News" },
  { key: "candles", eyebrow: "Market data", title: "Candle feed" },
  { key: "quality", eyebrow: "Operations", title: "Provider & storage" },
] as const;
type ModuleKey = (typeof moduleLaunchers)[number]["key"];

export function MarketTerminal() {
  const terminal = useMarketTerminal();
  const [now, setNow] = useState<Date | null>(null);
  const [selected, setSelected] = useState<string | null>(null);
  const [activeModule, setActiveModule] = useState<ModuleKey | null>(null);
  const news = useNewsModule(activeModule === "news");

  useEffect(() => {
    const initialTimer = window.setTimeout(() => setNow(new Date()), 0);
    const timer = window.setInterval(() => setNow(new Date()), 1_000);
    return () => {
      window.clearTimeout(initialTimer);
      window.clearInterval(timer);
    };
  }, []);
  useEffect(() => {
    if (!activeModule) return;
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setActiveModule(null);
    };
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [activeModule]);

  const selectedCandle = terminal.candles.find((item) => item.openTimeUtc === selected)
    ?? terminal.candles[0]
    ?? null;
  const availability = terminal.pipeline?.availability;
  const synchronization = terminal.pipeline?.synchronization;
  const spread = terminal.quote ? terminal.quote.ask - terminal.quote.bid : null;
  const completedVisible = terminal.candles.filter((item) => item.isComplete).length;
  const providerName = terminal.provider?.provider ?? "AllTick";
  const systemState = terminal.provider?.connected ? "Operational" : terminal.state === "loading" ? "Checking" : "Attention";
  const marketSession = now ? getXauUsdMarketSession(now) : null;
  const weeklyRangeLabel = now ? formatWeeklyRange(now) : "7D view";
  const changeTimeframe = (timeframe: MarketTimeframeCode) => {
    setSelected(null);
    terminal.setTimeframe(timeframe);
  };
  const activeModuleDefinition = moduleLaunchers.find((item) => item.key === activeModule) ?? null;
  const activeModuleMessage = activeModule === "news"
    ? news.error ?? news.status?.provider.message ?? "Latest normalized news from SQL storage."
    : activeModule === "candles"
      ? `${terminal.timeframe} · ${integer.format(terminal.candles.length)} candles loaded from SQL storage.`
      : terminal.error ?? terminal.analysisError ?? "Module data is live and read only.";

  return (
    <main className="terminal-shell">
      <section className="terminal-stage">
        <header className="market-header">
          <div className="instrument-identity">
            <span className="instrument-mark">Au</span>
            <div><strong>XAU/USD</strong><small>Spot gold · analysis only</small></div>
          </div>
          {moduleLaunchers.map((module) => (
            <button className="module-launcher" type="button" onClick={() => setActiveModule(module.key)} aria-haspopup="dialog" key={module.key}>
              <span>{module.eyebrow}</span><strong>{module.title}</strong>
            </button>
          ))}
          <div className="utc-clock"><span>UTC</span><strong>{now ? utcTime.format(now) : "--:--:--"}</strong></div>
          <div className="utc-clock"><span>UTC+7</span><strong>{now ? utcPlusSevenTime.format(now) : "--:--:--"}</strong></div>
        </header>

        <div className="terminal-grid">
          <section className="center-stack">
            <article className="terminal-panel chart-panel">
              <PanelHeader eyebrow="Price evidence" title="Market chart">
                <span className={`state-chip ${terminal.provider?.connected ? "complete" : "negative"}`}>{terminal.provider?.connected ? `${providerName} live` : `${providerName} waiting`}</span>
                <span className={`state-chip ${marketSession === null ? "neutral" : marketSession.isOpen ? "complete" : "negative"}`}>{marketSession === null ? "Market checking" : `Market ${marketSession.isOpen ? "open" : "closed"} · ${marketSession.nextTransitionLocalLabel}`}</span>
              </PanelHeader>
              <div className="chart-toolbar">
                <div className="chart-timeframes" aria-label="Chart timeframe">
                  {marketTimeframes.map((timeframe) => (
                    <button type="button" className={terminal.timeframe === timeframe ? "active" : ""} onClick={() => changeTimeframe(timeframe)} aria-pressed={terminal.timeframe === timeframe} key={timeframe}>
                      {timeframeLabels[timeframe]}
                    </button>
                  ))}
                </div>
                <span className="chart-context">XAUUSD</span><span className="chart-context">UTC</span><span className="chart-context" title="Seven-day history loaded; chart opens in recent focus">{weeklyRangeLabel}</span>
                <span className="chart-source">SQL history | AllTick live forming candle</span>
              </div>
              <TradingChart
                candles={terminal.candles}
                quote={terminal.quote}
                providerConnected={Boolean(terminal.provider?.connected)}
                timeframe={terminal.timeframe}
                nowUtcMilliseconds={now?.getTime() ?? null}
                marketSession={marketSession}
              />
            </article>
          </section>

          <aside className="right-stack">
            <article className="terminal-panel quality-panel">
              <PanelHeader eyebrow="Provider and storage" title="Data quality" />
              <div className="connection-row">
                <StatusDot online={Boolean(terminal.provider?.connected)} />
                <div><strong>{terminal.provider?.connected ? `${providerName} connected` : `${providerName} waiting`}</strong><small>{terminal.provider?.message ?? "Checking provider connection…"}</small></div>
              </div>
              <dl className="terminal-details">
                <Detail label="Internal symbol" value={terminal.provider?.applicationSymbol ?? "XAUUSD"} />
                <Detail label="Provider symbol" value={terminal.provider?.providerSymbol ?? "—"} />
                <Detail label="Pipeline status" value={synchronization?.status ?? "Not started"} tone={statusTone(synchronization?.status)} />
                <Detail label="Visible completed" value={integer.format(completedVisible)} />
                <Detail label="Visible forming" value={integer.format(terminal.candles.length - completedVisible)} />
                <Detail label="Consecutive failures" value={integer.format(synchronization?.consecutiveFailures ?? 0)} tone={synchronization?.consecutiveFailures ? "negative" : undefined} />
                <Detail label="Gap candidates" value={integer.format(synchronization?.detectedGapCount ?? 0)} tone={synchronization?.detectedGapCount ? "warning" : undefined} />
              </dl>
              <p className="quality-note">A missing interval is reported for review. The system never fabricates a candle.</p>
            </article>
          </aside>
        </div>

        <footer className="status-bar">
          <span><StatusDot online={Boolean(terminal.provider?.connected)} />System {systemState}</span>
          <span>SQL Server · source of truth</span><span>AllTick live · Twelve Data reference</span>
          <span className="status-right">Phase 6 · Technical analysis engine</span>
        </footer>
      </section>

      {activeModule && activeModuleDefinition ? (
        <div className="control-modal-backdrop" role="presentation" onMouseDown={() => setActiveModule(null)}>
          <section className="control-modal module-modal" role="dialog" aria-modal="true" aria-labelledby="module-modal-title" onMouseDown={(event) => event.stopPropagation()}>
            <header className="control-modal-header">
              <div><span>{activeModuleDefinition.eyebrow}</span><h2 id="module-modal-title">{activeModuleDefinition.title}</h2><p>Live operational detail without leaving the analyst workspace.</p></div>
              <button type="button" onClick={() => setActiveModule(null)} autoFocus aria-label={`Close ${activeModuleDefinition.title}`}>×</button>
            </header>
            <div className={`control-modal-body module-modal-body module-${activeModule}`}>
              {activeModule === "analysis" ? (
                <TechnicalAnalysisPanel
                  analysis={terminal.analysis?.timeframe === terminal.timeframe ? terminal.analysis : null}
                  multiTimeframe={terminal.multiTimeframe}
                  error={terminal.analysisError}
                  loading={terminal.state === "loading"}
                />
              ) : null}
              {activeModule === "news" ? <NewsModule articles={news.articles} status={news.status} loading={news.loading} error={news.error} onRefresh={news.refresh} /> : null}
              {activeModule === "candles" ? (
                <CandleFeedModule
                  candles={terminal.candles}
                  selectedCandle={selectedCandle}
                  loading={terminal.state === "loading"}
                  onSelect={setSelected}
                />
              ) : null}
              {activeModule === "quality" ? (
                <section className="module-quality-content">
                  <div className="module-summary-grid">
                    <AnalysisTile label="Bid" value={formatPrice(terminal.quote?.bid)} tone="positive" detail="AllTick live" />
                    <AnalysisTile label="Ask" value={formatPrice(terminal.quote?.ask)} detail="AllTick live" />
                    <AnalysisTile label="Spread" value={spread === null ? "—" : price.format(spread)} detail="Ask minus bid" />
                    <AnalysisTile label={`${terminal.timeframe} stored`} value={integer.format(availability?.storedCandles ?? 0)} detail="SQL candles" />
                  </div>
                  <div className="connection-row">
                    <StatusDot online={Boolean(terminal.provider?.connected)} />
                    <div><strong>{terminal.provider?.connected ? `${providerName} connected` : `${providerName} waiting`}</strong><small>{terminal.provider?.message ?? "Checking provider connection…"}</small></div>
                  </div>
                  <dl className="terminal-details module-quality-details">
                    <Detail label="Internal symbol" value={terminal.provider?.applicationSymbol ?? "XAUUSD"} />
                    <Detail label="Provider symbol" value={terminal.provider?.providerSymbol ?? "—"} />
                    <Detail label="Pipeline status" value={synchronization?.status ?? "Not started"} tone={statusTone(synchronization?.status)} />
                    <Detail label="Visible completed" value={integer.format(completedVisible)} />
                    <Detail label="Visible forming" value={integer.format(terminal.candles.length - completedVisible)} />
                    <Detail label="Gap candidates" value={integer.format(synchronization?.detectedGapCount ?? 0)} tone={synchronization?.detectedGapCount ? "warning" : undefined} />
                  </dl>
                </section>
              ) : null}
            </div>
            <footer className="control-modal-footer">
              <span>{activeModuleMessage}</span>
              <button type="button" onClick={() => setActiveModule(null)}>Done</button>
            </footer>
          </section>
        </div>
      ) : null}
    </main>
  );
}

function CandleFeedModule({ candles, selectedCandle, loading, onSelect }: Readonly<{
  candles: StoredMarketCandle[];
  selectedCandle: StoredMarketCandle | null;
  loading: boolean;
  onSelect: (value: string) => void;
}>) {
  return (
    <article className="terminal-panel feed-panel">
      <PanelHeader eyebrow="Stored evidence" title="Candle feed">
        <span className="panel-note">{integer.format(candles.length)} records</span>
      </PanelHeader>
      <div className="terminal-table-scroll">
        <table className="terminal-table">
          <thead><tr><th>Open time</th><th>Open</th><th>High</th><th>Low</th><th>Close</th><th>Ticks</th><th>State</th></tr></thead>
          <tbody>
            {candles.map((candle) => (
              <CandleRow candle={candle} selected={candle.openTimeUtc === selectedCandle?.openTimeUtc} onSelect={onSelect} key={candle.openTimeUtc} />
            ))}
            {candles.length === 0 ? (
              <tr><td className="terminal-empty" colSpan={7}>{loading ? "Loading stored candles…" : "No stored candles for this timeframe."}</td></tr>
            ) : null}
          </tbody>
        </table>
      </div>
    </article>
  );
}

function TechnicalAnalysisPanel({ analysis, multiTimeframe, error, loading }: Readonly<{
  analysis: TechnicalAnalysis | null;
  multiTimeframe: MultiTimeframeAnalysis | null;
  error: string | null;
  loading: boolean;
}>) {
  if (!analysis) {
    return (
      <article className="terminal-panel analysis-panel">
        <PanelHeader eyebrow="Application-owned engine" title="Technical analysis">
          <span className="state-chip forming">{loading ? "Calculating" : "Unavailable"}</span>
        </PanelHeader>
        <div className="analysis-empty">
          <strong>{loading ? "Calculating completed-candle evidence…" : "Analysis is not ready"}</strong>
          <span>{error ?? "Synchronize this timeframe to provide completed candles for analysis."}</span>
        </div>
      </article>
    );
  }

  const readiness = analysis.diagnostics.insufficientIndicators === 0 ? "Ready" : "Warming up";
  const nearestLevels = analysis.supportResistance.slice(0, 2);
  const patterns = analysis.candlestickPatterns.slice(0, 2);
  const conflicts = [...new Set([...analysis.conflicts, ...(multiTimeframe?.conflicts ?? [])])];
  return (
    <article className="terminal-panel analysis-panel">
      <PanelHeader eyebrow="Application-owned engine" title={`${analysis.timeframe} technical analysis`}>
        <span className={`state-chip ${readiness === "Ready" ? "complete" : "forming"}`}>{readiness}</span>
      </PanelHeader>

      <div className="analysis-summary-grid">
        <AnalysisTile label="Trend" value={analysis.trend.direction} tone={directionTone(analysis.trend.direction)} detail={analysis.trend.strength} />
        <AnalysisTile label="Structure" value={formatEvidence(analysis.marketStructure.structure)} tone={directionTone(analysis.marketStructure.direction)} detail={analysis.marketStructure.direction} />
        <AnalysisTile label={`RSI ${analysis.momentum.rsi.period}`} value={formatNumber(analysis.momentum.rsi.value, 1)} tone={directionTone(analysis.momentum.rsi.momentum)} detail={analysis.momentum.rsi.zone} />
        <AnalysisTile label="Volatility" value={formatEvidence(analysis.volatility.regime)} detail={`ATR ${formatNumber(analysis.volatility.atr.value, 2)}`} />
      </div>

      <section className="analysis-section">
        <div className="analysis-section-title"><span>Indicator evidence</span><small>{analysis.diagnostics.candlesUsed} closed candles</small></div>
        <dl className="analysis-evidence-list">
          <Detail label="EMA alignment" value={formatEvidence(analysis.trend.emaAlignment)} tone={directionTone(analysis.trend.direction)} />
          <Detail label="MACD histogram" value={formatNumber(analysis.momentum.macd.histogram, 4)} tone={directionTone(analysis.momentum.macd.momentum)} />
          <Detail label="Stochastic K / D" value={`${formatNumber(analysis.momentum.stochastic.percentK, 1)} / ${formatNumber(analysis.momentum.stochastic.percentD, 1)}`} />
          <Detail label="Range vs average" value={`${formatNumber(analysis.volatility.currentRangeRelativeToAverage, 2)}×`} />
        </dl>
      </section>

      <section className="analysis-section">
        <div className="analysis-section-title"><span>Multi-timeframe context</span><small>{formatEvidence(multiTimeframe?.trendAlignment ?? "Unavailable")}</small></div>
        <div className="timeframe-evidence">
          {multiTimeframe?.timeframes.map((item) => (
            <div key={item.timeframe}>
              <strong>{item.timeframe}</strong>
              <span className={`${directionTone(item.trend)}-text`}>{item.trend}</span>
              <small>{item.candlesUsed} candles</small>
            </div>
          )) ?? <span className="analysis-muted">No cross-timeframe evidence.</span>}
        </div>
      </section>

      {(nearestLevels.length > 0 || patterns.length > 0) ? (
        <section className="analysis-section compact-analysis-section">
          <div className="analysis-section-title"><span>Price evidence</span><small>No trade signal</small></div>
          <div className="evidence-chips">
            {nearestLevels.map((level) => <span key={`${level.type}-${level.center}`}>{level.type} {price.format(level.center)} · {level.touches} touches</span>)}
            {patterns.map((pattern) => <span key={`${pattern.pattern}-${pattern.candleTimeUtc}`}>{formatEvidence(pattern.pattern)} · {pattern.direction}</span>)}
          </div>
        </section>
      ) : null}

      <div className={`analysis-conflicts ${conflicts.length > 0 ? "has-conflicts" : ""}`}>
        <strong>{conflicts.length > 0 ? `${conflicts.length} conflict${conflicts.length === 1 ? "" : "s"} detected` : "No explicit conflicts detected"}</strong>
        <span>{conflicts.length > 0 ? conflicts.map(formatEvidence).join(" · ") : "Evidence is descriptive and does not authorize execution."}</span>
      </div>
      <footer className="analysis-footer">Cutoff {formatDate(analysis.diagnostics.dataCutoffUtc)} · {analysis.diagnostics.durationMilliseconds} ms</footer>
    </article>
  );
}

function NewsModule({ articles, status, loading, error, onRefresh }: Readonly<{
  articles: PagedNewsArticles | null;
  status: NewsSystemStatus | null;
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}>) {
  return (
    <section className="terminal-panel news-module">
      <PanelHeader eyebrow="Application-owned news store" title="Latest market intelligence">
        <span className={`state-chip ${status?.collection?.status === "Healthy" ? "complete" : "forming"}`}>{status?.collection?.status ?? (loading ? "Loading" : "Checking")}</span>
      </PanelHeader>
      <div className="news-module-summary">
        <div><span>Stored articles</span><strong>{integer.format(status?.storedArticles ?? articles?.totalItems ?? 0)}</strong></div>
        <div><span>Provider</span><strong>{status?.provider.provider ?? "NewsData"}</strong></div>
        <div><span>Last collection</span><strong>{formatDate(status?.collection?.lastSuccessfulCollectionAtUtc ?? null)}</strong></div>
        <button className="secondary-action" type="button" onClick={onRefresh} disabled={loading}>{loading ? "Refreshing…" : "Refresh news"}</button>
      </div>
      {error ? <p className="terminal-error" role="alert">{error}</p> : null}
      <div className="news-list">
        {articles?.items.map((article) => (
          <article className="news-item" key={article.id}>
            <div className="news-item-meta"><span className={`state-chip ${article.relevance === "VeryHigh" || article.relevance === "High" ? "forming" : "neutral"}`}>{formatEvidence(article.relevance)}</span><span>{article.sourceName ?? article.provider}</span><time>{formatDate(article.publishedAtUtc)}</time></div>
            <h3>{article.title}</h3>
            <p>{article.description ?? "No description is available from the provider."}</p>
            <div className="news-item-footer"><span>{article.categories.map(formatEvidence).join(" · ") || "Market news"}</span><a href={article.url} target="_blank" rel="noreferrer">Open source</a></div>
          </article>
        ))}
        {!loading && (articles?.items.length ?? 0) === 0 ? <div className="news-empty">No stored news articles are available.</div> : null}
      </div>
    </section>
  );
}

function CandleRow({ candle, selected, onSelect }: Readonly<{ candle: StoredMarketCandle; selected: boolean; onSelect: (value: string) => void }>) {
  return <tr data-selected={selected || undefined}><td><button className="row-select" type="button" onClick={() => onSelect(candle.openTimeUtc)}>{formatDate(candle.openTimeUtc)}</button></td><td>{formatPrice(candle.open)}</td><td>{formatPrice(candle.high)}</td><td>{formatPrice(candle.low)}</td><td className={candle.close >= candle.open ? "positive-text" : "negative-text"}>{formatPrice(candle.close)}</td><td>{candle.tickVolume === null ? "—" : compact.format(candle.tickVolume)}</td><td><span className={`state-chip ${candle.isComplete ? "complete" : "forming"}`}>{candle.isComplete ? "Closed" : "Forming"}</span></td></tr>;
}

function PanelHeader({ eyebrow, title, children }: Readonly<{ eyebrow: string; title: string; children?: ReactNode }>) {
  return <header className="panel-header"><div><span>{eyebrow}</span><h2>{title}</h2></div>{children ? <div className="panel-header-actions">{children}</div> : null}</header>;
}
function Detail({ label, value, tone }: Readonly<{ label: string; value: string; tone?: string }>) { return <div><dt>{label}</dt><dd className={tone ? `${tone}-text` : undefined}>{value}</dd></div>; }
function AnalysisTile({ label, value, tone, detail }: Readonly<{ label: string; value: string; tone?: string; detail: string }>) { return <div><span>{label}</span><strong className={tone ? `${tone}-text` : undefined}>{formatEvidence(value)}</strong><small>{formatEvidence(detail)}</small></div>; }
function StatusDot({ online }: Readonly<{ online: boolean }>) { return <i className="status-dot" data-state={online ? "online" : "offline"} aria-hidden="true" />; }
function formatPrice(value: number | null | undefined) { return value === null || value === undefined ? "—" : price.format(value); }
function formatNumber(value: number | null | undefined, digits: number) { return value === null || value === undefined ? "—" : value.toFixed(digits); }
function formatDate(value: string | null) { return value ? `${utcDate.format(new Date(value))} UTC` : "—"; }
function formatWeeklyRange(now: Date) {
  const end = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
  const start = new Date(end - (6 * MILLISECONDS_PER_DAY));
  return `${utcShortDate.format(start)}–${utcShortDate.format(new Date(end))}`;
}
function formatEvidence(value: string) { return value.replace(/([a-z0-9])([A-Z])/g, "$1 $2").replaceAll(":", ": "); }
function directionTone(direction: string) {
  if (direction === "Bullish") return "positive";
  if (direction === "Bearish") return "negative";
  if (direction === "Conflicting") return "warning";
  return "neutral";
}
function statusTone(status: string | undefined) {
  const value = status?.toLowerCase() ?? "";
  if (value.includes("success") || value.includes("healthy") || value.includes("complete")) return "complete";
  if (value.includes("running")) return "forming";
  if (value.includes("fail") || value.includes("interrupt")) return "negative";
  return "neutral";
}
