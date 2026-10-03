import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";
import type { MultiTimeframeAnalysis, TechnicalAnalysis } from "@/types/analysis";
import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";

export async function getTechnicalAnalysis(timeframe: MarketTimeframeCode): Promise<TechnicalAnalysis> {
  const response = await apiClient.get<ApiSuccessResponse<TechnicalAnalysis>>(
    `/api/analysis/XAUUSD/${timeframe}`,
  );
  return response.data;
}

export async function getMultiTimeframeAnalysis(): Promise<MultiTimeframeAnalysis> {
  const response = await apiClient.get<ApiSuccessResponse<MultiTimeframeAnalysis>>(
    "/api/analysis/XAUUSD/multi-timeframe",
  );
  return response.data;
}
