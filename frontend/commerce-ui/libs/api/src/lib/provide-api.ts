import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { APP_CONFIG, AppConfig } from '@commerce/core';
import { apiErrorInterceptor } from './api-error.interceptor';
import { credentialsInterceptor } from './credentials.interceptor';

export function provideApi(config: AppConfig): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: APP_CONFIG, useValue: config },
    provideHttpClient(
      withInterceptors([credentialsInterceptor, apiErrorInterceptor]),
      withXsrfConfiguration({ cookieName: 'XSRF-REQUEST-TOKEN', headerName: 'X-XSRF-TOKEN' })
    )
  ]);
}
