"use client";

import { useCallback, useEffect, useState } from "react";
import {
  getMarketProviderStatus,
  getRecentXauUsdCandles,
  getXauUsdQuote,
} from "@/features/market/api/get-market-data";
import { ApiClientError } from "@/lib/api/errors";
import type {
  MarketCandle,
  MarketProviderStatus,
  MarketQuote,
} from "@/types/market";

type MarketDataLoadState = "loading" | "ready" | "unavailable";
type MarketDataSnapshot = {
  provider: MarketProviderStatus;
  quote: MarketQuote | null;
  candles: MarketCandle[];
  error: string | null;
};

export function useMarketData() {
  const [state, setState] = useState<MarketDataLoadState>("loading");
  const [provider, setProvider] = useState<MarketProviderStatus | null>(null);
  const [quote, setQuote] = useState<MarketQuote | null>(null);
  const [candles, setCandles] = useState<MarketCandle[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    void fetchMarketData().then(
      (result) => {
        if (active) applySnapshot(result, setProvider, setQuote, setCandles, setError, setState);
      },
      (reason: unknown) => {
        if (active) applyFailure(reason, setQuote, setCandles, setError, setState);
      },
    );

    return () => {
      active = false;
    };
  }, []);

  const refresh = useCallback(() => {
    setState("loading");
    setError(null);
    void fetchMarketData().then(
      (result) => applySnapshot(result, setProvider, setQuote, setCandles, setError, setState),
      (reason: unknown) => applyFailure(reason, setQuote, setCandles, setError, setState),
    );
  }, []);

  return { state, provider, quote, candles, error, refresh };
}

async function fetchMarketData(): Promise<MarketDataSnapshot> {
  const provider = await getMarketProviderStatus();
  if (!provider.connected) {
    return { provider, quote: null, candles: [], error: provider.message };
  }

  const [quote, candles] = await Promise.all([
    getXauUsdQuote(),
    getRecentXauUsdCandles(),
  ]);

  return { provider, quote, candles, error: null };
}

function applySnapshot(
  snapshot: MarketDataSnapshot,
  setProvider: (value: MarketProviderStatus) => void,
  setQuote: (value: MarketQuote | null) => void,
  setCandles: (value: MarketCandle[]) => void,
  setError: (value: string | null) => void,
  setState: (value: MarketDataLoadState) => void,
) {
  setProvider(snapshot.provider);
  setQuote(snapshot.quote);
  setCandles(snapshot.candles);
  setError(snapshot.error);
  setState(snapshot.error ? "unavailable" : "ready");
}

function applyFailure(
  reason: unknown,
  setQuote: (value: MarketQuote | null) => void,
  setCandles: (value: MarketCandle[]) => void,
  setError: (value: string | null) => void,
  setState: (value: MarketDataLoadState) => void,
) {
  setQuote(null);
  setCandles([]);
  setError(toErrorMessage(reason));
  setState("unavailable");
}

function toErrorMessage(reason: unknown): string {
  return reason instanceof ApiClientError
    ? reason.message
    : "Market data is currently unavailable.";
}
