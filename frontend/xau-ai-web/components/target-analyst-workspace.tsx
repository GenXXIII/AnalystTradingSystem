"use client";

import { useEffect, useState } from "react";
import type { AiProviderAccountStatus } from "@/features/analysis/api/ai-provider-status";
import type { LocalAnalystStatus } from "@/features/analysis/api/get-local-analyst";
import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import {
  cancelTargetAnalysis,
  createTargetAnalysis,
  getActiveTargets,
  getTargetHistory,
  getTargetProviderStatuses,
  getTargetWorkspaceConfiguration,
  type TargetAnalysisResult,
  type TargetWorkspaceConfiguration,
} from "@/features/target-analysis/api/target-analyst";

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

export function TargetAnalystWorkspace({ timeframe, localStatus, onResultChange }: Readonly<{
  timeframe: MarketTimeframeCode;
  localStatus: LocalAnalystStatus | null;
  onResultChange?: (result: TargetAnalysisResult | null) => void;
}>) {
  const [result, setResult] = useState<TargetAnalysisResult | null>(null);
  const [history, setHistory] = useState<TargetAnalysisResult[]>([]);
  const [configuration, setConfiguration] = useState<TargetWorkspaceConfiguration[]>([]);
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
          getActiveTargets(),
          getTargetHistory(),
          getTargetWorkspaceConfiguration(),
          getTargetProviderStatuses(),
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
        const [active, providers] = await Promise.all([getActiveTargets(), getTargetProviderStatuses()]);
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
        // Preserve the last known active result; the health panel reports connectivity failures.
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
      const created = await createTargetAnalysis(timeframe);
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
      const cancelled = await cancelTargetAnalysis(result.id);
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

  const direction = targetDirection(result);
  const signal = targetSignal(result);
  const local = localStatus?.timeframes.find((item) => item.timeframe === timeframe)?.snapshot ?? null;

  return (
    <section className="target-analyst-workspace">
      <header className="full-analyst-toolbar">
        <div>
          <span>Forward best-position synthesis</span>
          <strong>XAUUSD · {timeframe}</strong>
          <small>{configured}/8 Target AI configured · Local {local ? local.state : "checking"}</small>
        </div>
        <button type="button" className="primary-action" onClick={runAnalysis} disabled={!canRun}>
          {running ? "Analyzing…" : activeResultId ? "Active target loaded" : providerButtonLabel(blockedProvider, "Generate Target")}
        </button>
      </header>

      {configured < 8 && !loading ? (
        <p className="full-analyst-warning">Enable and configure all eight Target AI workspaces before requesting a target.</p>
      ) : null}
      {blockedProvider ? <p className="full-analyst-warning" role="status">{blockedProvider.message}</p> : null}
      {error ? <p className="terminal-error" role="alert">{error}</p> : null}

      {result ? (
        <article className="target-analyst-result" data-direction={direction}>
          <div className="target-decision-hero">
            <span aria-hidden="true">◎</span>
            <div>
              <small>Target Analyst · best forward position · {result.symbol} · {result.timeframe}</small>
              <strong>{signal}</strong>
              <p>{result.reasoningSummary || result.noTargetReason || "No target reasoning is available."}</p>
            </div>
            <span className={`state-chip ${targetTone(direction)}`}>{result.status}</span>
          </div>

          <div className="full-result-grid">
            <Metric label="Signal" value={signal} />
            <Metric label="Entry / current" value={formatPrice(result.currentPrice)} />
            <Metric label="Take profit (TP)" value={formatPrice(result.targetPrice)} />
            <Metric label="Stop loss (SL)" value={formatPrice(result.invalidationPrice)} />
            <Metric label="Confidence" value={formatRatio(result.confidence)} />
            <Metric label="Valid until" value={formatDate(result.validUntilUtc)} />
          </div>

          <div className="full-result-body">
            <section>
              <div className="analysis-section-title"><span>Specialist workspaces</span><small>{result.specialistResults.length} results</small></div>
              <div className="full-specialist-grid">
                {result.specialistResults.map((specialist) => (
                  <div key={specialist.id}>
                    <span>{specialist.workspace}</span>
                    <strong className={`${candidateTone(specialist.hasCandidate)}-text`}>
                      {specialist.hasCandidate ? formatPrice(specialist.candidateTargetPrice) : specialist.status}
                    </strong>
                    <small>{specialist.summary || specialist.errorMessage || specialist.uncertainty || "No output"}</small>
                  </div>
                ))}
              </div>
            </section>
            <section>
              <div className="analysis-section-title"><span>Uncertainty</span><small>{result.evidenceIds.length} evidence records</small></div>
              <p>{result.uncertainty || result.noTargetReason || "No material uncertainty recorded."}</p>
            </section>
          </div>

          {result.status === "Active" ? (
            <footer className="full-result-actions">
              <span>Analysis only · no order or automatic trading</span>
              <button type="button" onClick={cancelAnalysis} disabled={running}>Cancel Target</button>
            </footer>
          ) : null}
        </article>
      ) : (
        <div className="analysis-empty analyst-empty-compact">
          <strong>{loading ? "Loading Target Analyst history…" : "No Target Analyst result yet"}</strong>
          <span>Request a target after all eight workspaces are configured.</span>
        </div>
      )}

      {history.length > 0 ? (
        <section className="full-history">
          <div className="analysis-section-title"><span>Target history</span><small>Preserved snapshots</small></div>
          <div>
            {history.map((item) => (
              <button type="button" onClick={() => { setResult(item); onResultChange?.(item); }} aria-pressed={result?.id === item.id} key={item.id}>
                <strong className={`${targetTextTone(targetDirection(item))}-text`}>{item.targetPrice === null ? "No target" : formatPrice(item.targetPrice)}</strong>
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

function targetDirection(result: TargetAnalysisResult | null): "up" | "down" | "none" {
  if (!result || result.targetPrice === null || result.currentPrice === null) return "none";
  return result.targetPrice >= result.currentPrice ? "up" : "down";
}

function targetSignal(result: TargetAnalysisResult | null): "BUY" | "SELL" | "WAIT" {
  const direction = targetDirection(result);
  if (direction === "up") return "BUY";
  if (direction === "down") return "SELL";
  return "WAIT";
}

function targetTone(direction: "up" | "down" | "none"): string {
  if (direction === "up") return "complete";
  if (direction === "down") return "negative";
  return "forming";
}

function targetTextTone(direction: "up" | "down" | "none"): string {
  if (direction === "up") return "positive";
  if (direction === "down") return "negative";
  return "warning";
}

function candidateTone(hasCandidate: boolean): string {
  return hasCandidate ? "positive" : "warning";
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
  return error instanceof Error ? error.message : "Target Analyst request failed.";
}

function providerButtonLabel(status: AiProviderAccountStatus | null, fallback: string): string {
  if (!status) return fallback;
  return status.state === "QuotaExhausted" ? "Daily quota exhausted" : "AI provider unavailable";
}
