"use client";

import { useEffect, useState } from "react";
import type { AiProviderAccountStatus } from "@/features/analysis/api/ai-provider-status";
import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import {
  cancelFullAnalysis,
  createFullAnalysis,
  getActiveFullAnalyses,
  getFullAnalysisHistory,
  getFullProviderStatuses,
  getFullWorkspaceConfiguration,
  type FullAnalysisResult,
  type FullDecision,
  type FullWorkspaceConfiguration,
} from "@/features/full-analysis/api/full-analyst";

const price = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const percent = new Intl.NumberFormat("en-US", { style: "percent", maximumFractionDigits: 0 });
const utcDate = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "UTC",
  timeZoneName: "short",
});

export function FullAnalystWorkspace({ timeframe, onResultChange }: Readonly<{
  timeframe: MarketTimeframeCode;
  onResultChange?: (result: FullAnalysisResult | null) => void;
}>) {
  const [result, setResult] = useState<FullAnalysisResult | null>(null);
  const [history, setHistory] = useState<FullAnalysisResult[]>([]);
  const [configuration, setConfiguration] = useState<FullWorkspaceConfiguration[]>([]);
  const [providerStatuses, setProviderStatuses] = useState<AiProviderAccountStatus[]>([]);
  const [activeResultId, setActiveResultId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const [active, recent, workspaces, providers] = await Promise.all([
          getActiveFullAnalyses(),
          getFullAnalysisHistory(),
          getFullWorkspaceConfiguration(),
          getFullProviderStatuses(),
        ]);
        if (!cancelled) {
          const selected = active.find((item) => item.timeframe === timeframe) ?? active[0] ?? null;
          setHistory(recent.items);
          setConfiguration(workspaces);
          setProviderStatuses(providers);
          setActiveResultId(selected?.id ?? null);
          setResult(selected);
          onResultChange?.(selected);
        }
      } catch (loadError) {
        if (!cancelled) setError(message(loadError));
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [onResultChange, timeframe]);

  useEffect(() => {
    let cancelled = false;
    const refreshActive = async () => {
      try {
        const [active, providers] = await Promise.all([getActiveFullAnalyses(), getFullProviderStatuses()]);
        if (cancelled) return;
        setProviderStatuses(providers);
        const selected = active.find((item) => item.timeframe === timeframe) ?? active[0] ?? null;
        setActiveResultId(selected?.id ?? null);
        if (selected) {
          setResult(selected);
          onResultChange?.(selected);
        } else if (activeResultId) {
          setResult(null);
          onResultChange?.(null);
        }
      } catch {
        // Preserve the last known result; provider diagnostics remain available in health.
      }
    };
    const timer = window.setInterval(() => void refreshActive(), 30_000);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [activeResultId, onResultChange, timeframe]);

  const configured = configuration.filter((workspace) => workspace.enabled && workspace.hasApiKey).length;
  const blockedProvider = providerStatuses.find((status) => !status.canGenerate) ?? null;
  const canRun = !loading && !running && configured === 8 && activeResultId === null && blockedProvider === null;

  async function runAnalysis() {
    setRunning(true);
    setError(null);
    try {
      const created = await createFullAnalysis(timeframe);
      setResult(created);
      setActiveResultId(created.status === "Active" ? created.id : null);
      onResultChange?.(created);
      setHistory((items) => [created, ...items.filter((item) => item.id !== created.id)].slice(0, 12));
    } catch (runError) {
      setError(message(runError));
    } finally {
      setRunning(false);
    }
  }

  async function cancelAnalysis() {
    if (!result) return;
    setRunning(true);
    setError(null);
    try {
      const cancelled = await cancelFullAnalysis(result.id);
      setHistory((items) => items.map((item) => item.id === cancelled.id ? cancelled : item));
      setActiveResultId(null);
      setResult(null);
      onResultChange?.(null);
    } catch (cancelError) {
      setError(message(cancelError));
    } finally {
      setRunning(false);
    }
  }

  return (
    <section className="full-analyst-workspace">
      <header className="full-analyst-toolbar">
        <div>
          <span>Independent future direction</span>
          <strong>XAUUSD · {timeframe}</strong>
          <small>{configured}/8 Future AI workspaces configured</small>
        </div>
        <button type="button" className="primary-action" onClick={runAnalysis} disabled={!canRun}>
          {running ? "Analyzing…" : activeResultId ? "Active future loaded" : providerButtonLabel(blockedProvider, "Generate Future")}
        </button>
      </header>

      {configured < 8 && !loading ? (
        <p className="full-analyst-warning">Enable and configure all eight independent Future AI workspaces before requesting analysis.</p>
      ) : null}
      {blockedProvider ? <p className="full-analyst-warning" role="status">{blockedProvider.message}</p> : null}
      {error ? <p className="terminal-error" role="alert">{error}</p> : null}

      {result ? (
        <article className="full-analyst-result" data-decision={result.decision.toLowerCase()}>
          <div className="full-decision-hero">
            <span aria-hidden="true">{decisionSymbol(result.decision)}</span>
            <div>
              <small>Future Analyst · real-time chart outlook · {result.symbol} · {result.timeframe}</small>
              <strong>{result.decision.toUpperCase()}</strong>
              <p>{result.reasoning}</p>
            </div>
            <span className={`state-chip ${decisionTone(result.decision)}`}>{result.status}</span>
          </div>

          <div className="full-result-grid">
            <Metric label="Signal" value={result.decision.toUpperCase()} />
            <Metric label="Entry / current" value={formatPrice(result.currentPrice)} />
            <Metric label="Take profit (TP)" value="Not part of Future" />
            <Metric label="Stop loss (SL)" value={formatPrice(result.invalidation?.price ?? null)} />
            <Metric label="Confidence" value={formatRatio(result.confidence)} />
            <Metric label="Valid until" value={formatDate(result.validUntilUtc)} />
          </div>

          <div className="full-result-body">
            <section>
              <div className="analysis-section-title"><span>Specialist workspaces</span><small>{result.inputTokens + result.outputTokens} tokens</small></div>
              <div className="full-specialist-grid">
                {result.workspaceResults.map((run) => (
                  <div key={run.id}>
                    <span>{run.workspace}</span>
                    <strong className={`${decisionTextTone(run.specialistOutput?.direction)}-text`}>
                      {run.specialistOutput?.direction ?? run.status}
                    </strong>
                    <small>{run.cacheHit ? "Cached" : run.specialistOutput?.summary ?? run.errorMessage ?? "No output"}</small>
                  </div>
                ))}
              </div>
            </section>
            <section>
              <div className="analysis-section-title"><span>Uncertainty & conflicts</span><small>{formatRatio(result.agreement)} agreement</small></div>
              <p>{result.invalidation?.summary ?? "No active invalidation rule."}</p>
              <p>{result.uncertainty}</p>
              <ul>
                {(result.conflicts.length > 0 ? result.conflicts : ["No material conflict recorded."]).map((conflict) => <li key={conflict}>{conflict}</li>)}
              </ul>
            </section>
          </div>

          {result.status === "Active" ? (
            <footer className="full-result-actions">
              <span>Independent direction only · no target sharing · no automatic trading</span>
              <button type="button" onClick={cancelAnalysis} disabled={running}>Cancel Future</button>
            </footer>
          ) : null}
        </article>
      ) : (
        <div className="analysis-empty analyst-empty-compact">
          <strong>{loading ? "Loading Future Analyst history…" : "No Future Analyst result yet"}</strong>
          <span>Generate an outlook after all eight workspaces are configured.</span>
        </div>
      )}

      {history.length > 0 ? (
        <section className="full-history">
          <div className="analysis-section-title"><span>Future history</span><small>Preserved snapshots</small></div>
          <div>
            {history.map((item) => (
              <button type="button" onClick={() => { setResult(item); onResultChange?.(item); }} aria-pressed={result?.id === item.id} key={item.id}>
                <strong className={`${decisionTextTone(item.decision)}-text`}>{item.decision}</strong>
                <span>{item.timeframe}</span>
                <time>{formatDate(item.analysisTimeUtc)}</time>
              </button>
            ))}
          </div>
        </section>
      ) : null}
    </section>
  );
}

function Metric({ label, value }: Readonly<{ label: string; value: string }>) {
  return <div><span>{label}</span><strong>{value}</strong></div>;
}

function decisionSymbol(decision: FullDecision): string {
  if (decision === "Buy") return "↑";
  if (decision === "Sell") return "↓";
  return "—";
}

function decisionTone(decision: FullDecision): string {
  if (decision === "Buy") return "complete";
  if (decision === "Sell") return "negative";
  return "forming";
}

function decisionTextTone(decision?: FullDecision): string {
  if (decision === "Buy") return "positive";
  if (decision === "Sell") return "negative";
  return "warning";
}

function formatPrice(value: number | null): string {
  return value === null ? "—" : price.format(value);
}

function formatRatio(value: number | null): string {
  return value === null ? "—" : percent.format(value);
}

function formatDate(value: string | null): string {
  return value ? utcDate.format(new Date(value)) : "—";
}

function message(error: unknown): string {
  return error instanceof Error ? error.message : "Future Analyst request failed.";
}

function providerButtonLabel(status: AiProviderAccountStatus | null, fallback: string): string {
  if (!status) return fallback;
  return status.state === "QuotaExhausted" ? "Daily quota exhausted" : "AI provider unavailable";
}
