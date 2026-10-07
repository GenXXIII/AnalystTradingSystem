import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";
import type {
  MarketDataPipelineResult,
  MarketDataPipelineStatus,
  MarketDataSourceComparison,
  StoredMarketCandle,
} from "@/types/market";

export const marketTimeframes = ["M1", "M5", "M15", "M30", "H1", "H4", "D1"] as const;
export type MarketTimeframeCode = (typeof marketTimeframes)[number];

const weeklyCandleLimits: Record<MarketTimeframeCode, number> = {
  M1: 5_000,
  M5: 2_017,
  M15: 673,
  M30: 337,
  H1: 169,
  H4: 43,
  D1: 8,
};

export async function getLatestStoredCandles(
  timeframe: MarketTimeframeCode,
  limit = weeklyCandleLimits[timeframe],
): Promise<StoredMarketCandle[]> {
  const query = new URLSearchParams({
    timeframe,
    limit: String(limit),
    completedOnly: "false",
  });
  const response = await apiClient.get<ApiSuccessResponse<StoredMarketCandle[]>>(
    `/api/market-data/XAUUSD/latest?${query.toString()}`,
  );
  return response.data;
}

export async function getMarketPipelineStatus(
  timeframe: MarketTimeframeCode,
): Promise<MarketDataPipelineStatus> {
  const query = new URLSearchParams({ timeframe });
  const response = await apiClient.get<ApiSuccessResponse<MarketDataPipelineStatus>>(
    `/api/market-data/XAUUSD/status?${query.toString()}`,
  );
  return response.data;
}

export async function getMarketSourceComparison(
  timeframe: MarketTimeframeCode,
): Promise<MarketDataSourceComparison> {
  const query = new URLSearchParams({ timeframe, limit: "100" });
  const response = await apiClient.get<ApiSuccessResponse<MarketDataSourceComparison>>(
    `/api/market-data/XAUUSD/source-comparison?${query.toString()}`,
  );
  return response.data;
}

export async function synchronizeMarketData(
  timeframe: MarketTimeframeCode,
): Promise<MarketDataPipelineResult> {
  const response = await apiClient.post<
    ApiSuccessResponse<MarketDataPipelineResult>,
    { timeframe: MarketTimeframeCode; from: null; to: null; includeFormingCandle: true }
  >("/api/market-data/XAUUSD/sync", {
    timeframe,
    from: null,
    to: null,
    includeFormingCandle: true,
  });
  return response.data;
}
