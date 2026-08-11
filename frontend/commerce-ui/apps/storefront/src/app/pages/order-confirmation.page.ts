import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
      <section class="confirmation">
        <h1>{{ 'orders.confirmation.title' | translate }}</h1>
        <p>{{ 'orders.confirmation.message' | translate }}</p>
        <p class="order-number">{{ order.orderNumber }}</p>
        <p>{{ order.createdAtUtc | date: 'medium' }}</p>

        <ul class="items">
          @for (item of order.items; track item.id) {
            <li>
              <span>{{ item.productName }} × {{ item.quantity }}</span>
              <strong>{{ item.lineTotal | currencyFormat: item.currencyCode }}</strong>
            </li>
          }
        </ul>

        <dl class="totals">
          <div class="grand">
            <dt>{{ 'checkout.grandTotal' | translate }}</dt>
            <dd>{{ order.totals.grandTotal | currencyFormat: order.totals.currencyCode }}</dd>
          </div>
        </dl>

        <div class="actions">
          <a routerLink="/">{{ 'nav.home' | translate }}</a>
          <a routerLink="/products">{{ 'cart.continueShopping' | translate }}</a>
        </div>
      </section>
    } @else if (state === 'error') {
      <cmr-error-state [message]="errorMessage" (retry)="load()" />
    }
  `,
  styles: [`
    .confirmation { display: grid; gap: 1rem; max-width: 32rem; }
    .order-number { font-size: 1.25rem; font-weight: 700; }
    .items { list-style: none; padding: 0; margin: 0; display: grid; gap: 0.5rem; }
    .items li { display: flex; justify-content: space-between; gap: 1rem; }
    .totals { margin: 1rem 0; }
    .totals div { display: flex; justify-content: space-between; gap: 1rem; }
    .grand { font-weight: 700; }
    .actions { display: flex; gap: 1rem; flex-wrap: wrap; }
    .actions a { color: var(--primary, #0f766e); }
  `]
})
export class OrderConfirmationPageComponent implements OnInit {
  readonly orderNumber = input.required<string>();
  private readonly ordersApi = inject(OrdersApi);
  private readonly route = inject(ActivatedRoute);

  state: PageState = 'loading';
  errorMessage = '';
  order: OrderDetail | null = null;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    const accessToken = this.route.snapshot.queryParamMap.get('accessToken');
    try {
      this.order = await firstValueFrom(
        this.ordersApi.getByNumber(this.orderNumber(), accessToken ?? undefined)
      );
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load order.';
      this.state = 'error';
    }
  }
}
