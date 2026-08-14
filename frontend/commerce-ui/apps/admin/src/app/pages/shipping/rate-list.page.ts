import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShippingApi, ShippingMethodSummary, ShippingRateSummary, ShippingZoneSummary } from '@commerce/api';
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
      { label: ('shipping.rates.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'shipping.rates.title' | translate }}</h1>
      @if (permissions.hasPermission('Shipping.Manage')) {
        <a routerLink="/shipping/rates/new" class="btn">{{ 'action.create' | translate }}</a>
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
                <th>{{ 'shipping.rates.method' | translate }}</th>
                <th>{{ 'shipping.rates.zone' | translate }}</th>
                <th>{{ 'shipping.rates.currency' | translate }}</th>
                <th>{{ 'shipping.rates.rateType' | translate }}</th>
                <th>{{ 'shipping.rates.basePrice' | translate }}</th>
                <th>{{ 'shipping.active' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (item of items; track item.id) {
                <tr>
                  <td>{{ methodName(item.shippingMethodId) }}</td>
                  <td>{{ zoneName(item.shippingZoneId) }}</td>
                  <td>{{ item.currencyCode }}</td>
                  <td>{{ rateTypeLabel(item.rateType) | translate }}</td>
                  <td>{{ item.basePrice }}</td>
                  <td>{{ item.isActive ? ('shipping.active' | translate) : ('shipping.inactive' | translate) }}</td>
                  <td class="actions">
                    @if (permissions.hasPermission('Shipping.Manage')) {
                      <a [routerLink]="['/shipping/rates', item.id]">{{ 'action.edit' | translate }}</a>
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
      [title]="'shipping.rates.deleteTitle' | translate"
      [message]="'shipping.rates.deleteMessage' | translate"
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
  private readonly shippingApi = inject(ShippingApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: ShippingRateSummary[] = [];
  methods: ShippingMethodSummary[] = [];
  zones: ShippingZoneSummary[] = [];
  deleteTarget: ShippingRateSummary | null = null;

  ngOnInit(): void {
    void this.load();
  }

  methodName(id: number): string {
    return this.methods.find(m => m.id === id)?.name ?? String(id);
  }

  zoneName(id: number | null): string {
    if (id == null) return '—';
    return this.zones.find(z => z.id === id)?.name ?? String(id);
  }

  rateTypeLabel(type: ShippingRateSummary['rateType']): string {
    const map: Record<ShippingRateSummary['rateType'], string> = {
      Flat: 'shipping.rates.typeFlat',
      WeightBased: 'shipping.rates.typeWeightBased',
      OrderSubtotalBased: 'shipping.rates.typeOrderSubtotalBased',
      QuantityBased: 'shipping.rates.typeQuantityBased'
    };
    return map[type];
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      const [items, methods, zones] = await Promise.all([
        firstValueFrom(this.shippingApi.listRates()),
        firstValueFrom(this.shippingApi.listMethods()),
        firstValueFrom(this.shippingApi.listZones())
      ]);
      this.items = items;
      this.methods = methods;
      this.zones = zones;
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load shipping rates.';
      this.state = 'error';
    }
  }

  confirmDelete(item: ShippingRateSummary): void {
    this.deleteTarget = item;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.shippingApi.deleteRate(this.deleteTarget.id));
      this.deleteTarget = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';
      this.state = 'error';
    }
  }
}
