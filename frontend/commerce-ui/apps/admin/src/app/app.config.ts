import { ApplicationConfig, inject, provideAppInitializer } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideApi } from '@commerce/api';
import { AuthService } from '@commerce/auth';
import { environment } from '@commerce/core';
import { LocalizationService } from '@commerce/localization';
import { ThemeService } from '@commerce/theme';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideApi({ apiBaseUrl: environment.apiBaseUrl, appName: 'Commerce Admin' }),
    provideRouter(routes, withComponentInputBinding()),
    provideAppInitializer(() => {
      const auth = inject(AuthService);
      const localization = inject(LocalizationService);
      const theme = inject(ThemeService);
      localization.setLocale('en');
      theme.applyTheme('admin');
      return auth.initialize();
    })
  ]
};
