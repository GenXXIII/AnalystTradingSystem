import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";
import type {
  MarketCandle,
  MarketProviderStatus,
  MarketQuote,
} from "@/types/market";

export async function getMarketProviderStatus(): Promise<MarketProviderStatus> {
  const response = await apiClient.get<ApiSuccessResponse<MarketProviderStatus>>(
    "/api/mt5/status",
  );

  return response.data;
}

export async function getXauUsdQuote(): Promise<MarketQuote> {
  const response = await apiClient.get<ApiSuccessResponse<MarketQuote>>(
    "/api/market/xauusd/quote",
  );

  return response.data;
}

export async function getRecentXauUsdCandles(): Promise<MarketCandle[]> {
  const to = new Date();
  const from = new Date(to.getTime() - 24 * 60 * 60 * 1_000);
  const query = new URLSearchParams({
    timeframe: "H1",
    from: from.toISOString(),
    to: to.toISOString(),
  });
  const response = await apiClient.get<ApiSuccessResponse<MarketCandle[]>>(
    `/api/market/xauusd/candles?${query.toString()}`,
  );

  return response.data;
}
