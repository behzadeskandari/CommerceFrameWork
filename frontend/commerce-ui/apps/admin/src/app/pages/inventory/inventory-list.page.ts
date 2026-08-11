import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InventoryApi, InventoryAvailabilityStatus, InventoryItemSummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState,
  PaginationComponent
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    BreadcrumbsComponent,
    TranslatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('nav.inventory' | translate) }
    ]" />
    <h1>{{ 'inventory.title' | translate }}</h1>

    <div class="filters">
      <label>
        {{ 'inventory.offerId' | translate }}
        <input type="number" [(ngModel)]="offerIdFilter" (ngModelChange)="applyFilters()" />
      </label>
      <label>
        {{ 'inventory.status' | translate }}
        <select [(ngModel)]="statusFilter" (ngModelChange)="applyFilters()">
          <option value="">{{ 'inventory.allStatuses' | translate }}</option>
          <option value="InStock">{{ 'inventory.inStock' | translate }}</option>
          <option value="OutOfStock">{{ 'inventory.outOfStock' | translate }}</option>
          <option value="Backorder">{{ 'inventory.backorder' | translate }}</option>
          <option value="NotTracked">{{ 'inventory.notTracked' | translate }}</option>
        </select>
      </label>
    </div>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>{{ 'inventory.offerId' | translate }}</th>
              <th>{{ 'inventory.onHand' | translate }}</th>
              <th>{{ 'inventory.reserved' | translate }}</th>
              <th>{{ 'inventory.available' | translate }}</th>
              <th>{{ 'inventory.status' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.offerId }}</td>
                <td>{{ item.onHand }}</td>
                <td>{{ item.reserved }}</td>
                <td>{{ item.available }}</td>
                <td>{{ availabilityLabel(item.availabilityStatus) | translate }}</td>
                <td><a [routerLink]="['/inventory', item.id]">{{ 'action.view' | translate }}</a></td>
              </tr>
            }
          </tbody>
        </table>
        <cmr-pagination [page]="page" [totalPages]="totalPages" (pageChange)="setPage($event)" />
      }
    }
  `,
  styles: [`
    .filters { display: flex; flex-wrap: wrap; gap: 1rem; margin: 1rem 0; }
    .filters label { display: grid; gap: 0.375rem; min-width: 10rem; }
    input, select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class InventoryListPageComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryApi);

  state: PageState = 'loading';
  errorMessage = '';
  items: InventoryItemSummary[] = [];
  offerIdFilter: number | null = null;
  statusFilter: InventoryAvailabilityStatus | '' = '';
  page = 1;
  pageSize = 20;
  totalPages = 1;

  ngOnInit(): void {
    void this.load();
  }

  availabilityLabel(status: InventoryAvailabilityStatus): string {
    switch (status) {
      case 'InStock': return 'inventory.inStock';
      case 'OutOfStock': return 'inventory.outOfStock';
      case 'Backorder': return 'inventory.backorder';
      default: return 'inventory.notTracked';
    }
  }

  applyFilters(): void {
    this.page = 1;
    void this.load();
  }

  setPage(page: number): void {
    this.page = page;
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      const result = await firstValueFrom(this.inventoryApi.list({
        page: this.page,
        pageSize: this.pageSize,
        offerId: this.offerIdFilter ?? undefined,
        availabilityStatus: this.statusFilter || undefined
      }));
      this.items = result.items;
      this.totalPages = Math.max(1, Math.ceil(result.totalCount / result.pageSize));
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load inventory.';
      this.state = 'error';
    }
  }
}
