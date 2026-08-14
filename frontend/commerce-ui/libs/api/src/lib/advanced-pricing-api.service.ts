import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CustomerGroup,
  CustomerGroupPrice,
  OfferTierPrice,
  PricePreviewResult,
  TaxSettings
} from './models/advanced-pricing.models';

@Injectable({ providedIn: 'root' })
export class AdvancedPricingApi {
  private readonly http = inject(HttpClient);

  listCustomerGroups(storeId?: number): Observable<CustomerGroup[]> {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return this.http
      .get<ApiResponse<CustomerGroup[]>>('/api/admin/pricing/customer-groups', { params })
      .pipe(map(r => r.data ?? []));
  }

  createCustomerGroup(body: Pick<CustomerGroup, 'storeId' | 'name' | 'code' | 'isActive' | 'displayOrder'>): Observable<CustomerGroup> {
    return this.http.post<ApiResponse<CustomerGroup>>('/api/admin/pricing/customer-groups', body).pipe(map(r => r.data!));
  }

  updateCustomerGroup(id: number, body: Pick<CustomerGroup, 'name' | 'code' | 'isActive' | 'displayOrder'>): Observable<CustomerGroup> {
    return this.http.put<ApiResponse<CustomerGroup>>(`/api/admin/pricing/customer-groups/${id}`, body).pipe(map(r => r.data!));
  }

  deleteCustomerGroup(id: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`/api/admin/pricing/customer-groups/${id}`).pipe(map(() => undefined));
  }

  listGroupPrices(groupId: number): Observable<CustomerGroupPrice[]> {
    return this.http
      .get<ApiResponse<CustomerGroupPrice[]>>(`/api/admin/pricing/customer-groups/${groupId}/prices`)
      .pipe(map(r => r.data ?? []));
  }

  listOfferTierPrices(offerId: number): Observable<OfferTierPrice[]> {
    return this.http
      .get<ApiResponse<OfferTierPrice[]>>(`/api/admin/catalog/offers/${offerId}/tier-prices`)
      .pipe(map(r => r.data ?? []));
  }

  addOfferTierPrice(offerId: number, body: { minQuantity: number; price: number; isActive?: boolean }): Observable<OfferTierPrice> {
    return this.http
      .post<ApiResponse<OfferTierPrice>>(`/api/admin/catalog/offers/${offerId}/tier-prices`, body)
      .pipe(map(r => r.data!));
  }

  removeOfferTierPrice(offerId: number, tierPriceId: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`/api/admin/catalog/offers/${offerId}/tier-prices/${tierPriceId}`)
      .pipe(map(() => undefined));
  }

  getTaxSettings(storeId?: number): Observable<TaxSettings> {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return this.http.get<ApiResponse<TaxSettings>>('/api/admin/tax/settings', { params }).pipe(map(r => r.data!));
  }

  saveTaxSettings(body: TaxSettings, storeId?: number): Observable<TaxSettings> {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return this.http.put<ApiResponse<TaxSettings>>('/api/admin/tax/settings', {
      enabled: body.enabled,
      pricesIncludeTax: body.pricesIncludeTax,
      defaultCategoryId: body.defaultCategoryId ?? null,
      shippingTaxableByDefault: body.shippingTaxableByDefault
    }, { params }).pipe(map(r => r.data!));
  }

  previewPrice(body: { offerId: number; quantity: number; customerId?: number | null; customerGroupId?: number | null; currencyCode: string }): Observable<PricePreviewResult> {
    return this.http.post<ApiResponse<PricePreviewResult>>('/api/admin/pricing/preview', body).pipe(map(r => r.data!));
  }
}
