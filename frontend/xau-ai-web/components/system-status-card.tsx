"use client";

import { useApiStatus } from "@/hooks/use-api-status";
import { DISPLAY_TIME_ZONE, DISPLAY_TIME_ZONE_LABEL } from "@/lib/time/utc-plus-seven";

export function SystemStatusCard() {
  const { state, status, error, refresh } = useApiStatus();
  const checkedAt = status
      ? new Intl.DateTimeFormat(undefined, {
        dateStyle: "medium",
        timeStyle: "medium",
        timeZone: DISPLAY_TIME_ZONE,
      }).format(new Date(status.checkedAtUtc)) + ` ${DISPLAY_TIME_ZONE_LABEL}`
    : "Waiting for API";

  return (
    <aside className="status-card" aria-live="polite">
      <div className="status-card-header">
        <h2>Foundation status</h2>
        <span className="live-dot" data-state={state} aria-label={`API ${state}`} />
      </div>

      <p className="status-label">API connection</p>
      <p className="status-value">{state}</p>

      <div className="status-line">
        <span>Service</span>
        <strong>{status?.service ?? "XAUUSD AI API"}</strong>
      </div>
      <div className="status-line">
        <span>Last check</span>
        <strong>{checkedAt}</strong>
      </div>

      {error ? <p className="status-error">{error}</p> : null}

      <div className="status-actions">
        <span>REST · JSON · Traceable</span>
        <button type="button" onClick={refresh} disabled={state === "checking"}>
          Check again
        </button>
      </div>
    </aside>
  );
}
