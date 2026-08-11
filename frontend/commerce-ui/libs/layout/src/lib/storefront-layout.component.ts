import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@commerce/auth';
import { CartStateService } from '@commerce/api';
import { TranslatePipe, StoreContextService } from '@commerce/localization';

@Component({
  selector: 'cmr-storefront-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  template: `
    <div class="storefront-shell">
      <header class="storefront-header">
        <a routerLink="/" class="brand">{{ 'app.title' | translate }}</a>
        <nav aria-label="Primary">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">{{ 'nav.home' | translate }}</a>
          <a routerLink="/categories" routerLinkActive="active">{{ 'nav.categories' | translate }}</a>
          <a routerLink="/products" routerLinkActive="active">{{ 'nav.shop' | translate }}</a>
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
        <router-outlet />
      </main>
      <footer class="storefront-footer">
        <small>&copy; {{ 'app.title' | translate }}</small>
      </footer>
    </div>
  `,
  styles: [`
    .storefront-shell { min-height: 100vh; display: flex; flex-direction: column; background: var(--surface, #fff); color: var(--text, #111827); }
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
  `]
})
export class StorefrontLayoutComponent {
  readonly auth = inject(AuthService);
  readonly storeContext = inject(StoreContextService);
  readonly cart = inject(CartStateService);

  async onLocaleChange(event: Event): Promise<void> {
    const value = (event.target as HTMLSelectElement).value;
    await this.storeContext.selectLanguage(value);
  }

  async logout(): Promise<void> {
    await this.auth.logoutAndRedirect('/login');
  }
}
