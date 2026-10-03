"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  getMultiTimeframeAnalysis,
  getTechnicalAnalysis,
} from "@/features/analysis/api/get-technical-analysis";
import {
  getLatestStoredCandles,
  getMarketPipelineStatus,
  synchronizeMarketData,
  type MarketTimeframeCode,
} from "@/features/market/api/get-pipeline-data";
import { getMarketProviderStatus, getXauUsdQuote } from "@/features/market/api/get-market-data";
import { ApiClientError } from "@/lib/api/errors";
import type { MultiTimeframeAnalysis, TechnicalAnalysis } from "@/types/analysis";
import type {
  MarketDataPipelineResult,
  MarketDataPipelineStatus,
  MarketProviderStatus,
  MarketQuote,
  StoredMarketCandle,
} from "@/types/market";

type TerminalLoadState = "loading" | "ready" | "unavailable";
const LIVE_SNAPSHOT_CANDLE_LIMIT = 3;

export function useMarketTerminal() {
  const [timeframe, setTimeframe] = useState<MarketTimeframeCode>("M15");
  const [state, setState] = useState<TerminalLoadState>("loading");
  const [provider, setProvider] = useState<MarketProviderStatus | null>(null);
  const [quote, setQuote] = useState<MarketQuote | null>(null);
  const [candles, setCandles] = useState<StoredMarketCandle[]>([]);
  const [pipeline, setPipeline] = useState<MarketDataPipelineStatus | null>(null);
  const [lastSync, setLastSync] = useState<MarketDataPipelineResult | null>(null);
  const [analysis, setAnalysis] = useState<TechnicalAnalysis | null>(null);
  const [multiTimeframe, setMultiTimeframe] = useState<MultiTimeframeAnalysis | null>(null);
  const [analysisError, setAnalysisError] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSyncing, setIsSyncing] = useState(false);
  const requestSequence = useRef(0);
  const liveSnapshotSequence = useRef(0);
  const liveQuoteSequence = useRef(0);
  const latestQuote = useRef<MarketQuote | null>(null);

  const load = useCallback(async (selectedTimeframe: MarketTimeframeCode, showLoading = true) => {
    const sequence = ++requestSequence.current;
    if (showLoading) setState("loading");

    const [providerResult, quoteResult, candlesResult, pipelineResult, analysisResult, multiTimeframeResult] = await Promise.allSettled([
      getMarketProviderStatus(),
      getXauUsdQuote(),
      getLatestStoredCandles(selectedTimeframe),
      getMarketPipelineStatus(selectedTimeframe),
      getTechnicalAnalysis(selectedTimeframe),
      getMultiTimeframeAnalysis(),
    ]);

    if (sequence !== requestSequence.current) return;

    if (providerResult.status === "fulfilled") setProvider(providerResult.value);
    if (quoteResult.status === "fulfilled") {
      latestQuote.current = quoteResult.value;
      setQuote(quoteResult.value);
    }
    if (candlesResult.status === "fulfilled") {
      setCandles(applyLiveQuote(candlesResult.value, latestQuote.current));
    }
    if (pipelineResult.status === "fulfilled") setPipeline(pipelineResult.value);
    if (analysisResult.status === "fulfilled") setAnalysis(analysisResult.value);
    if (multiTimeframeResult.status === "fulfilled") setMultiTimeframe(multiTimeframeResult.value);

    const analysisFailures = [analysisResult, multiTimeframeResult]
      .filter((result): result is PromiseRejectedResult => result.status === "rejected")
      .map((result) => toErrorMessage(result.reason));
    setAnalysisError(analysisFailures.length > 0 ? [...new Set(analysisFailures)].join(" ") : null);

    const failures = [providerResult, quoteResult, candlesResult, pipelineResult]
      .filter((result): result is PromiseRejectedResult => result.status === "rejected")
      .map((result) => toErrorMessage(result.reason));
    const uniqueFailures = [...new Set(failures)];
    setError(uniqueFailures.length > 0 ? uniqueFailures.join(" ") : null);
    setState(candlesResult.status === "fulfilled" || quoteResult.status === "fulfilled" ? "ready" : "unavailable");
  }, []);

  const loadLiveSnapshot = useCallback(async (selectedTimeframe: MarketTimeframeCode) => {
    const sequence = ++liveSnapshotSequence.current;
    const [candlesResult, pipelineResult] = await Promise.allSettled([
      getLatestStoredCandles(selectedTimeframe, LIVE_SNAPSHOT_CANDLE_LIMIT),
      getMarketPipelineStatus(selectedTimeframe),
    ]);

    if (sequence !== liveSnapshotSequence.current) return;

    if (candlesResult.status === "fulfilled") {
      setCandles((current) => applyLiveQuote(
        mergeStoredCandles(current, candlesResult.value),
        latestQuote.current,
      ));
    }
    if (pipelineResult.status === "fulfilled") setPipeline(pipelineResult.value);
  }, []);

  const loadLiveQuote = useCallback(async () => {
    const sequence = ++liveQuoteSequence.current;
    try {
      const result = await getXauUsdQuote();
      if (sequence !== liveQuoteSequence.current) return;
      latestQuote.current = result;
      setQuote(result);
      setCandles((current) => applyLiveQuote(current, result));
    } catch {
      // Keep the last good quote and let the normal status refresh report provider failures.
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(timeframe), 0);
    return () => window.clearTimeout(timer);
  }, [load, timeframe]);

  useEffect(() => {
    const timer = window.setInterval(() => void load(timeframe, false), 30_000);
    return () => window.clearInterval(timer);
  }, [load, timeframe]);

  useEffect(() => {
    const timer = window.setInterval(() => void loadLiveSnapshot(timeframe), 5_000);
    return () => {
      window.clearInterval(timer);
      liveSnapshotSequence.current += 1;
    };
  }, [loadLiveSnapshot, timeframe]);

  useEffect(() => {
    let cancelled = false;
    let timer = 0;
    const poll = async () => {
      if (document.visibilityState === "visible") await loadLiveQuote();
      if (!cancelled) timer = window.setTimeout(() => void poll(), 500);
    };
    timer = window.setTimeout(() => void poll(), 2_000);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
      liveQuoteSequence.current += 1;
    };
  }, [loadLiveQuote]);

  const refresh = useCallback(() => void load(timeframe), [load, timeframe]);

  const sync = useCallback(async () => {
    setIsSyncing(true);
    setError(null);
    try {
      const result = await synchronizeMarketData(timeframe);
      setLastSync(result);
      await load(timeframe, false);
    } catch (reason) {
      setError(toErrorMessage(reason));
    } finally {
      setIsSyncing(false);
    }
  }, [load, timeframe]);

  return {
    timeframe,
    setTimeframe,
    state,
    provider,
    quote,
    candles,
    pipeline,
    lastSync,
    analysis,
    multiTimeframe,
    analysisError,
    error,
    isSyncing,
    refresh,
    sync,
  };
}

function toErrorMessage(reason: unknown): string {
  return reason instanceof ApiClientError
    ? reason.message
    : "Market data is currently unavailable.";
}

function applyLiveQuote(
  candles: StoredMarketCandle[],
  quote: MarketQuote | null,
): StoredMarketCandle[] {
  if (!quote) return candles;

  const quoteTime = Date.parse(quote.timestampUtc);
  if (!Number.isFinite(quoteTime)) return candles;

  return candles.map((candle) => {
    if (candle.isComplete) return candle;
    const openTime = Date.parse(candle.openTimeUtc);
    const closeTime = Date.parse(candle.closeTimeUtc);
    if (quoteTime < openTime || quoteTime >= closeTime) return candle;

    return {
      ...candle,
      high: Math.max(candle.high, quote.bid),
      low: Math.min(candle.low, quote.bid),
      close: quote.bid,
      fetchedAtUtc: quote.timestampUtc,
    };
  });
}

function mergeStoredCandles(
  current: StoredMarketCandle[],
  incoming: StoredMarketCandle[],
): StoredMarketCandle[] {
  if (incoming.length === 0) return current;
  const timeframe = incoming[0].timeframe;
  const byOpenTime = new Map(
    current
      .filter((candle) => candle.timeframe === timeframe)
      .map((candle) => [candle.openTimeUtc, candle]),
  );
  for (const candle of incoming) byOpenTime.set(candle.openTimeUtc, candle);
  return [...byOpenTime.values()].sort(
    (left, right) => Date.parse(right.openTimeUtc) - Date.parse(left.openTimeUtc),
  );
}
