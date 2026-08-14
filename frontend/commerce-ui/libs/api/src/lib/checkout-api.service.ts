import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG, ApiResponse } from '@commerce/core';
import {
  CheckoutSession,
  CheckoutValidationResult,
  SetBillingAddressRequest,
  SetShippingAddressRequest
} from './models/checkout.models';

@Injectable({ providedIn: 'root' })
export class CheckoutApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly base = `${this.config.apiBaseUrl}/api/checkout`;

  start(): Observable<CheckoutSession> {
    return this.http
      .post<ApiResponse<CheckoutSession>>(this.base, {})
      .pipe(map(response => response.data!));
  }

  get(checkoutId: number): Observable<CheckoutSession> {
    return this.http
      .get<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}`)
      .pipe(map(response => response.data!));
  }

  setGuestContact(checkoutId: number, email: string): Observable<CheckoutSession> {
    return this.http
      .put<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/guest-contact`, { email })
      .pipe(map(response => response.data!));
  }

  setBillingAddress(checkoutId: number, request: SetBillingAddressRequest): Observable<CheckoutSession> {
    return this.http
      .put<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/billing-address`, request)
      .pipe(map(response => response.data!));
  }

  setShippingAddress(checkoutId: number, request: SetShippingAddressRequest): Observable<CheckoutSession> {
    return this.http
      .put<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/shipping-address`, request)
      .pipe(map(response => response.data!));
  }

  selectShippingMethod(checkoutId: number, methodId: string, providerSystemName: string): Observable<CheckoutSession> {
    return this.http
      .put<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/shipping-method`, { methodId, providerSystemName })
      .pipe(map(response => response.data!));
  }

  selectPaymentMethod(checkoutId: number, methodId: string, systemName: string): Observable<CheckoutSession> {
    return this.http
      .put<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/payment-method`, { methodId, systemName })
      .pipe(map(response => response.data!));
  }

  refresh(checkoutId: number): Observable<CheckoutSession> {
    return this.http
      .post<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/refresh`, {})
      .pipe(map(response => response.data!));
  }

  validate(checkoutId: number): Observable<CheckoutValidationResult> {
    return this.http
      .post<ApiResponse<CheckoutValidationResult>>(`${this.base}/${checkoutId}/validate`, {})
      .pipe(map(response => response.data!));
  }

  applyGiftCard(checkoutId: number, code: string): Observable<CheckoutSession> {
    return this.http
      .post<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/gift-cards`, { code })
      .pipe(map(response => response.data!));
  }

  removeGiftCard(checkoutId: number): Observable<CheckoutSession> {
    return this.http
      .delete<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/gift-cards`)
      .pipe(map(response => response.data!));
  }

  applyStoreCredit(checkoutId: number, amount: number): Observable<CheckoutSession> {
    return this.http
      .post<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/store-credit`, { amount })
      .pipe(map(response => response.data!));
  }

  removeStoreCredit(checkoutId: number): Observable<CheckoutSession> {
    return this.http
      .delete<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/store-credit`)
      .pipe(map(response => response.data!));
  }

  applyReferralCode(checkoutId: number, referralCode: string): Observable<CheckoutSession> {
    return this.http
      .post<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/referral-code`, { referralCode })
      .pipe(map(response => response.data!));
  }

  removeReferralCode(checkoutId: number): Observable<CheckoutSession> {
    return this.http
      .delete<ApiResponse<CheckoutSession>>(`${this.base}/${checkoutId}/referral-code`)
      .pipe(map(response => response.data!));
  }
}
