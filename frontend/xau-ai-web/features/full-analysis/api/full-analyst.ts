import type { MarketTimeframeCode } from "@/features/market/api/get-pipeline-data";
import type { AiProviderAccountStatus } from "@/features/analysis/api/ai-provider-status";
import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";

export type FullDecision = "Buy" | "Sell" | "Wait";
export type FullAnalysisStatus = "Analyzing" | "Active" | "Wait" | "Cancelled" | "Expired" | "Invalidated" | "Completed";
export type FullWorkspace = "Structure" | "Liquidity" | "Candle" | "Flow" | "Ktr" | "News" | "Risk" | "Master";

export interface FullInvalidation {
  summary: string;
  price: number | null;
  condition: "None" | "AtOrBelow" | "AtOrAbove";
}

export interface FullSpecialistOutput {
  workspace: FullWorkspace;
  direction: FullDecision;
  evidenceIds: string[];
  keyFindings: string[];
  impact: string;
  confidence: number;
  uncertainty: string;
  invalidation: string;
  summary: string;
  insufficientEvidence: boolean;
}

export interface FullWorkspaceRunResult {
  id: string;
  workspace: FullWorkspace;
  status: "Completed" | "Failed" | "Disabled" | "Cached";
  specialistOutput: FullSpecialistOutput | null;
  configuration: {
    provider: string;
    model: string;
    fallbackModels: string[];
  };
  cacheHit: boolean;
  inputTokens: number | null;
  outputTokens: number | null;
  latencyMilliseconds: number | null;
  errorCode: string | null;
  errorMessage: string | null;
}

export interface FullAnalysisResult {
  id: string;
  symbol: string;
  analysisTimeUtc: string;
  currentPrice: number | null;
  timeframe: MarketTimeframeCode;
  decision: FullDecision;
  confidence: number | null;
  agreement: number | null;
  conflicts: string[];
  keyEvidenceIds: string[];
  reasoning: string;
  invalidation: FullInvalidation | null;
  uncertainty: string;
  validUntilUtc: string | null;
  futureAvailable: boolean;
  workspaceResults: FullWorkspaceRunResult[];
  status: FullAnalysisStatus;
  provider: string;
  model: string;
  promptVersion: string;
  configurationVersion: string;
  inputTokens: number;
  outputTokens: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  endedAtUtc: string | null;
}

export interface PagedFullAnalyses {
  items: FullAnalysisResult[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface FullWorkspaceConfiguration {
  workspace: FullWorkspace;
  enabled: boolean;
  provider: string;
  adapter: string;
  model: string;
  fallbackModels: string[];
  baseUrl: string;
  maxOutputTokens: number;
  requestsPerMinute: number;
  promptVersion: string;
  configurationVersion: string;
  hasApiKey: boolean;
}

const analysisTimeoutMilliseconds = 15 * 60 * 1_000;

export async function createFullAnalysis(timeframe: MarketTimeframeCode): Promise<FullAnalysisResult> {
  const response = await apiClient.post<
    ApiSuccessResponse<FullAnalysisResult>,
    { symbol: string; timeframe: MarketTimeframeCode }
  >(
    "/api/full-analyst/jobs",
    { symbol: "XAUUSD", timeframe },
    undefined,
    analysisTimeoutMilliseconds,
  );
  return response.data;
}

export async function getActiveFullAnalyses(): Promise<FullAnalysisResult[]> {
  const response = await apiClient.get<ApiSuccessResponse<FullAnalysisResult[]>>(
    "/api/full-analyst/active/XAUUSD",
  );
  return response.data;
}

export async function getFullAnalysisHistory(
  pageSize = 12,
  timeframe?: MarketTimeframeCode,
): Promise<PagedFullAnalyses> {
  const query = new URLSearchParams({ page: "1", pageSize: String(pageSize) });
  if (timeframe) query.set("timeframe", timeframe);
  const response = await apiClient.get<ApiSuccessResponse<PagedFullAnalyses>>(
    `/api/full-analyst/history/XAUUSD?${query.toString()}`,
  );
  return response.data;
}

export async function getFullWorkspaceConfiguration(): Promise<FullWorkspaceConfiguration[]> {
  const response = await apiClient.get<ApiSuccessResponse<FullWorkspaceConfiguration[]>>(
    "/api/full-analyst/configuration",
  );
  return response.data;
}

export async function getFullProviderStatuses(): Promise<AiProviderAccountStatus[]> {
  const response = await apiClient.get<ApiSuccessResponse<AiProviderAccountStatus[]>>(
    "/api/full-analyst/provider-status",
  );
  return response.data;
}

export async function cancelFullAnalysis(id: string): Promise<FullAnalysisResult> {
  const response = await apiClient.post<
    ApiSuccessResponse<FullAnalysisResult>,
    Record<string, never>
  >(`/api/full-analyst/jobs/${id}/cancel`, {});
  return response.data;
}
