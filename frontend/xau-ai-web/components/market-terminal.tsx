"use client";

import { useEffect, useState, type ReactNode } from "react";
import { marketTimeframes, type MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import { useMarketTerminal } from "@/hooks/use-market-terminal";
import { useNewsModule } from "@/hooks/use-news-module";
import { useEconomicModule } from "@/hooks/use-economic-module";
import { useAnalystHealth, type AnalystHealthSnapshot } from "@/hooks/use-analyst-health";
import { TradingChart } from "@/components/trading-chart";
import { FullAnalystWorkspace } from "@/components/full-analyst-workspace";
import { TargetAnalystWorkspace } from "@/components/target-analyst-workspace";
import type { FullAnalysisResult } from "@/features/full-analysis/api/full-analyst";
import type { TargetAnalysisResult } from "@/features/target-analysis/api/target-analyst";
import type { LocalAnalystStatus } from "@/features/analysis/api/get-local-analyst";
import type {
  EconomicObservation,
  EconomicSeries,
  EconomicSystemStatus,
} from "@/features/economic/api/get-economic-data";
import { getActiveTradingSessions, getXauUsdMarketSession } from "@/lib/market/xauusd-session";
import { DISPLAY_TIME_ZONE, DISPLAY_TIME_ZONE_LABEL } from "@/lib/time/utc-plus-seven";
import type { MultiTimeframeAnalysis, TechnicalAnalysis } from "@/types/analysis";
import type { MarketDataPipelineStatus, MarketDataSourceComparison, MarketProviderStatus } from "@/types/market";
import type { NewsSystemStatus, PagedNewsArticles } from "@/types/news";

const price = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const integer = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 });
const economicNumber = new Intl.NumberFormat("en-US", { maximumFractionDigits: 4 });
const displayDate = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit", hour12: false, timeZone: DISPLAY_TIME_ZONE,
});
const utcPlusSevenTime = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false, timeZone: DISPLAY_TIME_ZONE,
});
const utcTime = new Intl.DateTimeFormat("en-GB", {
  hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false, timeZone: "UTC",
});
const dateOnly = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit", month: "short", year: "numeric", timeZone: DISPLAY_TIME_ZONE,
});
const timeframeLabels: Record<MarketTimeframeCode, string> = {
  M1: "M1", M5: "M5", M15: "M15", M30: "M30", H1: "1H", H4: "4H", D1: "1D",
};
const moduleLaunchers = [
  { key: "analysis", eyebrow: "Analysis", title: "Application-owned engine" },
  { key: "news", eyebrow: "Intelligence", title: "News" },
  { key: "economy", eyebrow: "Macro", title: "Economic schedule" },
  { key: "quality", eyebrow: "Operations", title: "Provider & storage" },
] as const;
type ModuleKey = (typeof moduleLaunchers)[number]["key"];

export function MarketTerminal() {
  const terminal = useMarketTerminal();
  const [now, setNow] = useState<Date | null>(null);
  const [activeModule, setActiveModule] = useState<ModuleKey | null>(null);
  const [targetChartResult, setTargetChartResult] = useState<TargetAnalysisResult | null>(null);
  const [fullChartResult, setFullChartResult] = useState<FullAnalysisResult | null>(null);
  const news = useNewsModule(true);
  const economy = useEconomicModule(true);
  const analystHealth = useAnalystHealth(true);

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

  const completedVisible = terminal.candles.filter((item) => item.isComplete).length;
  const twelveDataReady = terminal.sourceComparison?.referenceEnabled === true
    && terminal.sourceComparison.matchedCandles > 0;
  const localIssues = terminal.localStatus?.timeframes.some((item) => isLocalReadinessFailure(item.lastReason)) ?? true;
  const targetConfigured = analystHealth.snapshot?.targetConfiguration.filter((item) => item.enabled && item.hasApiKey).length ?? 0;
  const futureConfigured = analystHealth.snapshot?.futureConfiguration.filter((item) => item.enabled && item.hasApiKey).length ?? 0;
  const targetFailed = analystHealth.snapshot?.latestTarget?.specialistResults.some((item) => item.status === "Failed") ?? false;
  const futureFailed = analystHealth.snapshot?.latestFuture?.workspaceResults.some((item) => item.status === "Failed") ?? false;
  const compactProviderStatuses: CompactProviderStatus[] = [
    { name: "AllTick", healthy: terminal.provider?.connected === true, detail: terminal.provider?.message ?? "Live market provider is still checking." },
    { name: "Twelve Data", healthy: twelveDataReady, detail: twelveDataReady ? "Reference candles are ready." : "Reference candles are unavailable." },
    {
      name: "SQL",
      healthy: isHealthyStatus(terminal.pipeline?.synchronization?.status)
        && (terminal.pipeline?.synchronization?.consecutiveFailures ?? 0) === 0,
      detail: terminal.pipeline?.synchronization?.lastErrorMessage ?? "Primary candle storage and synchronization.",
    },
    {
      name: "Local",
      healthy: terminal.localStatus?.enabled === true && !localIssues,
      detail: terminal.localSignalError ?? "Deterministic local analyst checkpoints.",
    },
    {
      name: "Target",
      healthy: targetConfigured === 8
        && !targetFailed
        && !(analystHealth.snapshot?.targetProviderStatuses.some((item) => !item.canGenerate) ?? true),
      detail: analystHealth.error ?? `${targetConfigured}/8 Target workspaces ready.`,
    },
    {
      name: "Future",
      healthy: futureConfigured === 8
        && !futureFailed
        && !(analystHealth.snapshot?.futureProviderStatuses.some((item) => !item.canGenerate) ?? true),
      detail: analystHealth.error ?? `${futureConfigured}/8 Future workspaces ready.`,
    },
    {
      name: news.status?.provider.provider ?? "NewsData",
      healthy: news.status?.provider.state === "Available" && news.status.collection?.status === "Healthy",
      detail: news.error ?? news.status?.provider.message ?? "News provider is still checking.",
    },
    {
      name: economy.status?.provider.provider ?? "FRED",
      healthy: economy.status?.provider.state === "Available"
        && economy.status.series.every((item) => item.consecutiveFailures === 0),
      detail: economy.error ?? economy.status?.provider.message ?? "Economic provider is still checking.",
    },
  ];
  const systemState = terminal.provider?.connected ? "Operational" : terminal.state === "loading" ? "Checking" : "Attention";
  const marketSession = now ? getXauUsdMarketSession(now) : null;
  const activeTradingSessions = now ? getActiveTradingSessions(now) : [];
  const changeTimeframe = (timeframe: MarketTimeframeCode) => {
    terminal.setTimeframe(timeframe);
  };
  const activeModuleDefinition = moduleLaunchers.find((item) => item.key === activeModule) ?? null;
  const activeModuleMessage = activeModule === "news"
    ? news.error ?? news.status?.provider.message ?? "Latest normalized news from SQL storage."
    : activeModule === "economy"
      ? economy.error ?? economy.status?.provider.message ?? "Latest normalized economic observations from SQL storage."
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
                <span className={`state-chip ${marketSession === null ? "neutral" : marketSession.isOpen ? "complete" : "negative"}`}>{marketSession === null ? "Checking" : marketSession.isOpen ? "Market open" : "Market closed"}</span>
              </PanelHeader>
              <div className="chart-toolbar">
                <div className="chart-timeframes" aria-label="Chart timeframe">
                  {marketTimeframes.map((timeframe) => (
                    <button type="button" className={terminal.timeframe === timeframe ? "active" : ""} onClick={() => changeTimeframe(timeframe)} aria-pressed={terminal.timeframe === timeframe} key={timeframe}>
                      {timeframeLabels[timeframe]}
                    </button>
                  ))}
                </div>
                <span className="chart-context">XAUUSD</span>
                <span className="chart-context">UTC+7</span>
                <span className="chart-session">{activeTradingSessions.length > 0 ? activeTradingSessions.join(" · ") : "Between sessions"}</span>
                {compactProviderStatuses.map((item) => (
                  <span className={`chart-provider ${item.healthy ? "connected" : "waiting"}`} title={item.detail} key={item.name}>
                    <StatusDot online={item.healthy} />{item.name}
                  </span>
                ))}
                <span className="chart-source">SQL history · live forming candle</span>
              </div>
              <TradingChart
                candles={terminal.candles}
                quote={terminal.quote}
                providerConnected={Boolean(terminal.provider?.connected)}
                timeframe={terminal.timeframe}
                nowUtcMilliseconds={now?.getTime() ?? null}
                marketSession={marketSession}
                signalMarkers={terminal.signalMarkers}
                targetAnalysis={targetChartResult}
                fullAnalysis={fullChartResult}
              />
            </article>
          </section>

          <aside className="right-stack">
            <AnalystRail
              timeframe={terminal.timeframe}
              localStatus={terminal.localStatus}
              onTargetResultChange={setTargetChartResult}
              onFullResultChange={setFullChartResult}
            />
          </aside>
        </div>

        <footer className="status-bar">
          <span><StatusDot online={Boolean(terminal.provider?.connected)} />System {systemState}</span>
          <span>SQL Server · source of truth</span><span>AllTick live · Twelve Data reference</span>
          <span className="status-right">Target / Future AI workspaces</span>
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
                  localStatus={terminal.localStatus}
                  error={terminal.analysisError}
                  loading={terminal.state === "loading"}
                />
              ) : null}
              {activeModule === "news" ? <NewsModule articles={news.articles} status={news.status} loading={news.loading} error={news.error} onRefresh={news.refresh} /> : null}
              {activeModule === "economy" ? <EconomicScheduleModule {...economy} /> : null}
              {activeModule === "quality" ? (
                <ProviderStorageModule
                  timeframe={terminal.timeframe}
                  provider={terminal.provider}
                  pipeline={terminal.pipeline}
                  comparison={terminal.sourceComparison}
                  localStatus={terminal.localStatus}
                  localError={terminal.localSignalError}
                  newsStatus={news.status}
                  newsError={news.error}
                  economicStatus={economy.status}
                  economicError={economy.error}
                  analysts={analystHealth.snapshot}
                  analystError={analystHealth.error}
                  visibleCandles={terminal.candles.length}
                  completedVisible={completedVisible}
                  onRefresh={() => {
                    terminal.refresh();
                    news.refresh();
                    economy.refresh();
                    analystHealth.refresh();
                  }}
                  loading={news.loading || economy.loading || analystHealth.loading}
                />
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

function AnalystRail({ timeframe, localStatus, onTargetResultChange, onFullResultChange }: Readonly<{
  timeframe: MarketTimeframeCode;
  localStatus: LocalAnalystStatus | null;
  onTargetResultChange: (result: TargetAnalysisResult | null) => void;
  onFullResultChange: (result: FullAnalysisResult | null) => void;
}>) {
  const [activeWorkspace, setActiveWorkspace] = useState<"target" | "full">("target");

  return (
    <section className="terminal-panel analyst-rail">
      <header className="analyst-switcher" role="tablist" aria-label="AI analyst workspace">
        <button
          type="button"
          role="tab"
          aria-selected={activeWorkspace === "target"}
          onClick={() => setActiveWorkspace("target")}
        >
          <span>Phase 13</span>
          <strong>Target Analyst</strong>
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={activeWorkspace === "full"}
          onClick={() => setActiveWorkspace("full")}
        >
          <span>Phase 14</span>
          <strong>Future Analyst</strong>
        </button>
      </header>
      <div className="analyst-rail-body" role="tabpanel">
        {activeWorkspace === "target"
          ? <TargetAnalystWorkspace timeframe={timeframe} localStatus={localStatus} onResultChange={onTargetResultChange} />
          : <FullAnalystWorkspace timeframe={timeframe} onResultChange={onFullResultChange} />}
      </div>
    </section>
  );
}

function EconomicScheduleModule({ series, observations, status, loading, error, refresh }: Readonly<{
  series: EconomicSeries[];
  observations: EconomicObservation[];
  status: EconomicSystemStatus | null;
  loading: boolean;
  error: string | null;
  refresh: () => void;
}>) {
  const seriesById = new Map(series.map((item) => [item.id, item]));
  const rows = [...observations].sort((left, right) => right.observationDate.localeCompare(left.observationDate));
  const healthy = status?.provider.state === "Available" && status.series.every((item) => item.consecutiveFailures === 0);
  return (
    <article className="terminal-panel feed-panel economy-schedule">
      <PanelHeader eyebrow="FRED · normalized SQL storage" title="Economic schedule">
        <span className={`state-chip ${healthy ? "complete" : loading ? "forming" : "negative"}`}>
          {loading ? "Loading" : healthy ? "Healthy" : "Attention"}
        </span>
      </PanelHeader>
      <div className="news-module-summary">
        <div><span>Provider</span><strong>{status?.provider.provider ?? "FRED"}</strong></div>
        <div><span>Stored series</span><strong>{integer.format(status?.storedSeries ?? series.length)}</strong></div>
        <div><span>Observations</span><strong>{integer.format(status?.storedObservations ?? 0)}</strong></div>
        <button className="secondary-action" type="button" onClick={refresh} disabled={loading}>{loading ? "Refreshing…" : "Refresh schedule"}</button>
      </div>
      {error ? <p className="terminal-error" role="alert">{error}</p> : null}
      <p className="module-disclaimer">Next period is estimated from the stored series cadence; it is not an official release timestamp.</p>
      <div className="terminal-table-scroll">
        <table className="terminal-table">
          <thead><tr><th>Indicator</th><th>Frequency</th><th>Latest period</th><th>Value</th><th>Next expected period</th><th>Storage</th></tr></thead>
          <tbody>
            {rows.map((observation) => {
              const definition = seriesById.get(observation.economicSeriesId);
              return (
                <tr key={observation.id}>
                  <td><strong>{observation.seriesName}</strong><small>{observation.externalSeriesId}</small></td>
                  <td>{definition?.frequency ?? "—"}</td>
                  <td>{formatDateOnly(observation.observationDate)}</td>
                  <td>{formatEconomicValue(observation, definition)}</td>
                  <td>{expectedEconomicPeriod(observation.observationDate, definition?.frequency)}</td>
                  <td><span className={`state-chip ${observation.status === "Valid" ? "complete" : "neutral"}`}>{observation.status}</span></td>
                </tr>
              );
            })}
            {!loading && rows.length === 0 ? <tr><td className="terminal-empty" colSpan={6}>No stored economic observations are available.</td></tr> : null}
          </tbody>
        </table>
      </div>
    </article>
  );
}

function ProviderStorageModule({
  timeframe,
  provider,
  pipeline,
  comparison,
  localStatus,
  localError,
  newsStatus,
  newsError,
  economicStatus,
  economicError,
  analysts,
  analystError,
  visibleCandles,
  completedVisible,
  onRefresh,
  loading,
}: Readonly<{
  timeframe: MarketTimeframeCode;
  provider: MarketProviderStatus | null;
  pipeline: MarketDataPipelineStatus | null;
  comparison: MarketDataSourceComparison | null;
  localStatus: LocalAnalystStatus | null;
  localError: string | null;
  newsStatus: NewsSystemStatus | null;
  newsError: string | null;
  economicStatus: EconomicSystemStatus | null;
  economicError: string | null;
  analysts: AnalystHealthSnapshot | null;
  analystError: string | null;
  visibleCandles: number;
  completedVisible: number;
  onRefresh: () => void;
  loading: boolean;
}>) {
  const synchronization = pipeline?.synchronization;
  const targetReady = analysts?.targetConfiguration.filter((item) => item.enabled && item.hasApiKey).length ?? 0;
  const futureReady = analysts?.futureConfiguration.filter((item) => item.enabled && item.hasApiKey).length ?? 0;
  const targetFailures = analysts?.latestTarget?.specialistResults.filter((item) => item.status === "Failed") ?? [];
  const futureFailures = analysts?.latestFuture?.workspaceResults.filter((item) => item.status === "Failed") ?? [];
  const targetProviderIssue = analysts?.targetProviderStatuses.find((item) => !item.canGenerate) ?? null;
  const futureProviderIssue = analysts?.futureProviderStatuses.find((item) => !item.canGenerate) ?? null;
  const localIssues = localStatus?.timeframes
    .filter((item) => isLocalReadinessFailure(item.lastReason))
    .map((item) => `${item.timeframe}: ${formatEvidence(item.lastReason ?? "not evaluated")}`) ?? [];
  const rows: HealthRowData[] = [
    {
      name: "AllTick",
      role: "Live market provider",
      healthy: provider?.connected === true,
      storage: `${integer.format(pipeline?.availability.storedCandles ?? visibleCandles)} ${timeframe} candles in SQL`,
      message: provider?.message ?? "Provider status has not loaded.",
    },
    {
      name: "Twelve Data",
      role: "Historical reference provider",
      healthy: comparison?.referenceEnabled === true && comparison.matchedCandles > 0,
      storage: `${integer.format(comparison?.matchedCandles ?? 0)} compared reference candles`,
      message: comparison?.referenceEnabled
        ? comparison.matchedCandles > 0 ? "Reference candles are stored and comparable." : "Enabled, but no matching stored reference candles were found."
        : "Reference provider is disabled or unavailable.",
    },
    {
      name: "SQL candle pipeline",
      role: "Primary market storage",
      healthy: isHealthyStatus(synchronization?.status) && (synchronization?.consecutiveFailures ?? 0) === 0,
      storage: `${integer.format(completedVisible)} complete + ${integer.format(Math.max(0, visibleCandles - completedVisible))} forming visible`,
      message: synchronization?.lastErrorMessage
        ?? `${synchronization?.status ?? "Not started"}; ${integer.format(synchronization?.detectedGapCount ?? 0)} reported gap candidates.`,
    },
    {
      name: "Local Analyst",
      role: "Possible BUY / SELL / STOP",
      healthy: localStatus?.enabled === true && localStatus.timeframes.length > 0 && localIssues.length === 0,
      storage: `${integer.format(localStatus?.timeframes.length ?? 0)} timeframe checkpoints in SQL`,
      message: localError ?? (localIssues.length > 0 ? localIssues.join(" · ") : "All configured timeframe evaluators are reporting."),
    },
    {
      name: "Target AI",
      role: "Best forward position",
      healthy: targetReady === 8 && targetFailures.length === 0 && targetProviderIssue === null,
      storage: `${integer.format(analysts?.targetStoredResults ?? 0)} analysis snapshots in SQL`,
      message: analystError
        ?? targetProviderIssue?.message
        ?? (targetFailures.length > 0
          ? `Latest run failed: ${targetFailures.map((item) => `${item.workspace}: ${item.errorCode ?? item.errorMessage ?? "provider unavailable"}`).join(" · ")}`
          : `${targetReady}/8 independent AI workspaces configured; latest persisted run has no provider failure.`),
    },
    {
      name: "Future AI",
      role: "BUY / SELL / WAIT outlook",
      healthy: futureReady === 8 && futureFailures.length === 0 && futureProviderIssue === null,
      storage: `${integer.format(analysts?.futureStoredResults ?? 0)} analysis snapshots in SQL`,
      message: analystError
        ?? futureProviderIssue?.message
        ?? (futureFailures.length > 0
          ? `Latest run failed: ${futureFailures.map((item) => `${item.workspace}: ${item.errorMessage ?? "provider unavailable"}`).join(" · ")}`
          : `${futureReady}/8 independent AI workspaces configured; latest persisted run has no provider failure.`),
    },
    {
      name: newsStatus?.provider.provider ?? "NewsData",
      role: "News provider and collector",
      healthy: newsStatus?.provider.state === "Available" && newsStatus.collection?.status === "Healthy",
      storage: `${integer.format(newsStatus?.storedArticles ?? 0)} articles in SQL`,
      message: newsError ?? newsStatus?.collection?.lastErrorMessage ?? newsStatus?.provider.message ?? "News status has not loaded.",
    },
    {
      name: economicStatus?.provider.provider ?? "FRED",
      role: "Economic provider and schedule store",
      healthy: economicStatus?.provider.state === "Available"
        && economicStatus.series.every((item) => item.consecutiveFailures === 0),
      storage: `${integer.format(economicStatus?.storedObservations ?? 0)} observations across ${integer.format(economicStatus?.storedSeries ?? 0)} series`,
      message: economicError
        ?? economicStatus?.series.find((item) => item.lastErrorMessage)?.lastErrorMessage
        ?? economicStatus?.provider.message
        ?? "Economic status has not loaded.",
    },
  ];

  return (
    <section className="module-quality-content provider-health-module">
      <header className="provider-health-header">
        <div><span>Provider, engine, and durable-storage status</span><strong>Healthy only when the real dependency is usable</strong></div>
        <button className="secondary-action" type="button" onClick={onRefresh} disabled={loading}>{loading ? "Checking…" : "Check all"}</button>
      </header>
      <div className="provider-health-grid">
        {rows.map((row) => <ProviderHealthRow row={row} key={row.name} />)}
      </div>
    </section>
  );
}

interface HealthRowData {
  name: string;
  role: string;
  healthy: boolean;
  storage: string;
  message: string;
}

interface CompactProviderStatus {
  name: string;
  healthy: boolean;
  detail: string;
}

function ProviderHealthRow({ row }: Readonly<{ row: HealthRowData }>) {
  return (
    <article className="provider-health-row" data-state={row.healthy ? "healthy" : "attention"}>
      <StatusDot online={row.healthy} />
      <div><strong>{row.name}</strong><span>{row.role}</span><small>{row.message}</small></div>
      <div><span>{row.healthy ? "Healthy" : "Attention"}</span><small>{row.storage}</small></div>
    </article>
  );
}

function TechnicalAnalysisPanel({ analysis, multiTimeframe, localStatus, error, loading }: Readonly<{
  analysis: TechnicalAnalysis | null;
  multiTimeframe: MultiTimeframeAnalysis | null;
  localStatus: LocalAnalystStatus | null;
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
        <div className="analysis-section-title"><span>Local possible signals · all timeframes</span><small>{localStatus?.configurationVersion ?? "checking"}</small></div>
        <div className="local-timeframe-signals">
          {localStatus?.timeframes.map((item) => {
            const snapshot = item.snapshot;
            const state = snapshot?.state ?? "Nothing";
            return (
              <div data-state={state.toLowerCase()} key={item.timeframe}>
                <strong>{item.timeframe}</strong>
                <span>{state.toUpperCase()}</span>
                <small>{state === "Nothing"
                  ? formatEvidence(item.lastReason ?? "Not evaluated")
                  : `TP ${formatPrice(snapshot?.targetPrice)} · SL ${formatPrice(snapshot?.invalidationPrice)}`}</small>
              </div>
            );
          }) ?? <span className="analysis-muted">Local Analyst status is loading.</span>}
        </div>
      </section>

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

function PanelHeader({ eyebrow, title, children }: Readonly<{ eyebrow: string; title: string; children?: ReactNode }>) {
  return <header className="panel-header"><div><span>{eyebrow}</span><h2>{title}</h2></div>{children ? <div className="panel-header-actions">{children}</div> : null}</header>;
}
function Detail({ label, value, tone }: Readonly<{ label: string; value: string; tone?: string }>) { return <div><dt>{label}</dt><dd className={tone ? `${tone}-text` : undefined}>{value}</dd></div>; }
function AnalysisTile({ label, value, tone, detail }: Readonly<{ label: string; value: string; tone?: string; detail: string }>) { return <div><span>{label}</span><strong className={tone ? `${tone}-text` : undefined}>{formatEvidence(value)}</strong><small>{formatEvidence(detail)}</small></div>; }
function StatusDot({ online }: Readonly<{ online: boolean }>) { return <i className="status-dot" data-state={online ? "online" : "offline"} aria-hidden="true" />; }
function formatPrice(value: number | null | undefined) { return value === null || value === undefined ? "—" : price.format(value); }
function formatNumber(value: number | null | undefined, digits: number) { return value === null || value === undefined ? "—" : value.toFixed(digits); }
function formatDate(value: string | null) { return value ? `${displayDate.format(new Date(value))} ${DISPLAY_TIME_ZONE_LABEL}` : "—"; }
function formatDateOnly(value: string) { return dateOnly.format(new Date(`${value}T00:00:00Z`)); }
function formatEconomicValue(observation: EconomicObservation, series: EconomicSeries | undefined) {
  const value = observation.value === null ? observation.originalValue : economicNumber.format(observation.value);
  return series?.units ? `${value} ${series.units}` : value;
}
function expectedEconomicPeriod(value: string, frequency: string | undefined) {
  const next = new Date(`${value}T00:00:00Z`);
  const normalized = frequency?.toLowerCase() ?? "";
  if (normalized.includes("daily")) next.setUTCDate(next.getUTCDate() + 1);
  else if (normalized.includes("weekly")) next.setUTCDate(next.getUTCDate() + 7);
  else if (normalized.includes("quarter")) next.setUTCMonth(next.getUTCMonth() + 3);
  else if (normalized.includes("annual") || normalized.includes("year")) next.setUTCFullYear(next.getUTCFullYear() + 1);
  else if (normalized.includes("month")) next.setUTCMonth(next.getUTCMonth() + 1);
  else return "Cadence unavailable";
  return dateOnly.format(next);
}
function formatEvidence(value: string) { return value.replace(/([a-z0-9])([A-Z])/g, "$1 $2").replaceAll(":", ": "); }
function isLocalReadinessFailure(reason: string | null) {
  return reason !== null && [
    "NO_COMPLETED_CANDLES",
    "INSUFFICIENT_HISTORY",
    "DUPLICATE_CANDLES",
    "INVALID_OR_INCOMPLETE_CANDLE",
    "MIXED_MARKET_DATA",
    "STALE_MARKET_DATA",
    "NO_ANALYZABLE_CANDLES",
    "INVALID_CANDLES",
    "MARKET_DATA_GAPS",
  ].includes(reason);
}
function isHealthyStatus(status: string | undefined) {
  const value = status?.toLowerCase() ?? "";
  return value.includes("success") || value.includes("healthy") || value.includes("complete");
}
function directionTone(direction: string) {
  if (direction === "Bullish") return "positive";
  if (direction === "Bearish") return "negative";
  if (direction === "Conflicting") return "warning";
  return "neutral";
}
