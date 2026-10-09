"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  getFullAnalysisHistory,
  getFullProviderStatuses,
  getFullWorkspaceConfiguration,
  type FullAnalysisResult,
  type FullWorkspaceConfiguration,
} from "@/features/full-analysis/api/full-analyst";
import {
  getTargetHistory,
  getTargetProviderStatuses,
  getTargetWorkspaceConfiguration,
  type TargetAnalysisResult,
  type TargetWorkspaceConfiguration,
} from "@/features/target-analysis/api/target-analyst";
import { ApiClientError } from "@/lib/api/errors";
import type { AiProviderAccountStatus } from "@/features/analysis/api/ai-provider-status";

export interface AnalystHealthSnapshot {
  targetConfiguration: TargetWorkspaceConfiguration[];
  targetStoredResults: number;
  latestTarget: TargetAnalysisResult | null;
  targetProviderStatuses: AiProviderAccountStatus[];
  futureConfiguration: FullWorkspaceConfiguration[];
  futureStoredResults: number;
  latestFuture: FullAnalysisResult | null;
  futureProviderStatuses: AiProviderAccountStatus[];
}

export function useAnalystHealth(enabled: boolean) {
  const [snapshot, setSnapshot] = useState<AnalystHealthSnapshot | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestSequence = useRef(0);

  const refresh = useCallback(async () => {
    const sequence = ++requestSequence.current;
    setLoading(true);
    setError(null);
    try {
      const [targetConfiguration, targetHistory, targetProviderStatuses, futureConfiguration, futureHistory, futureProviderStatuses] = await Promise.all([
        getTargetWorkspaceConfiguration(),
        getTargetHistory(1),
        getTargetProviderStatuses(),
        getFullWorkspaceConfiguration(),
        getFullAnalysisHistory(1),
        getFullProviderStatuses(),
      ]);
      if (sequence !== requestSequence.current) return;
      setSnapshot({
        targetConfiguration,
        targetStoredResults: targetHistory.totalItems,
        latestTarget: targetHistory.items[0] ?? null,
        targetProviderStatuses,
        futureConfiguration,
        futureStoredResults: futureHistory.totalItems,
        latestFuture: futureHistory.items[0] ?? null,
        futureProviderStatuses,
      });
    } catch (reason) {
      if (sequence !== requestSequence.current) return;
      setError(reason instanceof ApiClientError ? reason.message : "Analyst health is currently unavailable.");
    } finally {
      if (sequence === requestSequence.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!enabled) return;
    const refreshWhenVisible = () => {
      if (document.visibilityState === "visible") void refresh();
    };
    const initialTimer = window.setTimeout(() => void refresh(), 0);
    const refreshTimer = window.setInterval(() => void refresh(), 30_000);
    window.addEventListener("focus", refreshWhenVisible);
    document.addEventListener("visibilitychange", refreshWhenVisible);
    return () => {
      window.clearTimeout(initialTimer);
      window.clearInterval(refreshTimer);
      window.removeEventListener("focus", refreshWhenVisible);
      document.removeEventListener("visibilitychange", refreshWhenVisible);
    };
  }, [enabled, refresh]);

  return { snapshot, loading, error, refresh };
}
