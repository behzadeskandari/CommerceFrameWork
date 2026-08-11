import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiClientError, ApiErrorBody, ConsoleLogger } from '@commerce/core';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const logger = inject(ConsoleLogger);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const body = error.error as ApiErrorBody | undefined;
        const message = body?.error ?? error.statusText ?? 'An unexpected error occurred.';
        logger.warn('API request failed', { url: request.url, status: error.status, message });
        return throwError(() => new ApiClientError(message, error.status, body));
      }

      logger.error('Unexpected API error', error);
      return throwError(() => error);
    })
  );
};
