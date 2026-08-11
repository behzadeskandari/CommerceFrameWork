import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, PermissionService } from '@commerce/auth';
import { TranslatePipe, LocalizationService, SupportedLocale } from '@commerce/localization';

@Component({
  selector: 'cmr-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  template: `
    <div class="admin-shell">
      <header class="admin-header">
        <strong>{{ 'app.title' | translate }} Admin</strong>
        <div class="header-actions">
          <label>
            <span class="sr-only">Language</span>
            <select [value]="localization.locale()" (change)="onLocaleChange($event)">
              <option value="en">English</option>
              <option value="fa">Persian</option>
            </select>
          </label>
          @if (auth.isAuthenticated()) {
            <span>{{ auth.session()?.email }}</span>
            <button type="button" (click)="logout()">{{ 'nav.logout' | translate }}</button>
          }
        </div>
      </header>
      <div class="admin-body">
        <aside class="admin-sidebar" aria-label="Admin navigation">
          <nav>
            <a routerLink="/dashboard" routerLinkActive="active">{{ 'nav.dashboard' | translate }}</a>
            @if (permissions.hasPermission('Catalog.Products.View')) {
              <a routerLink="/catalog/products" routerLinkActive="active">{{ 'nav.products' | translate }}</a>
            }
            @if (permissions.hasPermission('Catalog.Categories.View')) {
              <a routerLink="/catalog/categories" routerLinkActive="active">{{ 'nav.categories' | translate }}</a>
            }
            @if (permissions.hasPermission('Catalog.Attributes.View')) {
              <a routerLink="/catalog/attributes" routerLinkActive="active">{{ 'nav.attributes' | translate }}</a>
            }
            @if (permissions.hasPermission('Media.View')) {
              <a routerLink="/media" routerLinkActive="active">{{ 'nav.media' | translate }}</a>
            }
            @if (permissions.hasPermission('Customers.View')) {
              <a routerLink="/customers" routerLinkActive="active">{{ 'nav.customers' | translate }}</a>
            }
            @if (permissions.hasPermission('Orders.View')) {
              <a routerLink="/orders" routerLinkActive="active">{{ 'nav.orders' | translate }}</a>
            }
            @if (permissions.hasPermission('Inventory.View')) {
              <a routerLink="/inventory" routerLinkActive="active">{{ 'nav.inventory' | translate }}</a>
            }
            @if (permissions.hasPermission('Stores.View')) {
              <a routerLink="/stores" routerLinkActive="active">{{ 'nav.stores' | translate }}</a>
            }
            @if (permissions.hasPermission('Languages.View')) {
              <a routerLink="/languages" routerLinkActive="active">{{ 'nav.languages' | translate }}</a>
            }
            @if (permissions.hasPermission('Currencies.View')) {
              <a routerLink="/currencies" routerLinkActive="active">{{ 'nav.currencies' | translate }}</a>
            }
            @if (permissions.hasPermission('Settings.View')) {
              <a routerLink="/settings" routerLinkActive="active">{{ 'nav.settings' | translate }}</a>
            }
          </nav>
        </aside>
        <main class="admin-content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .admin-shell { min-height: 100vh; background: var(--surface, #f4f6f8); color: var(--text, #1f2937); }
    .admin-header {
      display: flex; justify-content: space-between; align-items: center;
      padding: 0.75rem 1.25rem; background: #111827; color: #fff;
    }
    .header-actions { display: flex; align-items: center; gap: 0.75rem; }
    .admin-body { display: grid; grid-template-columns: var(--sidebar-width, 260px) 1fr; min-height: calc(100vh - 56px); }
    .admin-sidebar { background: #fff; border-right: 1px solid #e5e7eb; padding: 1rem; }
    .admin-sidebar nav { display: flex; flex-direction: column; gap: 0.25rem; }
    .admin-sidebar a {
      padding: 0.625rem 0.75rem; border-radius: 0.375rem; color: inherit; text-decoration: none;
    }
    .admin-sidebar a.active, .admin-sidebar a:hover { background: #eff6ff; color: #1d4ed8; }
    .admin-content { padding: 1.25rem; }
    button, select { cursor: pointer; }
    .sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); border: 0; }
    @media (max-width: 900px) {
      .admin-body { grid-template-columns: 1fr; }
      .admin-sidebar { border-right: none; border-bottom: 1px solid #e5e7eb; }
      .admin-sidebar nav { flex-direction: row; flex-wrap: wrap; }
    }
  `]
})
export class AdminLayoutComponent {
  readonly auth = inject(AuthService);
  readonly permissions = inject(PermissionService);
  readonly localization = inject(LocalizationService);

  onLocaleChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as SupportedLocale;
    this.localization.setLocale(value);
  }

  async logout(): Promise<void> {
    await this.auth.logoutAndRedirect('/login');
  }
}
