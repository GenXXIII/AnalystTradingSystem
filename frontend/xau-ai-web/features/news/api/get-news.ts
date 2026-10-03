import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";
import type { NewsSystemStatus, PagedNewsArticles } from "@/types/news";

export async function getLatestNews(pageSize = 12): Promise<PagedNewsArticles> {
  const query = new URLSearchParams({ pageSize: String(pageSize) });
  const response = await apiClient.get<ApiSuccessResponse<PagedNewsArticles>>(
    `/api/news/latest?${query.toString()}`,
  );
  return response.data;
}

export async function getNewsStatus(): Promise<NewsSystemStatus> {
  const response = await apiClient.get<ApiSuccessResponse<NewsSystemStatus>>("/api/news/status");
  return response.data;
}
