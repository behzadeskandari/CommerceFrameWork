export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

export interface ApiErrorBody {
  success: false;
  error: string;
}

export class ApiClientError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly body?: ApiErrorBody
  ) {
    super(message);
    this.name = 'ApiClientError';
  }
}
