import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ORDER_STATUSES, OrderStatus, OrderSummary, OrdersApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { CurrencyFormatPipe } from '@commerce/localization';
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
    DatePipe,
    FormsModule,
    RouterLink,
    BreadcrumbsComponent,
    CurrencyFormatPipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: 'Orders' }
    ]" />
    <h1>Orders</h1>

    <div class="filters">
      <label>
        Status
        <select [(ngModel)]="statusFilter" (ngModelChange)="applyFilters()">
          <option value="">All</option>
          @for (status of statuses; track status) {
            <option [value]="status">{{ status }}</option>
          }
        </select>
      </label>
      <label>
        Email
        <input type="search" [(ngModel)]="emailFilter" (ngModelChange)="applyFilters()" />
      </label>
      <label>
        Order number
        <input type="search" [(ngModel)]="orderNumberFilter" (ngModelChange)="applyFilters()" />
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
              <th>Order</th>
              <th>Customer</th>
              <th>Status</th>
              <th>Payment</th>
              <th>Fulfillment</th>
              <th>Total</th>
              <th>Date</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (order of orders; track order.id) {
              <tr>
                <td>{{ order.orderNumber }}</td>
                <td>{{ order.customerDisplayName || order.customerEmail || '—' }}</td>
                <td>{{ order.status }}</td>
                <td>{{ order.paymentStatus }}</td>
                <td>{{ order.fulfillmentStatus }}</td>
                <td>{{ order.grandTotal | currencyFormat: order.currencyCode }}</td>
                <td>{{ order.createdAtUtc | date: 'medium' }}</td>
                <td><a [routerLink]="['/orders', order.id]">View</a></td>
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
export class OrderListPageComponent implements OnInit {
  private readonly ordersApi = inject(OrdersApi);

  readonly statuses = ORDER_STATUSES;
  state: PageState = 'loading';
  errorMessage = '';
  orders: OrderSummary[] = [];
  statusFilter: OrderStatus | '' = '';
  emailFilter = '';
  orderNumberFilter = '';
  page = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  ngOnInit(): void {
    void this.load();
  }

  applyFilters(): void {
    void this.setPage(1);
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const result = await firstValueFrom(this.ordersApi.listAdmin({
        page: this.page,
        pageSize: this.pageSize,
        status: this.statusFilter || undefined,
        email: this.emailFilter.trim() || undefined,
        orderNumber: this.orderNumberFilter.trim() || undefined
      }));
      this.orders = result.items;
      this.totalCount = result.totalCount;
      this.totalPages = Math.max(1, Math.ceil(result.totalCount / this.pageSize));
      this.state = this.orders.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load orders.';
      this.state = 'error';
    }
  }

  setPage(page: number): void {
    this.page = page;
    void this.load();
  }
}
