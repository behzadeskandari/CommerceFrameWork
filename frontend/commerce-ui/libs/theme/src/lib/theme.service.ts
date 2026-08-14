import { Injectable, inject, signal } from '@angular/core';
import { ThemeRuntimeService } from './theme-runtime.service';
import { storefrontThemeVariables } from './storefront-theme';

export type ThemeShell = 'admin' | 'storefront';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly themeRuntime = inject(ThemeRuntimeService);
  private readonly themeSignal = signal<ThemeShell>('storefront');

  readonly theme = this.themeSignal.asReadonly();

  applyTheme(theme: ThemeShell): void {
    this.themeSignal.set(theme);
    document.body.dataset['theme'] = theme;

    if (theme === 'storefront') {
      const root = document.documentElement;
      for (const [key, value] of Object.entries(storefrontThemeVariables)) {
        if (!root.style.getPropertyValue(key)) {
          root.style.setProperty(key, value);
        }
      }
    }
  }

  async initializeStorefrontTheme(): Promise<void> {
    await this.themeRuntime.initialize();
  }
}
