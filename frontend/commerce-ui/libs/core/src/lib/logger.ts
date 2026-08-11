import { Injectable } from '@angular/core';

export interface Logger {
  debug(message: string, context?: unknown): void;
  info(message: string, context?: unknown): void;
  warn(message: string, context?: unknown): void;
  error(message: string, context?: unknown): void;
}

@Injectable({ providedIn: 'root' })
export class ConsoleLogger implements Logger {
  debug(message: string, context?: unknown): void {
    if (!environmentProduction()) {
      console.debug(message, context);
    }
  }

  info(message: string, context?: unknown): void {
    console.info(message, context);
  }

  warn(message: string, context?: unknown): void {
    console.warn(message, context);
  }

  error(message: string, context?: unknown): void {
    console.error(message, context);
  }
}

function environmentProduction(): boolean {
  return typeof ngDevMode !== 'undefined' ? !ngDevMode : true;
}

declare const ngDevMode: boolean | undefined;
