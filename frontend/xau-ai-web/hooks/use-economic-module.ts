"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  getEconomicSeries,
  getEconomicStatus,
  getLatestEconomicObservations,
  type EconomicObservation,
  type EconomicSeries,
  type EconomicSystemStatus,
} from "@/features/economic/api/get-economic-data";
import { ApiClientError } from "@/lib/api/errors";

export function useEconomicModule(enabled: boolean) {
  const [series, setSeries] = useState<EconomicSeries[]>([]);
  const [observations, setObservations] = useState<EconomicObservation[]>([]);
  const [status, setStatus] = useState<EconomicSystemStatus | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestSequence = useRef(0);

  const refresh = useCallback(async () => {
    const sequence = ++requestSequence.current;
    setLoading(true);
    setError(null);
    try {
      const [seriesResult, latestResult, statusResult] = await Promise.all([
        getEconomicSeries(),
        getLatestEconomicObservations(),
        getEconomicStatus(),
      ]);
      if (sequence !== requestSequence.current) return;
      setSeries(seriesResult);
      setObservations(latestResult);
      setStatus(statusResult);
    } catch (reason) {
      if (sequence !== requestSequence.current) return;
      setError(reason instanceof ApiClientError ? reason.message : "Economic data is currently unavailable.");
    } finally {
      if (sequence === requestSequence.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!enabled) return;
    const timer = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timer);
  }, [enabled, refresh]);

  return { series, observations, status, loading, error, refresh };
}
