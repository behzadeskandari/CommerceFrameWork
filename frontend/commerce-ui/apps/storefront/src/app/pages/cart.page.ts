import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { CartStateService } from '@commerce/api';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    TranslatePipe,
    CurrencyFormatPipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  template: `
    <section class="cart-page" aria-labelledby="cart-title">
      <header class="cart-header">
        <h1 id="cart-title">{{ 'cart.title' | translate }}</h1>
        @if (cart.itemCount() > 0) {
          <button type="button" class="link-btn" (click)="clear()" [disabled]="busy">
            {{ 'cart.clear' | translate }}
          </button>
        }
      </header>

      @if (state === 'loading') { <cmr-loading-state /> }
      @else if (cart.error()) { <cmr-error-state [message]="cart.error()!" (retry)="load()" /> }
      @else if (cart.cart().items.length === 0) {
        <cmr-empty-state messageKey="cart.empty" />
        <a routerLink="/products" class="primary-btn">{{ 'cart.continueShopping' | translate }}</a>
      } @else {
        <ul class="cart-items" role="list">
          @for (item of cart.cart().items; track item.id) {
            <li class="cart-item" [class.invalid]="!item.isValid">
              @if (item.primaryImage) {
                <img
                  class="thumb"
                  [src]="item.primaryImage.thumbnailUrl || item.primaryImage.url"
                  [alt]="item.primaryImage.altText || item.productName" />
              }
              <div class="details">
                <h2>{{ item.productName }}</h2>
                @if (item.variantName) { <p class="variant">{{ item.variantName }}</p> }
                <p class="sku">SKU: {{ item.sku }}</p>
                @if (!item.isValid) {
                  <ul class="validation" aria-live="polite">
                    @for (message of item.validationMessages; track message) {
                      <li>{{ message }}</li>
                    }
                  </ul>
                }
                <div class="qty-row">
                  <label [attr.for]="'qty-' + item.id">{{ 'cart.quantity' | translate }}</label>
                  <div class="qty-controls">
                    <button type="button" [attr.aria-label]="'Decrease quantity'" (click)="decrease(item.id, item.quantity)" [disabled]="busy">−</button>
                    <input
                      [id]="'qty-' + item.id"
                      type="number"
                      min="1"
                      [value]="item.quantity"
                      (change)="onQuantityInput(item.id, $event)"
                      [disabled]="busy || !item.isValid" />
                    <button type="button" [attr.aria-label]="'Increase quantity'" (click)="increase(item.id, item.quantity)" [disabled]="busy || !item.isValid">+</button>
                  </div>
                </div>
              </div>
              <div class="pricing">
                <p>{{ item.unitPrice | currencyFormat: item.currency }}</p>
                <p class="line">{{ item.lineSubtotal | currencyFormat: item.currency }}</p>
                <button type="button" class="remove" (click)="remove(item.id)" [disabled]="busy">
                  {{ 'cart.remove' | translate }}
                </button>
              </div>
            </li>
          }
        </ul>

        <aside class="cart-summary" aria-labelledby="summary-title">
          <h2 id="summary-title">{{ 'cart.summary' | translate }}</h2>
          <p class="summary-row">
            <span>{{ 'cart.subtotal' | translate }}</span>
            <strong>{{ cart.cart().totals.subtotal | currencyFormat: cart.cart().currency }}</strong>
          </p>
          <a routerLink="/checkout" class="checkout-btn">
            {{ 'cart.checkout' | translate }}
          </a>
          <a routerLink="/products" class="secondary-btn">{{ 'cart.continueShopping' | translate }}</a>
        </aside>
      }
    </section>
  `,
  styles: [`
    .cart-page { display: grid; gap: 1.5rem; }
    .cart-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .link-btn { background: none; border: none; color: var(--primary, #0f766e); cursor: pointer; text-decoration: underline; }
    .cart-items { list-style: none; padding: 0; margin: 0; display: grid; gap: 1rem; }
    .cart-item {
      display: grid; gap: 1rem; grid-template-columns: 80px 1fr auto;
      border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1rem; background: #fff;
    }
    .cart-item.invalid { border-color: #fca5a5; background: #fff7f7; }
    .thumb { width: 80px; height: 80px; object-fit: cover; border-radius: 0.375rem; }
    .details h2 { margin: 0 0 0.25rem; font-size: 1rem; }
    .variant, .sku { margin: 0; color: #6b7280; font-size: 0.875rem; }
    .validation { color: #b91c1c; margin: 0.5rem 0 0; padding-inline-start: 1rem; }
    .qty-row { margin-top: 0.75rem; display: grid; gap: 0.375rem; }
    .qty-controls { display: inline-flex; align-items: center; gap: 0.25rem; }
    .qty-controls button {
      width: 2rem; height: 2rem; border: 1px solid #d1d5db; background: #fff; border-radius: 0.375rem; cursor: pointer;
    }
    .qty-controls input { width: 3.5rem; text-align: center; padding: 0.375rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .pricing { text-align: end; display: grid; gap: 0.25rem; align-content: start; }
    .line { font-weight: 600; }
    .remove { background: none; border: none; color: #b91c1c; cursor: pointer; justify-self: end; }
    .cart-summary {
      border: 1px solid #e5e7eb; border-radius: 0.75rem; padding: 1rem; background: #f9fafb;
      max-width: 24rem; justify-self: end; width: 100%;
    }
    .summary-row { display: flex; justify-content: space-between; gap: 1rem; }
    .primary-btn, .checkout-btn {
      display: inline-block; text-align: center; padding: 0.75rem 1rem; border-radius: 0.375rem; text-decoration: none;
    }
    .primary-btn, .checkout-btn { background: var(--primary, #0f766e); color: #fff; border: none; }
    .secondary-btn { border: 1px solid #d1d5db; color: inherit; margin-top: 0.5rem; }
    @media (max-width: 768px) {
      .cart-item { grid-template-columns: 64px 1fr; }
      .pricing { grid-column: 1 / -1; text-align: start; display: flex; flex-wrap: wrap; align-items: center; gap: 0.75rem; }
    }
  `]
})
export class CartPageComponent implements OnInit {
  readonly cart = inject(CartStateService);
  state: PageState = 'loading';
  busy = false;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    await this.cart.refresh();
    this.state = 'success';
  }

  async clear(): Promise<void> {
    this.busy = true;
    try { await this.cart.clearCart(); } finally { this.busy = false; }
  }

  async remove(cartItemId: number): Promise<void> {
    this.busy = true;
    try { await this.cart.removeItem(cartItemId); } finally { this.busy = false; }
  }

  async increase(cartItemId: number, current: number): Promise<void> {
    this.busy = true;
    try { await this.cart.updateQuantity(cartItemId, current + 1); } finally { this.busy = false; }
  }

  async decrease(cartItemId: number, current: number): Promise<void> {
    if (current <= 1) return;
    this.busy = true;
    try { await this.cart.updateQuantity(cartItemId, current - 1); } finally { this.busy = false; }
  }

  async onQuantityInput(cartItemId: number, event: Event): Promise<void> {
    const value = Number((event.target as HTMLInputElement).value);
    if (!Number.isFinite(value) || value <= 0) {
      await this.cart.refresh();
      return;
    }
    this.busy = true;
    try { await this.cart.updateQuantity(cartItemId, value); } finally { this.busy = false; }
  }
}
