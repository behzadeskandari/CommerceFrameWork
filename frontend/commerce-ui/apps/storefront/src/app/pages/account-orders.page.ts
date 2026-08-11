import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrderSummary, OrdersApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';
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
    RouterLink,
    TranslatePipe,
    CurrencyFormatPipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent
  ],
  template: `
    <h1>{{ 'orders.myOrders' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>{{ 'orders.number' | translate }}</th>
              <th>{{ 'orders.status' | translate }}</th>
              <th>{{ 'orders.total' | translate }}</th>
              <th>{{ 'orders.date' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (order of orders; track order.id) {
              <tr>
                <td>{{ order.orderNumber }}</td>
                <td>{{ order.status }}</td>
                <td>{{ order.grandTotal | currencyFormat: order.currencyCode }}</td>
                <td>{{ order.createdAtUtc | date: 'medium' }}</td>
                <td><a [routerLink]="['/account/orders', order.id]">{{ 'action.view' | translate }}</a></td>
              </tr>
            }
          </tbody>
        </table>
        <cmr-pagination [page]="page" [totalPages]="totalPages" (pageChange)="setPage($event)" />
      }
    }
    <p><a routerLink="/account">{{ 'nav.account' | translate }}</a></p>
  `,
  styles: [`
    table { width: 100%; border-collapse: collapse; margin: 1rem 0; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class AccountOrdersPageComponent implements OnInit {
  private readonly ordersApi = inject(OrdersApi);

  state: PageState = 'loading';
  errorMessage = '';
  orders: OrderSummary[] = [];
  page = 1;
  pageSize = 20;
  totalPages = 1;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const result = await firstValueFrom(this.ordersApi.list({ page: this.page, pageSize: this.pageSize }));
      this.orders = result.items;
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
