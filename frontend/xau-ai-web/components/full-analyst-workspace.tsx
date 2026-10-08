"use client";

import { useEffect, useState } from "react";
import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import {
  cancelFullAnalysis,
  createFullAnalysis,
  getActiveFullAnalyses,
  getFullAnalysisHistory,
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

export function FullAnalystWorkspace({ timeframe }: Readonly<{ timeframe: MarketTimeframeCode }>) {
  const [result, setResult] = useState<FullAnalysisResult | null>(null);
  const [history, setHistory] = useState<FullAnalysisResult[]>([]);
  const [configuration, setConfiguration] = useState<FullWorkspaceConfiguration[]>([]);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const [active, recent, workspaces] = await Promise.all([
          getActiveFullAnalyses(),
          getFullAnalysisHistory(),
          getFullWorkspaceConfiguration(),
        ]);
        if (!cancelled) {
          setHistory(recent.items);
          setConfiguration(workspaces);
          setResult(active[0] ?? recent.items[0] ?? null);
        }
      } catch (loadError) {
        if (!cancelled) setError(message(loadError));
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  const configured = configuration.filter((workspace) => workspace.enabled).length;
  const canRun = !loading && !running && configured === 8;

  async function runAnalysis() {
    setRunning(true);
    setError(null);
    try {
      const created = await createFullAnalysis(timeframe);
      setResult(created);
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
      setResult(null);
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
          <span>Independent AI synthesis</span>
          <strong>XAUUSD · {timeframe}</strong>
          <small>{configured}/8 Full AI workspaces enabled</small>
        </div>
        <button type="button" className="primary-action" onClick={runAnalysis} disabled={!canRun}>
          {running ? "Analyzing…" : "Run Full Analyst"}
        </button>
      </header>

      {configured < 8 && !loading ? (
        <p className="full-analyst-warning">Enable and configure all eight independent Full AI workspaces before requesting analysis.</p>
      ) : null}
      {error ? <p className="terminal-error">{error}</p> : null}

      {result ? (
        <article className="full-analyst-result" data-decision={result.decision.toLowerCase()}>
          <div className="full-decision-hero">
            <span aria-hidden="true">{decisionSymbol(result.decision)}</span>
            <div>
              <small>Full Analyst · {result.symbol} · {result.timeframe}</small>
              <strong>{result.decision.toUpperCase()}</strong>
              <p>{result.reasoning}</p>
            </div>
            <span className={`state-chip ${decisionTone(result.decision)}`}>{result.status}</span>
          </div>

          <div className="full-result-grid">
            <Metric label="Current price" value={formatPrice(result.currentPrice)} />
            <Metric label="Confidence" value={formatRatio(result.confidence)} />
            <Metric label="Agreement" value={formatRatio(result.agreement)} />
            <Metric label="Valid until" value={formatDate(result.validUntilUtc)} />
            <Metric label="Invalidation" value={result.invalidation?.summary ?? "No active invalidation"} />
            <Metric label="Future" value={result.futureAvailable ? "Available in Phase 15" : "None"} />
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
              <div className="analysis-section-title"><span>Uncertainty & conflicts</span><small>{result.keyEvidenceIds.length} evidence records</small></div>
              <p>{result.uncertainty}</p>
              <ul>
                {(result.conflicts.length > 0 ? result.conflicts : ["No material conflict recorded."]).map((conflict) => <li key={conflict}>{conflict}</li>)}
              </ul>
            </section>
          </div>

          {result.status === "Active" ? (
            <footer className="full-result-actions">
              <span>Analysis only · no target object · no automatic trading</span>
              <button type="button" onClick={cancelAnalysis} disabled={running}>Cancel Analyst</button>
            </footer>
          ) : null}
        </article>
      ) : (
        <div className="analysis-empty">
          <strong>{loading ? "Loading Full Analyst history…" : "No Full Analyst result yet"}</strong>
          <span>Request analysis after all eight workspaces are configured.</span>
        </div>
      )}

      {history.length > 0 ? (
        <section className="full-history">
          <div className="analysis-section-title"><span>History</span><small>Preserved snapshots</small></div>
          <div>
            {history.map((item) => (
              <button type="button" onClick={() => setResult(item)} aria-pressed={result?.id === item.id} key={item.id}>
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
  return error instanceof Error ? error.message : "Full Analyst request failed.";
}
