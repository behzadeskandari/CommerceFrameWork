import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG, ApiResponse } from '@commerce/core';
import {
  AddCartItemRequest,
  ApplyCartCouponRequest,
  Cart,
  CartMergeResult,
  UpdateCartItemQuantityRequest
} from './models/cart.models';

@Injectable({ providedIn: 'root' })
export class CartApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly base = `${this.config.apiBaseUrl}/api/cart`;

  getCart(): Observable<Cart> {
    return this.http
      .get<ApiResponse<Cart>>(this.base)
      .pipe(map(response => response.data!));
  }

  addItem(request: AddCartItemRequest): Observable<Cart> {
    return this.http
      .post<ApiResponse<Cart>>(`${this.base}/items`, request)
      .pipe(map(response => response.data!));
  }

  updateItem(cartItemId: number, request: UpdateCartItemQuantityRequest): Observable<Cart> {
    return this.http
      .put<ApiResponse<Cart>>(`${this.base}/items/${cartItemId}`, request)
      .pipe(map(response => response.data!));
  }

  removeItem(cartItemId: number): Observable<Cart> {
    return this.http
      .delete<ApiResponse<Cart>>(`${this.base}/items/${cartItemId}`)
      .pipe(map(response => response.data!));
  }

  clearCart(): Observable<Cart> {
    return this.http
      .delete<ApiResponse<Cart>>(this.base)
      .pipe(map(response => response.data!));
  }

  mergeGuestCart(): Observable<CartMergeResult> {
    return this.http
      .post<ApiResponse<CartMergeResult>>(`${this.base}/merge`, {})
      .pipe(map(response => response.data!));
  }

  applyCoupon(request: ApplyCartCouponRequest): Observable<Cart> {
    return this.http
      .post<ApiResponse<Cart>>(`${this.base}/coupons`, request)
      .pipe(map(response => response.data!));
  }

  removeCoupon(code: string): Observable<Cart> {
    return this.http
      .delete<ApiResponse<Cart>>(`${this.base}/coupons/${encodeURIComponent(code)}`)
      .pipe(map(response => response.data!));
  }
}
