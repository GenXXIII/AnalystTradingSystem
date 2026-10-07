import { apiClient } from "@/lib/api/client";
import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import type { ApiSuccessResponse } from "@/types/api";

export type LocalSignalState = "Nothing" | "Buy" | "Sell" | "Stop";

export interface LocalSignalCondition {
  component: string;
  state: string;
  longMatched: boolean;
  shortMatched: boolean;
  weight: number;
  evidence: string[];
}

export interface LocalSignalSnapshot {
  signalId: string | null;
  symbol: string;
  timeframe: MarketTimeframeCode;
  signalCandleId: string | null;
  signalCandleTimeUtc: string | null;
  state: LocalSignalState;
  originDirection: LocalSignalState | null;
  signalPrice: number | null;
  score: number;
  maxScore: number;
  confidence: number;
  structureState: string;
  liquidityState: string;
  candleState: string;
  momentumState: string;
  ktrState: string;
  volatilityState: string;
  invalidationPrice: number | null;
  targetPrice: number | null;
  reason: string | null;
  validUntilUtc: string | null;
  status: string;
  configurationVersion: string;
  conditions: LocalSignalCondition[];
  evaluatedAtUtc: string;
  createdAtUtc: string | null;
  updatedAtUtc: string | null;
  endedAtUtc: string | null;
  isCached: boolean;
}

export interface LocalSignalChartMarker {
  signalId: string;
  state: Exclude<LocalSignalState, "Nothing">;
  candleTimeUtc: string;
  price: number | null;
  reason: string | null;
}

export async function getCurrentLocalSignal(
  timeframe: MarketTimeframeCode,
): Promise<LocalSignalSnapshot> {
  const response = await apiClient.get<ApiSuccessResponse<LocalSignalSnapshot>>(
    `/api/local-analyst/XAUUSD/${timeframe}/current`,
  );
  return response.data;
}

export async function getLocalSignalChartMarkers(
  timeframe: MarketTimeframeCode,
  limit = 500,
): Promise<LocalSignalChartMarker[]> {
  const query = new URLSearchParams({ limit: String(limit) });
  const response = await apiClient.get<ApiSuccessResponse<LocalSignalChartMarker[]>>(
    `/api/local-analyst/XAUUSD/${timeframe}/markers?${query.toString()}`,
  );
  return response.data;
}
