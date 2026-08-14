import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG, ApiResponse } from '@commerce/core';
import {
  AssignCustomerGroupRequest,
  CreateCustomerSegmentRequest,
  CreateLoyaltyRewardRequest,
  CreditStoreCreditRequest,
  CustomerAccountOverview,
  CustomerActivity,
  CustomerPreference,
  CustomerPurchaseHistoryItem,
  CustomerSegmentSummary,
  LoyaltyAccount,
  LoyaltyReward,
  LoyaltyTransaction,
  RedeemLoyaltyRewardRequest,
  StoreCreditAccount,
  UpsertCustomerPreferenceRequest
} from './models/customer-account.models';

@Injectable({ providedIn: 'root' })
export class CustomerAccountApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly meBase = `${this.config.apiBaseUrl}/api/customers/me/account`;
  private readonly adminCustomerBase = `${this.config.apiBaseUrl}/api/admin/customers`;
  private readonly adminSegmentsBase = `${this.config.apiBaseUrl}/api/admin/customer-segments`;
  private readonly adminRewardsBase = `${this.config.apiBaseUrl}/api/admin/loyalty-rewards`;

  getOverview(): Observable<CustomerAccountOverview> {
    return this.http.get<ApiResponse<CustomerAccountOverview>>(`${this.meBase}/overview`)
      .pipe(map(r => r.data!));
  }

  listPreferences(): Observable<CustomerPreference[]> {
    return this.http.get<ApiResponse<CustomerPreference[]>>(`${this.meBase}/preferences`)
      .pipe(map(r => r.data ?? []));
  }

  upsertPreference(request: UpsertCustomerPreferenceRequest): Observable<CustomerPreference> {
    return this.http.put<ApiResponse<CustomerPreference>>(`${this.meBase}/preferences`, request)
      .pipe(map(r => r.data!));
  }

  getLoyalty(): Observable<LoyaltyAccount> {
    return this.http.get<ApiResponse<LoyaltyAccount>>(`${this.meBase}/loyalty`)
      .pipe(map(r => r.data!));
  }

  listLoyaltyTransactions(): Observable<LoyaltyTransaction[]> {
    return this.http.get<ApiResponse<LoyaltyTransaction[]>>(`${this.meBase}/loyalty/transactions`)
      .pipe(map(r => r.data ?? []));
  }

  listRewards(): Observable<LoyaltyReward[]> {
    return this.http.get<ApiResponse<LoyaltyReward[]>>(`${this.meBase}/loyalty/rewards`)
      .pipe(map(r => r.data ?? []));
  }

  redeemReward(request: RedeemLoyaltyRewardRequest, idempotencyKey: string): Observable<unknown> {
    return this.http.post<ApiResponse<unknown>>(`${this.meBase}/loyalty/redeem`, request, {
      headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey })
    }).pipe(map(r => r.data));
  }

  getStoreCredit(): Observable<StoreCreditAccount> {
    return this.http.get<ApiResponse<StoreCreditAccount>>(`${this.meBase}/store-credit`)
      .pipe(map(r => r.data!));
  }

  listActivity(): Observable<CustomerActivity[]> {
    return this.http.get<ApiResponse<CustomerActivity[]>>(`${this.meBase}/activity`)
      .pipe(map(r => r.data ?? []));
  }

  assignCustomerGroupAdmin(customerId: number, request: AssignCustomerGroupRequest): Observable<void> {
    return this.http.put<ApiResponse<unknown>>(`${this.adminCustomerBase}/${customerId}/group`, request)
      .pipe(map(() => undefined));
  }

  getPurchaseHistoryAdmin(customerId: number): Observable<CustomerPurchaseHistoryItem[]> {
    return this.http.get<ApiResponse<CustomerPurchaseHistoryItem[]>>(`${this.adminCustomerBase}/${customerId}/purchase-history`)
      .pipe(map(r => r.data ?? []));
  }

  getLoyaltyAdmin(customerId: number, storeId: number): Observable<LoyaltyAccount> {
    return this.http.get<ApiResponse<LoyaltyAccount>>(`${this.adminCustomerBase}/${customerId}/loyalty?storeId=${storeId}`)
      .pipe(map(r => r.data!));
  }

  creditStoreCreditAdmin(
    customerId: number,
    storeId: number,
    currencyCode: string,
    request: CreditStoreCreditRequest,
    idempotencyKey: string
  ): Observable<unknown> {
    return this.http.post<ApiResponse<unknown>>(
      `${this.adminCustomerBase}/${customerId}/store-credit/credit?storeId=${storeId}&currencyCode=${encodeURIComponent(currencyCode)}`,
      request,
      { headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }) }
    ).pipe(map(r => r.data));
  }

  listActivityAdmin(customerId: number, storeId?: number): Observable<CustomerActivity[]> {
    const query = storeId ? `?storeId=${storeId}` : '';
    return this.http.get<ApiResponse<CustomerActivity[]>>(`${this.adminCustomerBase}/${customerId}/activity${query}`)
      .pipe(map(r => r.data ?? []));
  }

  listSegmentsAdmin(storeId?: number): Observable<CustomerSegmentSummary[]> {
    const query = storeId ? `?storeId=${storeId}` : '';
    return this.http.get<ApiResponse<CustomerSegmentSummary[]>>(`${this.adminSegmentsBase}${query}`)
      .pipe(map(r => r.data ?? []));
  }

  createSegmentAdmin(request: CreateCustomerSegmentRequest): Observable<CustomerSegmentSummary> {
    return this.http.post<ApiResponse<CustomerSegmentSummary>>(this.adminSegmentsBase, request)
      .pipe(map(r => r.data!));
  }

  listRewardsAdmin(storeId?: number): Observable<LoyaltyReward[]> {
    const query = storeId ? `?storeId=${storeId}` : '';
    return this.http.get<ApiResponse<LoyaltyReward[]>>(`${this.adminRewardsBase}${query}`)
      .pipe(map(r => r.data ?? []));
  }

  createRewardAdmin(request: CreateLoyaltyRewardRequest): Observable<LoyaltyReward> {
    return this.http.post<ApiResponse<LoyaltyReward>>(this.adminRewardsBase, request)
      .pipe(map(r => r.data!));
  }
}
