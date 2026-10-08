"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  getFullAnalysisHistory,
  getFullWorkspaceConfiguration,
  type FullAnalysisResult,
  type FullWorkspaceConfiguration,
} from "@/features/full-analysis/api/full-analyst";
import {
  getTargetHistory,
  getTargetWorkspaceConfiguration,
  type TargetAnalysisResult,
  type TargetWorkspaceConfiguration,
} from "@/features/target-analysis/api/target-analyst";
import { ApiClientError } from "@/lib/api/errors";

export interface AnalystHealthSnapshot {
  targetConfiguration: TargetWorkspaceConfiguration[];
  targetStoredResults: number;
  latestTarget: TargetAnalysisResult | null;
  futureConfiguration: FullWorkspaceConfiguration[];
  futureStoredResults: number;
  latestFuture: FullAnalysisResult | null;
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
      const [targetConfiguration, targetHistory, futureConfiguration, futureHistory] = await Promise.all([
        getTargetWorkspaceConfiguration(),
        getTargetHistory(1),
        getFullWorkspaceConfiguration(),
        getFullAnalysisHistory(1),
      ]);
      if (sequence !== requestSequence.current) return;
      setSnapshot({
        targetConfiguration,
        targetStoredResults: targetHistory.totalItems,
        latestTarget: targetHistory.items[0] ?? null,
        futureConfiguration,
        futureStoredResults: futureHistory.totalItems,
        latestFuture: futureHistory.items[0] ?? null,
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
    const timer = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timer);
  }, [enabled, refresh]);

  return { snapshot, loading, error, refresh };
}
