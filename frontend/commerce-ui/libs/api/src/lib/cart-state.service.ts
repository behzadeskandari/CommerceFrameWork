import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiClientError } from '@commerce/core';
import { firstValueFrom } from 'rxjs';
import { CartApi } from './cart-api.service';
import { Cart } from './models/cart.models';

const emptyCart = (): Cart => ({
  id: 0,
  storeId: 0,
  currency: '',
  currencyId: 0,
  items: [],
  totals: {
    subtotal: 0,
    discountTotal: 0,
    shippingTotal: 0,
    taxTotal: 0,
    grandTotal: 0,
    currency: ''
  },
  itemCount: 0
});

@Injectable({ providedIn: 'root' })
export class CartStateService {
  private readonly cartApi = inject(CartApi);

  private readonly cartSignal = signal<Cart>(emptyCart());
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly cart = this.cartSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly itemCount = computed(() => this.cartSignal().itemCount);

  async initialize(): Promise<void> {
    await this.refresh();
  }

  async refresh(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const cart = await firstValueFrom(this.cartApi.getCart());
      this.cartSignal.set(cart);
    } catch (error) {
      this.errorSignal.set(error instanceof ApiClientError ? error.message : 'Failed to load cart.');
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async addItem(offerId: number, quantity = 1): Promise<void> {
    await this.mutate(() => this.cartApi.addItem({ offerId, quantity }));
  }

  async updateQuantity(cartItemId: number, quantity: number): Promise<void> {
    await this.mutate(() => this.cartApi.updateItem(cartItemId, { quantity }));
  }

  async removeItem(cartItemId: number): Promise<void> {
    await this.mutate(() => this.cartApi.removeItem(cartItemId));
  }

  async clearCart(): Promise<void> {
    await this.mutate(() => this.cartApi.clearCart());
  }

  async mergeAfterLogin(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const result = await firstValueFrom(this.cartApi.mergeGuestCart());
      this.cartSignal.set(result.cart);
    } catch (error) {
      this.errorSignal.set(error instanceof ApiClientError ? error.message : 'Failed to merge cart.');
      await this.refresh();
    } finally {
      this.loadingSignal.set(false);
    }
  }

  private async mutate(request: () => import('rxjs').Observable<Cart>): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const cart = await firstValueFrom(request());
      this.cartSignal.set(cart);
    } catch (error) {
      this.errorSignal.set(error instanceof ApiClientError ? error.message : 'Cart update failed.');
      await this.refresh();
      throw error;
    } finally {
      this.loadingSignal.set(false);
    }
  }
}
