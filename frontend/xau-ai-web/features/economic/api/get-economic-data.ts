import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";

export interface EconomicSeries {
  id: string;
  provider: string;
  externalSeriesId: string;
  name: string;
  units: string;
  frequency: string;
  category: string;
  isActive: boolean;
}

export interface EconomicObservation {
  id: string;
  economicSeriesId: string;
  externalSeriesId: string;
  seriesName: string;
  observationDate: string;
  value: number | null;
  originalValue: string;
  status: string;
  fetchedAtUtc: string;
  revisionCount: number;
}

export interface EconomicSystemStatus {
  provider: {
    provider: string;
    state: string;
    enabled: boolean;
    message: string;
    checkedAtUtc: string;
  };
  series: Array<{
    externalSeriesId: string;
    seriesName: string;
    status: string;
    lastAttemptAtUtc: string | null;
    lastSuccessfulSyncAtUtc: string | null;
    lastObservationDate: string | null;
    consecutiveFailures: number;
    lastErrorCode: string | null;
    lastErrorMessage: string | null;
  }>;
  configuredSeries: number;
  storedSeries: number;
  storedObservations: number;
}

export async function getEconomicSeries(): Promise<EconomicSeries[]> {
  const response = await apiClient.get<ApiSuccessResponse<EconomicSeries[]>>(
    "/api/economic-data/series",
  );
  return response.data;
}

export async function getLatestEconomicObservations(): Promise<EconomicObservation[]> {
  const response = await apiClient.get<ApiSuccessResponse<EconomicObservation[]>>(
    "/api/economic-data/latest",
  );
  return response.data;
}

export async function getEconomicStatus(): Promise<EconomicSystemStatus> {
  const response = await apiClient.get<ApiSuccessResponse<EconomicSystemStatus>>(
    "/api/economic-data/status",
  );
  return response.data;
}
