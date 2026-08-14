import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ORDER_STATUSES, OrderStatus, OrderSummary, OrdersApi } from '@commerce/api';
import { BreadcrumbsComponent } from '@commerce/layout';
import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState,
  PaginationComponent
} from '@commerce/shared';
import {
  AdminPageShellComponent,
  FilterBarComponent,
  ToastService,
  exportCsv,
  resolveAdminError
} from '@commerce/ui';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    BreadcrumbsComponent,
    AdminPageShellComponent,
    FilterBarComponent,
    CurrencyFormatPipe,
    TranslatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: ('nav.dashboard' | translate), link: '/dashboard' },
      { label: ('nav.orders' | translate) }
    ]" />

    <cmr-admin-page-shell [title]="'nav.orders' | translate">
      <button actions type="button" class="btn btn--secondary" (click)="exportOrders()" [disabled]="!orders.length">
        {{ 'action.export' | translate }}
      </button>

      <div toolbar>
        <cmr-filter-bar [showSearch]="false" (reset)="resetFilters()">
          <label class="filter-field">
            <span>{{ 'orders.status' | translate }}</span>
            <select [(ngModel)]="statusFilter" (ngModelChange)="applyFilters()">
              <option value="">{{ 'common.all' | translate }}</option>
              @for (status of statuses; track status) {
                <option [value]="status">{{ status }}</option>
              }
            </select>
          </label>
          <label class="filter-field">
            <span>{{ 'auth.email' | translate }}</span>
            <input type="search" [(ngModel)]="emailFilter" (ngModelChange)="applyFilters()" />
          </label>
          <label class="filter-field">
            <span>{{ 'orders.number' | translate }}</span>
            <input type="search" [(ngModel)]="orderNumberFilter" (ngModelChange)="applyFilters()" />
          </label>
        </cmr-filter-bar>
      </div>

      @switch (state) {
        @case ('loading') { <cmr-loading-state /> }
        @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
        @case ('empty') { <cmr-empty-state /> }
        @default {
          <div class="table-wrap">
            <table class="admin-table">
              <thead>
                <tr>
                  <th>{{ 'orders.number' | translate }}</th>
                  <th>{{ 'nav.customers' | translate }}</th>
                  <th>{{ 'orders.status' | translate }}</th>
                  <th>{{ 'orders.payment' | translate }}</th>
                  <th>{{ 'orders.fulfillment' | translate }}</th>
                  <th>{{ 'orders.total' | translate }}</th>
                  <th>{{ 'orders.date' | translate }}</th>
                  <th>{{ 'admin.table.actions' | translate }}</th>
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
                    <td><a [routerLink]="['/orders', order.id]">{{ 'action.view' | translate }}</a></td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <cmr-pagination
            [page]="page"
            [totalPages]="totalPages"
            [pageSize]="pageSize"
            [totalItems]="totalCount"
            (pageChange)="setPage($event)"
            (pageSizeChange)="setPageSize($event)" />
        }
      }
    </cmr-admin-page-shell>
  `,
  styles: [`
    .filter-field { display: grid; gap: 0.35rem; min-width: 10rem; }
    .table-wrap { overflow-x: auto; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.75rem; }
    .admin-table { width: 100%; border-collapse: collapse; min-width: 48rem; }
    th, td { padding: 0.75rem 1rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class OrderListPageComponent implements OnInit {
  private readonly ordersApi = inject(OrdersApi);
  private readonly toast = inject(ToastService);

  state: PageState = 'loading';
  errorMessage = '';
  orders: OrderSummary[] = [];
  statuses = ORDER_STATUSES;
  statusFilter = '';
  emailFilter = '';
  orderNumberFilter = '';
  page = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const result = await firstValueFrom(this.ordersApi.listAdmin({
        page: this.page,
        pageSize: this.pageSize,
        status: this.statusFilter ? this.statusFilter as OrderStatus : undefined,
        email: this.emailFilter || undefined,
        orderNumber: this.orderNumberFilter || undefined
      }));
      this.orders = result.items;
      this.totalCount = result.totalCount;
      this.totalPages = Math.max(1, Math.ceil(result.totalCount / this.pageSize));
      this.state = this.orders.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = resolveAdminError(error, 'Failed to load orders.');
      this.state = 'error';
    }
  }

  applyFilters(): void {
    this.page = 1;
    void this.load();
  }

  resetFilters(): void {
    this.statusFilter = '';
    this.emailFilter = '';
    this.orderNumberFilter = '';
    this.applyFilters();
  }

  setPage(page: number): void {
    this.page = page;
    void this.load();
  }

  setPageSize(pageSize: number): void {
    this.pageSize = pageSize;
    this.page = 1;
    void this.load();
  }

  exportOrders(): void {
    exportCsv(
      'orders.csv',
      ['Order', 'Customer', 'Status', 'Payment', 'Fulfillment', 'Total', 'Currency'],
      this.orders.map(order => [
        order.orderNumber,
        order.customerDisplayName || order.customerEmail || '',
        order.status,
        order.paymentStatus,
        order.fulfillmentStatus,
        String(order.grandTotal),
        order.currencyCode
      ])
    );
    this.toast.success('Export completed.');
  }
}
