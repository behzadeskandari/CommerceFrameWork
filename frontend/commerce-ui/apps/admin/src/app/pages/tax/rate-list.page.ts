import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TaxApi, TaxCategorySummary, TaxRateSummary, TaxZoneSummary } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  ConfirmDialogComponent,
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
    ErrorStateComponent,
    ConfirmDialogComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('tax.rates.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'tax.rates.title' | translate }}</h1>
      @if (permissions.hasPermission('Tax.Manage')) {
        <a routerLink="/tax/rates/new" class="btn">{{ 'action.create' | translate }}</a>
      }
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{{ 'tax.rates.category' | translate }}</th>
                <th>{{ 'tax.rates.zone' | translate }}</th>
                <th>{{ 'tax.rates.percentage' | translate }}</th>
                <th>{{ 'tax.rates.taxShipping' | translate }}</th>
                <th>{{ 'tax.rates.priority' | translate }}</th>
                <th>{{ 'tax.active' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (item of items; track item.id) {
                <tr>
                  <td>{{ categoryName(item.taxCategoryId) }}</td>
                  <td>{{ zoneName(item.taxZoneId) }}</td>
                  <td>{{ item.percentage }}%</td>
                  <td>{{ item.taxShipping ? ('tax.yes' | translate) : ('tax.no' | translate) }}</td>
                  <td>{{ item.priority }}</td>
                  <td>{{ item.isActive ? ('tax.active' | translate) : ('tax.inactive' | translate) }}</td>
                  <td class="actions">
                    @if (permissions.hasPermission('Tax.Manage')) {
                      <a [routerLink]="['/tax/rates', item.id]">{{ 'action.edit' | translate }}</a>
                      <button type="button" class="danger" (click)="confirmDelete(item)">{{ 'action.delete' | translate }}</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }

    <cmr-confirm-dialog
      [open]="deleteTarget !== null"
      [title]="'tax.rates.deleteTitle' | translate"
      [message]="'tax.rates.deleteMessage' | translate"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null" />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .btn { padding: 0.5rem 1rem; background: #2563eb; color: #fff; text-decoration: none; border-radius: 0.375rem; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; background: #fff; min-width: 720px; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .actions button, .actions a { font-size: 0.875rem; }
    .actions button { background: none; border: none; color: #2563eb; cursor: pointer; text-decoration: underline; }
    .actions button.danger { color: #dc2626; }
  `]
})
export class RateListPageComponent implements OnInit {
  private readonly taxApi = inject(TaxApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: TaxRateSummary[] = [];
  categories: TaxCategorySummary[] = [];
  zones: TaxZoneSummary[] = [];
  deleteTarget: TaxRateSummary | null = null;

  ngOnInit(): void {
    void this.load();
  }

  categoryName(id: number): string {
    return this.categories.find(c => c.id === id)?.name ?? String(id);
  }

  zoneName(id: number | null): string {
    if (id == null) return '—';
    return this.zones.find(z => z.id === id)?.name ?? String(id);
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      const [items, categories, zones] = await Promise.all([
        firstValueFrom(this.taxApi.listRates()),
        firstValueFrom(this.taxApi.listCategories()),
        firstValueFrom(this.taxApi.listZones())
      ]);
      this.items = items;
      this.categories = categories;
      this.zones = zones;
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load tax rates.';
      this.state = 'error';
    }
  }

  confirmDelete(item: TaxRateSummary): void {
    this.deleteTarget = item;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.taxApi.deleteRate(this.deleteTarget.id));
      this.deleteTarget = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';
      this.state = 'error';
    }
  }
}
