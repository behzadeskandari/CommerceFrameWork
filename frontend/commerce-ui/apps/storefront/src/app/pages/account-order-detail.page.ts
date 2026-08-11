import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrderDetail, OrdersApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    TranslatePipe,
    CurrencyFormatPipe,
    LoadingStateComponent,
    ErrorStateComponent
  ],
  template: `
    @if (state === 'loading') { <cmr-loading-state /> }
    @else if (order) {
      <header class="header">
        <div>
          <h1>{{ order.orderNumber }}</h1>
          <p>{{ order.createdAtUtc | date: 'medium' }}</p>
        </div>
        @if (order.status !== 'Cancelled' && order.status !== 'Completed') {
          <button type="button" (click)="cancel()" [disabled]="cancelling">{{ 'orders.cancelOrder' | translate }}</button>
        }
      </header>

      <section class="status-grid">
        <div><strong>{{ 'orders.status' | translate }}</strong><span>{{ order.status }}</span></div>
        <div><strong>{{ 'orders.payment' | translate }}</strong><span>{{ order.paymentStatus }}</span></div>
        <div><strong>{{ 'orders.fulfillment' | translate }}</strong><span>{{ order.fulfillmentStatus }}</span></div>
      </section>

      <section>
        <h2>{{ 'orders.items' | translate }}</h2>
        <ul class="items">
          @for (item of order.items; track item.id) {
            <li>
              <span>{{ item.productName }} × {{ item.quantity }}</span>
              <strong>{{ item.lineTotal | currencyFormat: item.currencyCode }}</strong>
            </li>
          }
        </ul>
      </section>

      <dl class="totals">
        <div><dt>{{ 'cart.subtotal' | translate }}</dt><dd>{{ order.totals.subtotal | currencyFormat: order.totals.currencyCode }}</dd></div>
        <div><dt>{{ 'checkout.shipping' | translate }}</dt><dd>{{ order.totals.shippingTotal | currencyFormat: order.totals.currencyCode }}</dd></div>
        <div><dt>{{ 'checkout.tax' | translate }}</dt><dd>{{ order.totals.taxTotal | currencyFormat: order.totals.currencyCode }}</dd></div>
        <div class="grand"><dt>{{ 'checkout.grandTotal' | translate }}</dt><dd>{{ order.totals.grandTotal | currencyFormat: order.totals.currencyCode }}</dd></div>
      </dl>

      @if (order.shippingAddress; as address) {
        <section>
          <h2>{{ 'checkout.step.shipping' | translate }}</h2>
          <p>{{ address.firstName }} {{ address.lastName }}</p>
          <p>{{ address.address1 }}, {{ address.city }} {{ address.postalCode }}</p>
          <p>{{ address.country }}</p>
        </section>
      }

      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
      <p><a routerLink="/account/orders">{{ 'orders.myOrders' | translate }}</a></p>
    }
  `,
  styles: [`
    .header { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: 1rem; }
    .status-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }
    .status-grid div { display: grid; gap: 0.25rem; }
    .items { list-style: none; padding: 0; margin: 0; display: grid; gap: 0.5rem; }
    .items li { display: flex; justify-content: space-between; gap: 1rem; }
    .totals { display: grid; gap: 0.375rem; max-width: 24rem; margin: 1rem 0; }
    .totals div { display: flex; justify-content: space-between; gap: 1rem; }
    .grand { font-weight: 700; border-top: 1px solid #e5e7eb; padding-top: 0.5rem; }
    button { padding: 0.625rem 1rem; background: #dc2626; color: #fff; border: none; border-radius: 0.375rem; cursor: pointer; }
    button:disabled { opacity: 0.6; cursor: not-allowed; }
  `]
})
export class AccountOrderDetailPageComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly ordersApi = inject(OrdersApi);

  state: PageState = 'loading';
  errorMessage = '';
  order: OrderDetail | null = null;
  cancelling = false;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.order = await firstValueFrom(this.ordersApi.getById(Number(this.id())));
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load order.';
      this.state = 'error';
    }
  }

  async cancel(): Promise<void> {
    if (!this.order) return;
    this.cancelling = true;
    this.errorMessage = '';
    try {
      this.order = await firstValueFrom(this.ordersApi.cancel(this.order.id, { reason: 'Cancelled by customer' }));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Cancel failed.';
    } finally {
      this.cancelling = false;
    }
  }
}
