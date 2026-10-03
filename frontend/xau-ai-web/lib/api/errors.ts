import type { ApiErrorResponse } from "@/types/api";

export class ApiClientError extends Error {
  constructor(
    message: string,
    public readonly code: string,
    public readonly status: number | null,
    public readonly traceId?: string,
  ) {
    super(message);
    this.name = "ApiClientError";
  }

  static fromResponse(response: Response, body: unknown): ApiClientError {
    if (isApiErrorResponse(body)) {
      return new ApiClientError(
        body.error.message,
        body.error.code,
        response.status,
        body.traceId,
      );
    }

    return new ApiClientError(
      `The API returned HTTP ${response.status}.`,
      "HTTP_ERROR",
      response.status,
    );
  }
}

function isApiErrorResponse(value: unknown): value is ApiErrorResponse {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Partial<ApiErrorResponse>;
  return (
    candidate.success === false &&
    typeof candidate.traceId === "string" &&
    typeof candidate.error?.code === "string" &&
    typeof candidate.error.message === "string"
  );
}
