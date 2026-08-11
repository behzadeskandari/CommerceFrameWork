import { InjectionToken } from '@angular/core';

export interface AppConfig {
  apiBaseUrl: string;
  appName: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');
