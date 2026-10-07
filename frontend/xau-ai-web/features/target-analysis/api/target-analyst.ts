import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";

export type TargetAnalysisStatus =
  | "Analyzing"
  | "Success"
  | "Active"
  | "NoValidTarget"
  | "TargetHit"
  | "Invalidated"
  | "Expired"
  | "Cancelled";

export type TargetWorkspace =
  | "Structure"
  | "Liquidity"
  | "Candle"
  | "Flow"
  | "Ktr"
  | "News"
  | "Risk"
  | "Master";

export interface TargetSpecialistResult {
  id: string;
  workspace: TargetWorkspace;
  status: "Completed" | "Failed" | "Disabled";
  hasCandidate: boolean;
  candidateTargetPrice: number | null;
  candidateInvalidationPrice: number | null;
  directionContext: string;
  confidence: number | null;
  riskAcceptable: boolean | null;
  summary: string;
  uncertainty: string;
  evidenceIds: string[];
  provider: string;
  model: string;
  promptVersion: string;
  configurationVersion: string;
  errorCode: string | null;
  errorMessage: string | null;
  completedAtUtc: string | null;
}

export interface TargetAnalysisResult {
  id: string;
  symbol: string;
  analysisTimeUtc: string;
  currentPrice: number | null;
  timeframe: MarketTimeframeCode;
  targetPrice: number | null;
  invalidationPrice: number | null;
  directionContext: string;
  confidence: number | null;
  validUntilUtc: string | null;
  evidenceIds: string[];
  specialistResults: TargetSpecialistResult[];
  masterResultId: string | null;
  reasoningSummary: string;
  uncertainty: string;
  noTargetReason: string | null;
  status: TargetAnalysisStatus;
  provider: string;
  model: string;
  promptVersion: string;
  configurationVersion: string;
  snapshot: unknown | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  endedAtUtc: string | null;
}

export interface PagedTargetAnalyses {
  items: TargetAnalysisResult[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface TargetWorkspaceConfiguration {
  workspace: TargetWorkspace;
  enabled: boolean;
  provider: string;
  adapter: string;
  model: string;
  baseUrl: string;
  promptVersion: string;
  configurationVersion: string;
  hasApiKey: boolean;
}

const analysisTimeoutMilliseconds = 15 * 60 * 1_000;

export async function createTargetAnalysis(
  timeframe: MarketTimeframeCode,
): Promise<TargetAnalysisResult> {
  const response = await apiClient.post<
    ApiSuccessResponse<TargetAnalysisResult>,
    { symbol: string; timeframe: MarketTimeframeCode }
  >(
    "/api/target-analyst/jobs",
    { symbol: "XAUUSD", timeframe },
    undefined,
    analysisTimeoutMilliseconds,
  );
  return response.data;
}

export async function getActiveTargets(): Promise<TargetAnalysisResult[]> {
  const response = await apiClient.get<ApiSuccessResponse<TargetAnalysisResult[]>>(
    "/api/target-analyst/active/XAUUSD",
  );
  return response.data;
}

export async function getTargetHistory(pageSize = 12): Promise<PagedTargetAnalyses> {
  const query = new URLSearchParams({ page: "1", pageSize: String(pageSize) });
  const response = await apiClient.get<ApiSuccessResponse<PagedTargetAnalyses>>(
    `/api/target-analyst/history/XAUUSD?${query.toString()}`,
  );
  return response.data;
}

export async function getTargetWorkspaceConfiguration(): Promise<TargetWorkspaceConfiguration[]> {
  const response = await apiClient.get<ApiSuccessResponse<TargetWorkspaceConfiguration[]>>(
    "/api/target-analyst/configuration",
  );
  return response.data;
}

export async function cancelTargetAnalysis(id: string): Promise<TargetAnalysisResult> {
  const response = await apiClient.post<
    ApiSuccessResponse<TargetAnalysisResult>,
    Record<string, never>
  >(`/api/target-analyst/jobs/${id}/cancel`, {});
  return response.data;
}
