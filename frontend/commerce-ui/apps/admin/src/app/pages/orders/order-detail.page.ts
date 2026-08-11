import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input } from '@angular/core';
import { OrderDetail, OrdersApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { CurrencyFormatPipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    BreadcrumbsComponent,
    CurrencyFormatPipe,
    LoadingStateComponent,
    ErrorStateComponent
  ],
  template: `
    @if (state === 'loading') { <cmr-loading-state /> }
    @else if (order) {
      <cmr-breadcrumbs [items]="[
        { label: 'Dashboard', link: '/dashboard' },
        { label: 'Orders', link: '/orders' },
        { label: order.orderNumber }
      ]" />
      <header class="header">
        <div>
          <h1>{{ order.orderNumber }}</h1>
          <p>{{ order.createdAtUtc | date: 'medium' }}</p>
        </div>
        @if (canCancel && order.status !== 'Cancelled' && order.status !== 'Completed') {
          <button type="button" (click)="cancel()" [disabled]="cancelling">Cancel order</button>
        }
      </header>

      <section class="status-grid">
        <div><strong>Status</strong><span>{{ order.status }}</span></div>
        <div><strong>Payment</strong><span>{{ order.paymentStatus }}</span></div>
        <div><strong>Fulfillment</strong><span>{{ order.fulfillmentStatus }}</span></div>
      </section>

      <section>
        <h2>Customer</h2>
        <p>{{ order.customer.displayName || order.customer.email || 'Guest' }}</p>
        @if (order.customer.email) { <p>{{ order.customer.email }}</p> }
      </section>

      <section>
        <h2>Items</h2>
        <table>
          <thead>
            <tr><th>Product</th><th>SKU</th><th>Qty</th><th>Unit</th><th>Total</th></tr>
          </thead>
          <tbody>
            @for (item of order.items; track item.id) {
              <tr>
                <td>
                  {{ item.productName }}
                  @if (item.variantName) { <small> — {{ item.variantName }}</small> }
                </td>
                <td>{{ item.sku }}</td>
                <td>{{ item.quantity }}</td>
                <td>{{ item.unitPrice | currencyFormat: item.currencyCode }}</td>
                <td>{{ item.lineTotal | currencyFormat: item.currencyCode }}</td>
              </tr>
            }
          </tbody>
        </table>
      </section>

      <section class="totals">
        <h2>Totals</h2>
        <dl>
          <div><dt>Subtotal</dt><dd>{{ order.totals.subtotal | currencyFormat: order.totals.currencyCode }}</dd></div>
          <div><dt>Discount</dt><dd>{{ order.totals.discountTotal | currencyFormat: order.totals.currencyCode }}</dd></div>
          <div><dt>Shipping</dt><dd>{{ order.totals.shippingTotal | currencyFormat: order.totals.currencyCode }}</dd></div>
          <div><dt>Tax</dt><dd>{{ order.totals.taxTotal | currencyFormat: order.totals.currencyCode }}</dd></div>
          <div class="grand"><dt>Grand total</dt><dd>{{ order.totals.grandTotal | currencyFormat: order.totals.currencyCode }}</dd></div>
        </dl>
      </section>

      <div class="addresses">
        <section>
          <h2>Billing address</h2>
          @if (order.billingAddress; as address) {
            <p>{{ address.firstName }} {{ address.lastName }}</p>
            <p>{{ address.address1 }}</p>
            @if (address.address2) { <p>{{ address.address2 }}</p> }
            <p>{{ address.city }}, {{ address.stateProvince }} {{ address.postalCode }}</p>
            <p>{{ address.country }}</p>
          } @else { <p>—</p> }
        </section>
        @if (order.requiresShipping) {
          <section>
            <h2>Shipping address</h2>
            @if (order.shippingAddress; as address) {
              <p>{{ address.firstName }} {{ address.lastName }}</p>
              <p>{{ address.address1 }}</p>
              @if (address.address2) { <p>{{ address.address2 }}</p> }
              <p>{{ address.city }}, {{ address.stateProvince }} {{ address.postalCode }}</p>
              <p>{{ address.country }}</p>
            } @else { <p>—</p> }
          </section>
        }
      </div>

      @if (order.statusHistory.length) {
        <section>
          <h2>Status history</h2>
          <ul>
            @for (entry of order.statusHistory; track entry.id) {
              <li>{{ entry.createdAtUtc | date: 'medium' }} — {{ entry.historyType }}: {{ entry.fromStatus || '—' }} → {{ entry.toStatus }} ({{ entry.reason }})</li>
            }
          </ul>
        </section>
      }

      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `,
  styles: [`
    .header { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: 1rem; }
    .status-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }
    .status-grid div { display: grid; gap: 0.25rem; padding: 0.75rem; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.375rem; }
    section { margin-bottom: 1.5rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .totals dl { display: grid; gap: 0.375rem; max-width: 20rem; }
    .totals div { display: flex; justify-content: space-between; gap: 1rem; }
    .grand { font-weight: 700; border-top: 1px solid #e5e7eb; padding-top: 0.5rem; }
    .addresses { display: grid; grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr)); gap: 1rem; }
    button { padding: 0.625rem 1rem; background: #dc2626; color: #fff; border: none; border-radius: 0.375rem; cursor: pointer; }
    button:disabled { opacity: 0.6; cursor: not-allowed; }
    ul { padding-inline-start: 1.25rem; }
  `]
})
export class OrderDetailPageComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly ordersApi = inject(OrdersApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  order: OrderDetail | null = null;
  cancelling = false;

  get canCancel(): boolean {
    return this.permissions.hasPermission('Orders.Cancel');
  }

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.order = await firstValueFrom(this.ordersApi.getByIdAdmin(Number(this.id())));
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load order.';
      this.state = 'error';
    }
  }

  async cancel(): Promise<void> {
    if (!this.order || !this.canCancel) return;
    this.cancelling = true;
    this.errorMessage = '';
    try {
      this.order = await firstValueFrom(this.ordersApi.cancelAdmin(this.order.id, { reason: 'Cancelled by admin' }));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Cancel failed.';
    } finally {
      this.cancelling = false;
    }
  }
}
