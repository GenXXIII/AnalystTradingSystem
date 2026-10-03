export interface ApiSuccessResponse<T> {
  success: true;
  data: T;
  traceId: string;
}

export interface ApiErrorResponse {
  success: false;
  error: {
    code: string;
    message: string;
  };
  traceId: string;
}
