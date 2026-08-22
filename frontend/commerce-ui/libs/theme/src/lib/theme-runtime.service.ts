import { Injectable, computed, inject, signal } from '@angular/core';
import { ThemeApi, ThemeLayoutDefinition, ThemeLayoutType, ThemeRuntime } from '@commerce/api';
import { StoreContextService } from '@commerce/localization';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ThemeRuntimeService {
  private readonly api = inject(ThemeApi);
  private readonly storeContext = inject(StoreContextService);
  private readonly runtimeSignal = signal<ThemeRuntime | null>(null);
  private loading: Promise<void> | null = null;

  readonly runtime = this.runtimeSignal.asReadonly();
  readonly direction = computed(() => this.runtimeSignal()?.direction ?? this.storeContext.direction());
  readonly cssVariables = computed(() => this.runtimeSignal()?.cssVariables ?? {});

  async initialize(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    if (this.loading) {
      await this.loading;
      return;
    }

    this.loading = this.loadInternal();
    try {
      await this.loading;
    } finally {
      this.loading = null;
    }
  }

  getLayout(layoutType: ThemeLayoutType | string): ThemeLayoutDefinition | null {
    const runtime = this.runtimeSignal();
    if (!runtime) {
      return null;
    }

    return runtime.layouts.find(layout => layout.layoutType === layoutType) ?? null;
  }

  private async loadInternal(): Promise<void> {
    try {
      const runtime = await firstValueFrom(this.api.getRuntime());
      this.runtimeSignal.set(runtime);
      this.applyRuntime(runtime);
    } catch {
      this.runtimeSignal.set(null);
    }
  }

  private applyRuntime(runtime: ThemeRuntime): void {
    const root = document.documentElement;
    root.setAttribute('dir', runtime.direction);
    root.lang = this.storeContext.currentLanguageCode();

    for (const [key, value] of Object.entries(runtime.cssVariables)) {
      root.style.setProperty(key, value);
    }

    for (const href of runtime.cssAssets) {
      this.ensureStylesheet(href);
    }
  }

  private ensureStylesheet(href: string): void {
    const id = `theme-css-${href.replace(/[^\w-]+/g, '-')}`;
    if (document.getElementById(id)) {
      return;
    }

    const link = document.createElement('link');
    link.id = id;
    link.rel = 'stylesheet';
    link.href = this.toRootRelativeAssetUrl(href);
    document.head.appendChild(link);
  }

  private toRootRelativeAssetUrl(href: string): string {
    if (href.startsWith('/') || /^[a-z][a-z\d+.-]*:/i.test(href)) {
      return href;
    }

    return `/${href}`;
  }
}
