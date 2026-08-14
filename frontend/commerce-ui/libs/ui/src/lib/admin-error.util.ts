import { ApiClientError } from '@commerce/core';

export function resolveAdminError(error: unknown, fallback: string): string {
  return error instanceof ApiClientError ? error.message : fallback;
}
