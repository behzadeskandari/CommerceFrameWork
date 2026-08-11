import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StoreApi, StoreSummary } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [RouterLink, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: ('nav.stores' | translate) }]" />
    <header class="page-header">
      <h1>{{ 'nav.stores' | translate }}</h1>
      @if (permissions.hasPermission('Stores.Create')) {
        <a routerLink="/stores/new" class="btn btn--primary">{{ 'action.create' | translate }}</a>
      }
    </header>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state messageKey="state.empty" /> }
      @default {
        <table>
          <thead><tr><th>Name</th><th>System name</th><th>URL</th><th>Status</th><th></th></tr></thead>
          <tbody>
            @for (store of stores; track store.id) {
              <tr>
                <td>{{ store.name }}</td>
                <td>{{ store.systemName }}</td>
                <td>{{ store.url }}</td>
                <td>{{ store.isActive ? 'Active' : 'Inactive' }}</td>
                <td><a [routerLink]="['/stores', store.id]">{{ 'action.edit' | translate }}</a></td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; text-decoration: none; }
    .btn--primary { background: #2563eb; color: #fff; }
    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 0.5rem; overflow: hidden; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class StoreListPageComponent implements OnInit {
  private readonly storeApi = inject(StoreApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  stores: StoreSummary[] = [];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.stores = await firstValueFrom(this.storeApi.listStores());
      this.state = this.stores.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load stores.';
      this.state = 'error';
    }
  }
}
