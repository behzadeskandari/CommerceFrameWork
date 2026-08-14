import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '@commerce/auth';
import { CartStateService, CmsApi, StorefrontMenuItem } from '@commerce/api';
import { TranslatePipe, StoreContextService } from '@commerce/localization';
import { WidgetZoneComponent, ThemeRuntimeService } from '@commerce/theme';
import { StorefrontRouterOutletComponent } from './storefront-router-outlet.component';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'cmr-storefront-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe, WidgetZoneComponent, StorefrontRouterOutletComponent],
  template: `
    <div class="storefront-shell" [attr.dir]="themeRuntime.direction()">
      <cmr-widget-zone zone="header" />
      <header class="storefront-header">
        <a routerLink="/" class="brand">{{ 'app.title' | translate }}</a>
        <nav aria-label="Primary">
          @if (menuItems.length) {
            @for (item of menuItems; track item.id) {
              @if (isInternalUrl(item.url)) {
                <a [routerLink]="item.url"
                   routerLinkActive="active"
                   [routerLinkActiveOptions]="{ exact: item.url === '/' }"
                   [attr.target]="item.openInNewTab ? '_blank' : null"
                   [attr.rel]="item.openInNewTab ? 'noopener noreferrer' : null">{{ item.title }}</a>
              } @else {
                <a [href]="item.url"
                   [attr.target]="item.openInNewTab ? '_blank' : null"
                   [attr.rel]="item.openInNewTab ? 'noopener noreferrer' : null">{{ item.title }}</a>
              }
            }
          } @else {
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">{{ 'nav.home' | translate }}</a>
            <a routerLink="/categories" routerLinkActive="active">{{ 'nav.categories' | translate }}</a>
            <a routerLink="/products" routerLinkActive="active">{{ 'nav.shop' | translate }}</a>
          }
        </nav>
        <div class="header-actions">
          <a routerLink="/cart" class="cart-link" [attr.aria-label]="'nav.cart' | translate">
            <span aria-hidden="true">🛒</span>
            <span class="cart-count">{{ cart.itemCount() }}</span>
          </a>
          <label>
            <span class="sr-only">Language</span>
            <select [value]="storeContext.currentLanguageCode()" (change)="onLocaleChange($event)">
              <option value="en">English</option>
              <option value="fa">فارسی</option>
            </select>
          </label>
          @if (auth.isAuthenticated()) {
            <a routerLink="/account">{{ 'nav.account' | translate }}</a>
            <button type="button" (click)="logout()">{{ 'nav.logout' | translate }}</button>
          } @else {
            <a routerLink="/login">{{ 'nav.login' | translate }}</a>
          }
        </div>
      </header>
      <main class="storefront-content">
        <cmr-storefront-router-outlet />
      </main>
      <cmr-widget-zone zone="footer" />
      <footer class="storefront-footer">
        <small>&copy; {{ 'app.title' | translate }}</small>
      </footer>
    </div>
  `,
  styles: [`
    .storefront-shell {
      min-height: 100vh; display: flex; flex-direction: column;
      background: var(--surface, #fff); color: var(--text, #111827);
      font-family: var(--font-family, system-ui, sans-serif);
    }
    .storefront-header {
      display: flex; flex-wrap: wrap; align-items: center; gap: 1rem; justify-content: space-between;
      padding: 0.75rem 1rem; border-bottom: 1px solid #e5e7eb; min-height: var(--header-height, 64px);
    }
    .brand { font-weight: 700; text-decoration: none; color: var(--primary, #0f766e); }
    nav { display: flex; gap: 0.75rem; flex-wrap: wrap; }
    nav a, .header-actions a { text-decoration: none; color: inherit; padding: 0.25rem 0.5rem; border-radius: 0.25rem; }
    nav a.active { color: var(--primary, #0f766e); font-weight: 600; }
    .header-actions { display: flex; align-items: center; gap: 0.75rem; }
    .cart-link { position: relative; display: inline-flex; align-items: center; gap: 0.25rem; }
    .cart-count {
      min-width: 1.25rem; height: 1.25rem; padding: 0 0.25rem; border-radius: 999px;
      background: var(--primary, #0f766e); color: #fff; font-size: 0.75rem; display: inline-flex;
      align-items: center; justify-content: center;
    }
    .storefront-content { flex: 1; padding: 1rem; width: min(1200px, 100%); margin: 0 auto; }
    .storefront-footer { padding: 1rem; border-top: 1px solid #e5e7eb; text-align: center; color: var(--text-muted, #6b7280); }
    .sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); border: 0; }
    :host-context([dir='rtl']) .storefront-header { direction: rtl; }
    :host-context([dir='rtl']) nav { justify-content: flex-start; }
  `]
})
export class StorefrontLayoutComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly storeContext = inject(StoreContextService);
  readonly cart = inject(CartStateService);
  readonly themeRuntime = inject(ThemeRuntimeService);
  private readonly cms = inject(CmsApi);
  menuItems: StorefrontMenuItem[] = [];

  ngOnInit(): void {
    void this.loadMenu();
  }

  private async loadMenu(): Promise<void> {
    try {
      const menu = await firstValueFrom(this.cms.getStorefrontMenu('main-menu'));
      this.menuItems = menu.items ?? [];
    } catch {
      this.menuItems = [];
    }
  }

  async onLocaleChange(event: Event): Promise<void> {
    const value = (event.target as HTMLSelectElement).value;
    await this.storeContext.selectLanguage(value);
    await this.themeRuntime.reload();
    await this.loadMenu();
  }

  isInternalUrl(url: string): boolean {
    return url.startsWith('/');
  }

  async logout(): Promise<void> {
    await this.auth.logoutAndRedirect('/login');
  }
}
