import { apiClient } from "@/lib/api/client";
import type { ApiSuccessResponse } from "@/types/api";
import type { SystemStatus } from "@/types/system";

export async function getSystemStatus(): Promise<SystemStatus> {
  const response = await apiClient.get<ApiSuccessResponse<SystemStatus>>(
    "/api/system/status",
  );

  return response.data;
}
