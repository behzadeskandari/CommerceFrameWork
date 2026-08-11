import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideApi, CartStateService } from '@commerce/api';
import { AuthService } from '@commerce/auth';
import { environment } from '@commerce/core';
import { StoreContextService } from '@commerce/localization';
import { ThemeService } from '@commerce/theme';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideApi({ apiBaseUrl: environment.apiBaseUrl, appName: 'Commerce Store' }),
    provideRouter(routes, withComponentInputBinding()),
    provideAppInitializer(() => {
      const auth = inject(AuthService);
      const storeContext = inject(StoreContextService);
      const theme = inject(ThemeService);
      theme.applyTheme('storefront');
      return Promise.all([
        auth.initialize(),
        storeContext.initialize(),
        inject(CartStateService).initialize()
      ]);
    })
  ]
};
