import { environment } from "@/config/environment";
import { ApiClientError } from "./errors";

const defaultRequestTimeoutMilliseconds = 8_000;

class ApiClient {
  constructor(private readonly baseUrl: string) {}

  get<TResponse>(path: string, init?: RequestInit, timeoutMilliseconds = defaultRequestTimeoutMilliseconds): Promise<TResponse> {
    return this.request<TResponse>(path, { ...init, method: "GET" }, timeoutMilliseconds);
  }

  post<TResponse, TRequest>(
    path: string,
    body: TRequest,
    init?: RequestInit,
    timeoutMilliseconds = defaultRequestTimeoutMilliseconds,
  ): Promise<TResponse> {
    return this.request<TResponse>(path, {
      ...init,
      method: "POST",
      body: JSON.stringify(body),
      headers: {
        "Content-Type": "application/json",
        ...init?.headers,
      },
    }, timeoutMilliseconds);
  }

  private async request<TResponse>(
    path: string,
    init: RequestInit,
    timeoutMilliseconds: number,
  ): Promise<TResponse> {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMilliseconds);

    try {
      const response = await fetch(`${this.baseUrl}${normalizePath(path)}`, {
        ...init,
        cache: "no-store",
        headers: {
          Accept: "application/json",
          ...init.headers,
        },
        signal: controller.signal,
      });

      const body: unknown = await readJson(response);
      if (!response.ok) {
        throw ApiClientError.fromResponse(response, body);
      }

      return body as TResponse;
    } catch (error) {
      if (error instanceof ApiClientError) {
        throw error;
      }

      if (error instanceof DOMException && error.name === "AbortError") {
        throw new ApiClientError("The API request timed out.", "REQUEST_TIMEOUT", null);
      }

      throw new ApiClientError("The API is currently unreachable.", "NETWORK_ERROR", null);
    } finally {
      clearTimeout(timeout);
    }
  }
}

function normalizePath(path: string): string {
  return path.startsWith("/") ? path : `/${path}`;
}

async function readJson(response: Response): Promise<unknown> {
  const contentType = response.headers.get("content-type");
  if (!contentType?.includes("application/json")) {
    return null;
  }

  return response.json();
}

export const apiClient = new ApiClient(environment.apiBaseUrl);
