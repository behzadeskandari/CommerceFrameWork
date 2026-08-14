import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideApi } from '@commerce/api';
import { AuthService } from '@commerce/auth';
import { environment } from '@commerce/core';
import { LocalizationService } from '@commerce/localization';
import { ThemeService } from '@commerce/theme';
import { AdminContextService } from '@commerce/ui';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideApi({ apiBaseUrl: environment.apiBaseUrl, appName: 'Commerce Admin' }),
    provideRouter(routes, withComponentInputBinding()),
    provideAppInitializer(async () => {
      const auth = inject(AuthService);
      const localization = inject(LocalizationService);
      const theme = inject(ThemeService);
      const adminContext = inject(AdminContextService);
      theme.applyTheme('admin');
      await Promise.all([auth.initialize(), adminContext.initialize()]);
    })
  ]
};
