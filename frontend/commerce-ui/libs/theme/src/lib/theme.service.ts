import { Injectable, signal } from '@angular/core';

export type ThemeName = 'admin' | 'storefront';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly themeSignal = signal<ThemeName>('storefront');

  readonly theme = this.themeSignal.asReadonly();

  applyTheme(theme: ThemeName): void {
    this.themeSignal.set(theme);
    document.body.dataset['theme'] = theme;
  }
}
