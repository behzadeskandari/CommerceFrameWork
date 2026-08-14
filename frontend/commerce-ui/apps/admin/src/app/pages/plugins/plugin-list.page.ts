import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PluginSummary, PluginsApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    BreadcrumbsComponent,
    TranslatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('plugins.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'plugins.title' | translate }}</h1>
      @if (permissions.hasPermission('Plugins.Install')) {
        <label class="upload-btn">
          {{ 'plugins.installPackage' | translate }}
          <input type="file" accept=".zip" (change)="onPackageSelected($event)" hidden />
        </label>
      }
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @case ('success') {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{{ 'plugins.name' | translate }}</th>
                <th>{{ 'plugins.systemName' | translate }}</th>
                <th>{{ 'plugins.version' | translate }}</th>
                <th>{{ 'plugins.status' | translate }}</th>
                <th>{{ 'plugins.installed' | translate }}</th>
                <th>{{ 'plugins.enabled' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (item of items; track item.systemName) {
                <tr>
                  <td>{{ item.name }}</td>
                  <td><code>{{ item.systemName }}</code></td>
                  <td>{{ item.version }}</td>
                  <td><span class="badge" [class]="badgeClass(item)">{{ item.state }}</span></td>
                  <td>{{ item.isInstalled ? ('tax.yes' | translate) : ('tax.no' | translate) }}</td>
                  <td>{{ item.isEnabled ? ('tax.yes' | translate) : ('tax.no' | translate) }}</td>
                  <td class="actions">
                    <a [routerLink]="['/plugins', item.systemName]">{{ 'action.view' | translate }}</a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
    @if (actionError) {
      <p class="action-error" role="alert">{{ actionError }}</p>
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; margin-bottom: 1rem; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.625rem 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .actions { white-space: nowrap; }
    .badge { padding: 0.125rem 0.5rem; border-radius: 999px; font-size: 0.75rem; background: #f3f4f6; }
    .badge.enabled { background: #d1fae5; color: #065f46; }
    .badge.failed { background: #fee2e2; color: #991b1b; }
    .upload-btn {
      display: inline-block; padding: 0.5rem 1rem; border-radius: 0.375rem;
      background: var(--primary, #0f766e); color: #fff; cursor: pointer;
    }
    .action-error { color: #b91c1c; margin-top: 1rem; }
  `]
})
export class PluginListPageComponent implements OnInit {
  private readonly pluginsApi = inject(PluginsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  items: PluginSummary[] = [];
  errorMessage = '';
  actionError = '';

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      this.items = await firstValueFrom(this.pluginsApi.list());
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load plugins.';
      this.state = 'error';
    }
  }

  badgeClass(item: PluginSummary): string {
    if (item.isEnabled) return 'enabled';
    if (item.state === 'Failed' || item.state === 'Invalid') return 'failed';
    return '';
  }

  async onPackageSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.actionError = '';
    try {
      await firstValueFrom(this.pluginsApi.installPackage(file));
      await this.load();
    } catch (error) {
      this.actionError = error instanceof ApiClientError ? error.message : 'Package installation failed.';
    }
  }
}
