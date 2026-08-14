import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, PermissionService } from '@commerce/auth';
import { TranslatePipe, LocalizationService, SupportedLocale } from '@commerce/localization';
import { AdminContextService, ToastContainerComponent } from '@commerce/ui';
import { ADMIN_NAV_GROUPS } from './admin-nav.config';

@Component({
  selector: 'cmr-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, ToastContainerComponent],
  template: `
    <a class="skip-link" href="#admin-main">{{ 'admin.skipToContent' | translate }}</a>
    <div class="admin-shell" [attr.dir]="localization.direction()">
      <header class="admin-header">
        <div class="admin-header__brand">
          <button type="button" class="admin-header__menu" (click)="toggleNav()" [attr.aria-expanded]="navOpen()">
            {{ 'admin.menu' | translate }}
          </button>
          <strong>{{ 'app.title' | translate }} {{ 'admin.titleSuffix' | translate }}</strong>
        </div>
        <div class="header-actions">
          @if (adminContext.stores().length > 1) {
            <label class="header-field">
              <span class="sr-only">{{ 'admin.store' | translate }}</span>
              <select
                [value]="adminContext.storeId()"
                (change)="onStoreChange($event)"
                [attr.aria-label]="'admin.store' | translate">
                @for (store of adminContext.stores(); track store.id) {
                  <option [value]="store.id">{{ store.name }}</option>
                }
              </select>
            </label>
          }
          <label class="header-field">
            <span class="sr-only">{{ 'admin.language' | translate }}</span>
            <select [value]="localization.locale()" (change)="onLocaleChange($event)" [attr.aria-label]="'admin.language' | translate">
              <option value="en">English</option>
              <option value="fa">فارسی</option>
            </select>
          </label>
          @if (auth.isAuthenticated()) {
            <span class="header-user">{{ auth.session()?.email }}</span>
            <button type="button" class="btn btn--ghost" (click)="logout()">{{ 'nav.logout' | translate }}</button>
          }
        </div>
      </header>
      <div class="admin-body">
        <aside class="admin-sidebar" [class.admin-sidebar--open]="navOpen()" aria-label="Admin navigation">
          <nav>
            @for (group of navGroups; track group.labelKey) {
              @if (visibleItems(group).length) {
                <section class="nav-group">
                  <h2 class="nav-group__title">{{ group.labelKey | translate }}</h2>
                  @for (item of visibleItems(group); track item.route) {
                    <a [routerLink]="item.route" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: item.exact ?? false }">
                      {{ item.labelKey | translate }}
                    </a>
                  }
                </section>
              }
            }
          </nav>
        </aside>
        @if (navOpen()) {
          <button type="button" class="admin-overlay" (click)="closeNav()" [attr.aria-label]="'action.close' | translate"></button>
        }
        <main id="admin-main" class="admin-content" tabindex="-1">
          <router-outlet />
        </main>
      </div>
      <cmr-toast-container />
    </div>
  `,
  styles: [`
    .skip-link {
      position: absolute;
      inset-inline-start: 1rem;
      inset-block-start: -3rem;
      background: #111827;
      color: #fff;
      padding: 0.5rem 0.75rem;
      border-radius: 0.375rem;
      z-index: 1001;
      text-decoration: none;
    }
    .skip-link:focus { inset-block-start: 1rem; }
    .admin-shell { min-height: 100vh; background: var(--surface, #f4f6f8); color: var(--text, #1f2937); }
    .admin-header {
      display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap;
      padding: 0.75rem 1.25rem; background: #111827; color: #fff;
    }
    .admin-header__brand { display: flex; align-items: center; gap: 0.75rem; }
    .admin-header__menu {
      display: none;
      border: 1px solid rgba(255,255,255,0.2);
      background: transparent;
      color: inherit;
      border-radius: 0.375rem;
      padding: 0.375rem 0.625rem;
      cursor: pointer;
    }
    .header-actions { display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; }
    .header-field select, .header-user { color: inherit; }
    .header-field select {
      background: rgba(255,255,255,0.08);
      border: 1px solid rgba(255,255,255,0.2);
      border-radius: 0.375rem;
      padding: 0.375rem 0.5rem;
    }
    .btn { cursor: pointer; border-radius: 0.375rem; padding: 0.375rem 0.75rem; border: 1px solid transparent; }
    .btn--ghost { background: transparent; color: inherit; border-color: rgba(255,255,255,0.2); }
    .admin-body { display: grid; grid-template-columns: var(--sidebar-width, 280px) 1fr; min-height: calc(100vh - 56px); }
    .admin-sidebar {
      background: var(--surface-elevated, #fff);
      border-inline-end: 1px solid #e5e7eb;
      padding: 1rem;
      overflow-y: auto;
    }
    .admin-sidebar nav { display: grid; gap: 1rem; }
    .nav-group { display: grid; gap: 0.25rem; }
    .nav-group__title {
      margin: 0;
      font-size: 0.75rem;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      color: var(--text-muted, #6b7280);
    }
    .admin-sidebar a {
      display: block;
      padding: 0.55rem 0.75rem;
      border-radius: 0.375rem;
      color: inherit;
      text-decoration: none;
    }
    .admin-sidebar a.active, .admin-sidebar a:hover, .admin-sidebar a:focus-visible {
      background: #eff6ff;
      color: #1d4ed8;
      outline: none;
    }
    .admin-content { padding: 1.25rem; min-width: 0; }
    .admin-overlay { display: none; }
    .sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0,0,0,0); border: 0; }
    @media (max-width: 960px) {
      .admin-header__menu { display: inline-flex; }
      .admin-body { grid-template-columns: 1fr; }
      .admin-sidebar {
        position: fixed;
        inset-block: 56px 0;
        inset-inline-start: 0;
        width: min(85vw, 320px);
        transform: translateX(calc(-100% * var(--sidebar-hidden, 1)));
        z-index: 20;
        box-shadow: 0 12px 40px rgba(0,0,0,0.18);
      }
      :host-context([dir='rtl']) .admin-sidebar {
        transform: translateX(calc(100% * var(--sidebar-hidden, 1)));
        inset-inline-start: auto;
        inset-inline-end: 0;
      }
      .admin-sidebar--open { --sidebar-hidden: 0; transform: translateX(0); }
      .admin-overlay {
        display: block;
        position: fixed;
        inset: 56px 0 0 0;
        border: none;
        background: rgba(0,0,0,0.35);
        z-index: 10;
      }
    }
  `]
})
export class AdminLayoutComponent {
  readonly auth = inject(AuthService);
  readonly permissions = inject(PermissionService);
  readonly localization = inject(LocalizationService);
  readonly adminContext = inject(AdminContextService);

  readonly navGroups = ADMIN_NAV_GROUPS;
  readonly navOpen = signal(false);

  visibleItems(group: (typeof ADMIN_NAV_GROUPS)[number]) {
    return group.items.filter(item => !item.permission || this.permissions.hasPermission(item.permission));
  }

  onLocaleChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as SupportedLocale;
    this.localization.setLocale(value, true);
  }

  onStoreChange(event: Event): void {
    const value = Number.parseInt((event.target as HTMLSelectElement).value, 10);
    if (Number.isFinite(value)) {
      this.adminContext.selectStore(value);
    }
  }

  toggleNav(): void {
    this.navOpen.update(open => !open);
  }

  closeNav(): void {
    this.navOpen.set(false);
  }

  async logout(): Promise<void> {
    await this.auth.logoutAndRedirect('/login');
  }
}
