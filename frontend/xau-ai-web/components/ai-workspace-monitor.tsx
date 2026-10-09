"use client";

import type { CSSProperties } from "react";
import type { AnalystHealthSnapshot } from "@/hooks/use-analyst-health";
import { DISPLAY_TIME_ZONE, DISPLAY_TIME_ZONE_LABEL } from "@/lib/time/utc-plus-seven";

const integer = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 });
const displayDate = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: DISPLAY_TIME_ZONE,
});

interface WorkspaceUsageRow {
  key: string;
  scope: "Target" | "Future";
  workspace: string;
  enabled: boolean;
  configured: boolean;
  provider: string;
  model: string;
  primaryModel: string;
  fallbackModels: string[];
  usedFallback: boolean;
  recoveryReady: boolean;
  requestsPerMinute: number;
  maxOutputTokens: number;
  status: string;
  inputTokens: number;
  outputTokens: number;
  latencyMilliseconds: number | null;
  errorCode: string | null;
  errorMessage: string | null;
  detail: string;
}

export function AiWorkspaceMonitor({
  snapshot,
  loading,
  error,
  onRefresh,
}: Readonly<{
  snapshot: AnalystHealthSnapshot | null;
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}>) {
  const rows = snapshot ? workspaceRows(snapshot) : [];
  const configured = rows.filter((item) => item.enabled && item.configured).length;
  const failures = rows.filter((item) => item.errorCode !== null).length;
  const inputTokens = rows.reduce((total, item) => total + item.inputTokens, 0);
  const outputTokens = rows.reduce((total, item) => total + item.outputTokens, 0);
  const outputAllowance = rows.reduce((total, item) => total + (item.enabled && item.configured ? item.maxOutputTokens : 0), 0);
  const usage = ratio(outputTokens, outputAllowance);
  const targetRows = rows.filter((item) => item.scope === "Target");
  const futureRows = rows.filter((item) => item.scope === "Future");

  return (
    <section className="ai-workspace-monitor">
      <header className="provider-health-header">
        <div>
          <span>Target + Future AI operations</span>
          <strong>16 independent workspace monitor</strong>
        </div>
        <button className="secondary-action" type="button" onClick={onRefresh} disabled={loading}>
          {loading ? "Checking…" : "Refresh usage"}
        </button>
      </header>

      {error ? <p className="terminal-error" role="alert">{error}</p> : null}

      <div className="ai-usage-overview">
        <AiMetric label="Ready" value={`${configured}/16`} detail="Configured workspaces" />
        <AiMetric label="Latest run tokens" value={integer.format(inputTokens + outputTokens)} detail={`${integer.format(inputTokens)} input · ${integer.format(outputTokens)} output`} />
        <AiMetric label="Output usage" value={`${usage}%`} detail={`${integer.format(outputTokens)} / ${integer.format(outputAllowance)} latest-run allowance`} />
        <AiMetric label="Errors" value={integer.format(failures)} detail={failures === 0 ? "No latest workspace failures" : "Open cards below for details"} />
      </div>

      <div className="ai-total-usage">
        <div>
          <span>Total latest-run output utilization</span>
          <strong>{usage}% used</strong>
        </div>
        <UsageBar value={usage} label={`Total output allowance ${usage}% used`} />
        <small>This percentage measures output tokens from the latest persisted Target and Future runs against their configured workspace output allowances. It is not a provider billing quota.</small>
      </div>

      <WorkspaceGroup
        title="Target AI"
        subtitle={latestLabel(snapshot?.latestTarget?.status ?? "No run", snapshot?.latestTarget?.analysisTimeUtc)}
        rows={targetRows}
      />
      <WorkspaceGroup
        title="Future AI"
        subtitle={latestLabel(snapshot?.latestFuture?.status ?? "No run", snapshot?.latestFuture?.analysisTimeUtc)}
        rows={futureRows}
      />

      <div className="ai-provider-summary">
        <ProviderSummary label="Target provider" statuses={snapshot?.targetProviderStatuses ?? []} />
        <ProviderSummary label="Future provider" statuses={snapshot?.futureProviderStatuses ?? []} />
      </div>
    </section>
  );
}

function WorkspaceGroup({ title, subtitle, rows }: Readonly<{
  title: string;
  subtitle: string;
  rows: WorkspaceUsageRow[];
}>) {
  return (
    <section className="ai-workspace-group">
      <div className="analysis-section-title"><span>{title}</span><small>{subtitle}</small></div>
      <div className="ai-workspace-grid">
        {rows.map((row) => {
          const usage = ratio(row.outputTokens, row.maxOutputTokens);
          const state = workspaceState(row);
          return (
            <article data-state={state} key={row.key}>
              <header>
                <div><span>{row.workspace}</span><strong>{row.status}</strong></div>
                <span className={`state-chip ${state === "healthy" ? "complete" : state === "failed" ? "negative" : "forming"}`}>{usage}%</span>
              </header>
              <UsageBar value={usage} label={`${row.scope} ${row.workspace} output allowance ${usage}% used`} />
              <dl>
                <div><dt>Tokens</dt><dd>{integer.format(row.inputTokens)} in · {integer.format(row.outputTokens)} out</dd></div>
                <div><dt>Limit</dt><dd>{integer.format(row.maxOutputTokens)} output</dd></div>
                <div><dt>Latency</dt><dd>{row.latencyMilliseconds === null ? "—" : `${integer.format(row.latencyMilliseconds)} ms`}</dd></div>
                <div><dt>Rate</dt><dd>{integer.format(row.requestsPerMinute)} RPM</dd></div>
              </dl>
              <p title={workspaceDetail(row)}>{workspaceDetail(row)}</p>
              <footer><span>{row.provider}</span><span title={row.model}>{row.model}</span></footer>
            </article>
          );
        })}
      </div>
    </section>
  );
}

function AiMetric({ label, value, detail }: Readonly<{ label: string; value: string; detail: string }>) {
  return <div><span>{label}</span><strong>{value}</strong><small>{detail}</small></div>;
}

function UsageBar({ value, label }: Readonly<{ value: number; label: string }>) {
  return (
    <div className="ai-usage-track" role="progressbar" aria-label={label} aria-valuemin={0} aria-valuemax={100} aria-valuenow={value}>
      <span style={{ "--ai-usage": `${value}%` } as CSSProperties} />
    </div>
  );
}

function ProviderSummary({ label, statuses }: Readonly<{
  label: string;
  statuses: AnalystHealthSnapshot["targetProviderStatuses"];
}>) {
  const status = statuses[0] ?? null;
  return (
    <div>
      <span>{label}</span>
      <strong>{status ? `${status.provider} · ${status.state}` : "Checking"}</strong>
      <small>{status?.message ?? "Provider status has not loaded."}</small>
    </div>
  );
}

function workspaceRows(snapshot: AnalystHealthSnapshot): WorkspaceUsageRow[] {
  const targetRuns = new Map(snapshot.latestTarget?.specialistResults.map((item) => [item.workspace, item]) ?? []);
  const futureRuns = new Map(snapshot.latestFuture?.workspaceResults.map((item) => [item.workspace, item]) ?? []);
  const target = snapshot.targetConfiguration.map((configuration) => {
    const run = targetRuns.get(configuration.workspace);
    const model = run?.model ?? configuration.model;
    const usedFallback = Boolean(run)
      && run?.status !== "Failed"
      && !sameModel(model, configuration.model);
    const recoveryReady = recoveryReadyForNextRun(
      run?.status,
      run?.errorCode,
      run?.errorMessage,
      configuration.fallbackModels,
    );
    return {
      key: `target-${configuration.workspace}`,
      scope: "Target" as const,
      workspace: configuration.workspace,
      enabled: configuration.enabled,
      configured: configuration.hasApiKey,
      provider: run?.provider ?? configuration.provider,
      model,
      primaryModel: configuration.model,
      fallbackModels: configuration.fallbackModels,
      usedFallback,
      recoveryReady,
      requestsPerMinute: configuration.requestsPerMinute,
      maxOutputTokens: configuration.maxOutputTokens,
      status: usedFallback
        ? "Recovered"
        : recoveryReady ? "Historical fail" : run?.status ?? (configuration.enabled ? "Not run" : "Disabled"),
      inputTokens: run?.inputTokens ?? 0,
      outputTokens: run?.outputTokens ?? 0,
      latencyMilliseconds: run?.latencyMilliseconds ?? null,
      errorCode: run?.errorCode ?? null,
      errorMessage: run?.errorMessage ?? null,
      detail: run?.summary || run?.uncertainty || "Not used by the latest staged run.",
    };
  });
  const future = snapshot.futureConfiguration.map((configuration) => {
    const run = futureRuns.get(configuration.workspace);
    const model = run?.configuration?.model ?? configuration.model;
    const usedFallback = Boolean(run)
      && run?.status !== "Failed"
      && !sameModel(model, configuration.model);
    const recoveryReady = recoveryReadyForNextRun(
      run?.status,
      run?.errorCode,
      run?.errorMessage,
      configuration.fallbackModels,
    );
    return {
      key: `future-${configuration.workspace}`,
      scope: "Future" as const,
      workspace: configuration.workspace,
      enabled: configuration.enabled,
      configured: configuration.hasApiKey,
      provider: run?.configuration?.provider ?? configuration.provider,
      model,
      primaryModel: configuration.model,
      fallbackModels: configuration.fallbackModels,
      usedFallback,
      recoveryReady,
      requestsPerMinute: configuration.requestsPerMinute,
      maxOutputTokens: configuration.maxOutputTokens,
      status: usedFallback
        ? "Recovered"
        : recoveryReady ? "Historical fail" : run?.status ?? (configuration.enabled ? "Not run" : "Disabled"),
      inputTokens: run?.inputTokens ?? 0,
      outputTokens: run?.outputTokens ?? 0,
      latencyMilliseconds: run?.latencyMilliseconds ?? null,
      errorCode: run?.errorCode ?? null,
      errorMessage: run?.errorMessage ?? null,
      detail: run?.cacheHit ? "Reused a matching persisted result." : run?.specialistOutput?.summary ?? "Not used by the latest staged run.",
    };
  });
  return [...target, ...future];
}

function workspaceState(row: WorkspaceUsageRow): "healthy" | "failed" | "idle" {
  if (row.status === "Failed" || row.errorCode) return "failed";
  if (row.status === "Completed" || row.status === "Cached" || row.status === "Recovered") return "healthy";
  return "idle";
}

function workspaceDetail(row: WorkspaceUsageRow): string {
  if (row.errorCode) {
    const failure = `${row.errorCode}${row.errorMessage ? ` · ${row.errorMessage}` : ""}`;
    if (row.recoveryReady) {
      return `${failure} This stored run predates automatic recovery. The next Generate will try: ${[row.primaryModel, ...row.fallbackModels].join(" → ")}.`;
    }

    return recoverableError(row.errorCode) && row.fallbackModels.length > 0
      ? `${failure} Attempted recovery path: ${[row.primaryModel, ...row.fallbackModels].join(" → ")}.`
      : failure;
  }

  if (row.usedFallback) {
    return `Recovered automatically: ${row.primaryModel} was replaced by ${row.model}.`;
  }

  if (row.status === "Not run" && row.fallbackModels.length > 0) {
    return `${row.fallbackModels.length} automatic fallback model${row.fallbackModels.length === 1 ? "" : "s"} ready.`;
  }

  return row.detail;
}

function sameModel(left: string, right: string): boolean {
  return left.trim().toLowerCase() === right.trim().toLowerCase();
}

function recoverableError(errorCode: string): boolean {
  return errorCode.endsWith("_RATE_LIMITED")
    || errorCode.endsWith("_TIMEOUT")
    || errorCode.endsWith("_TOKEN_LIMIT")
    || errorCode.endsWith("_INVALID_RESPONSE")
    || errorCode.endsWith("_UNAVAILABLE");
}

function recoveryReadyForNextRun(
  status: string | undefined,
  errorCode: string | null | undefined,
  errorMessage: string | null | undefined,
  fallbackModels: string[],
): boolean {
  return status === "Failed"
    && Boolean(errorCode && recoverableError(errorCode))
    && fallbackModels.length > 0
    && !errorMessage?.includes("Automatic recovery exhausted");
}

function ratio(value: number, maximum: number): number {
  if (maximum <= 0) return 0;
  return Math.min(100, Math.max(0, Math.round((value / maximum) * 100)));
}

function latestLabel(status: string, timestamp?: string): string {
  return timestamp
    ? `${status} · ${displayDate.format(new Date(timestamp))} ${DISPLAY_TIME_ZONE_LABEL}`
    : status;
}
