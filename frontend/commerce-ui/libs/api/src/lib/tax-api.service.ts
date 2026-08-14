import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CreateTaxCategoryRequest,
  CreateTaxRateRequest,
  CreateTaxZoneRequest,
  TaxCategoryDetail,
  TaxCategorySummary,
  TaxRateDetail,
  TaxRateSummary,
  TaxZoneDetail,
  TaxZoneSummary,
  UpdateTaxCategoryRequest,
  UpdateTaxRateRequest,
  UpdateTaxZoneRequest
} from './models/tax.models';

@Injectable({ providedIn: 'root' })
export class TaxApi {
  private readonly http = inject(HttpClient);
  private readonly categoriesBase = '/api/admin/tax/categories';
  private readonly zonesBase = '/api/admin/tax/zones';
  private readonly ratesBase = '/api/admin/tax/rates';

  listCategories(storeId?: number | null): Observable<TaxCategorySummary[]> {
    const params = storeId != null ? new HttpParams().set('storeId', storeId) : undefined;
    return this.http
      .get<ApiResponse<TaxCategorySummary[]>>(this.categoriesBase, { params })
      .pipe(map(response => response.data!));
  }

  getCategory(id: number): Observable<TaxCategoryDetail> {
    return this.http
      .get<ApiResponse<TaxCategoryDetail>>(`${this.categoriesBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createCategory(request: CreateTaxCategoryRequest): Observable<TaxCategoryDetail> {
    return this.http
      .post<ApiResponse<TaxCategoryDetail>>(this.categoriesBase, request)
      .pipe(map(response => response.data!));
  }

  updateCategory(id: number, request: UpdateTaxCategoryRequest): Observable<TaxCategoryDetail> {
    return this.http
      .put<ApiResponse<TaxCategoryDetail>>(`${this.categoriesBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteCategory(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.categoriesBase}/${id}`)
      .pipe(map(() => undefined));
  }

  listZones(storeId?: number | null): Observable<TaxZoneSummary[]> {
    const params = storeId != null ? new HttpParams().set('storeId', storeId) : undefined;
    return this.http
      .get<ApiResponse<TaxZoneSummary[]>>(this.zonesBase, { params })
      .pipe(map(response => response.data!));
  }

  getZone(id: number): Observable<TaxZoneDetail> {
    return this.http
      .get<ApiResponse<TaxZoneDetail>>(`${this.zonesBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createZone(request: CreateTaxZoneRequest): Observable<TaxZoneDetail> {
    return this.http
      .post<ApiResponse<TaxZoneDetail>>(this.zonesBase, request)
      .pipe(map(response => response.data!));
  }

  updateZone(id: number, request: UpdateTaxZoneRequest): Observable<TaxZoneDetail> {
    return this.http
      .put<ApiResponse<TaxZoneDetail>>(`${this.zonesBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteZone(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.zonesBase}/${id}`)
      .pipe(map(() => undefined));
  }

  listRates(storeId?: number | null, categoryId?: number | null): Observable<TaxRateSummary[]> {
    let params = new HttpParams();
    if (storeId != null) params = params.set('storeId', storeId);
    if (categoryId != null) params = params.set('categoryId', categoryId);
    return this.http
      .get<ApiResponse<TaxRateSummary[]>>(this.ratesBase, { params: params.keys().length ? params : undefined })
      .pipe(map(response => response.data!));
  }

  getRate(id: number): Observable<TaxRateDetail> {
    return this.http
      .get<ApiResponse<TaxRateDetail>>(`${this.ratesBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createRate(request: CreateTaxRateRequest): Observable<TaxRateDetail> {
    return this.http
      .post<ApiResponse<TaxRateDetail>>(this.ratesBase, request)
      .pipe(map(response => response.data!));
  }

  updateRate(id: number, request: UpdateTaxRateRequest): Observable<TaxRateDetail> {
    return this.http
      .put<ApiResponse<TaxRateDetail>>(`${this.ratesBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteRate(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.ratesBase}/${id}`)
      .pipe(map(() => undefined));
  }
}
