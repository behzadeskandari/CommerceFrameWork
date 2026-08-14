import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CreateShippingMethodRequest,
  CreateShippingRateRequest,
  CreateShippingZoneRequest,
  ShippingMethodDetail,
  ShippingMethodSummary,
  ShippingRateDetail,
  ShippingRateSummary,
  ShippingZoneDetail,
  ShippingZoneSummary,
  UpdateShippingMethodRequest,
  UpdateShippingRateRequest,
  UpdateShippingZoneRequest
} from './models/shipping.models';

@Injectable({ providedIn: 'root' })
export class ShippingApi {
  private readonly http = inject(HttpClient);
  private readonly methodsBase = '/api/admin/shipping/methods';
  private readonly zonesBase = '/api/admin/shipping/zones';
  private readonly ratesBase = '/api/admin/shipping/rates';

  listMethods(storeId?: number | null): Observable<ShippingMethodSummary[]> {
    const params = storeId != null ? new HttpParams().set('storeId', storeId) : undefined;
    return this.http
      .get<ApiResponse<ShippingMethodSummary[]>>(this.methodsBase, { params })
      .pipe(map(response => response.data!));
  }

  getMethod(id: number): Observable<ShippingMethodDetail> {
    return this.http
      .get<ApiResponse<ShippingMethodDetail>>(`${this.methodsBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createMethod(request: CreateShippingMethodRequest): Observable<ShippingMethodDetail> {
    return this.http
      .post<ApiResponse<ShippingMethodDetail>>(this.methodsBase, request)
      .pipe(map(response => response.data!));
  }

  updateMethod(id: number, request: UpdateShippingMethodRequest): Observable<ShippingMethodDetail> {
    return this.http
      .put<ApiResponse<ShippingMethodDetail>>(`${this.methodsBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteMethod(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.methodsBase}/${id}`)
      .pipe(map(() => undefined));
  }

  listZones(storeId?: number | null): Observable<ShippingZoneSummary[]> {
    const params = storeId != null ? new HttpParams().set('storeId', storeId) : undefined;
    return this.http
      .get<ApiResponse<ShippingZoneSummary[]>>(this.zonesBase, { params })
      .pipe(map(response => response.data!));
  }

  getZone(id: number): Observable<ShippingZoneDetail> {
    return this.http
      .get<ApiResponse<ShippingZoneDetail>>(`${this.zonesBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createZone(request: CreateShippingZoneRequest): Observable<ShippingZoneDetail> {
    return this.http
      .post<ApiResponse<ShippingZoneDetail>>(this.zonesBase, request)
      .pipe(map(response => response.data!));
  }

  updateZone(id: number, request: UpdateShippingZoneRequest): Observable<ShippingZoneDetail> {
    return this.http
      .put<ApiResponse<ShippingZoneDetail>>(`${this.zonesBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteZone(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.zonesBase}/${id}`)
      .pipe(map(() => undefined));
  }

  listRates(storeId?: number | null, methodId?: number | null): Observable<ShippingRateSummary[]> {
    let params = new HttpParams();
    if (storeId != null) params = params.set('storeId', storeId);
    if (methodId != null) params = params.set('methodId', methodId);
    return this.http
      .get<ApiResponse<ShippingRateSummary[]>>(this.ratesBase, { params: params.keys().length ? params : undefined })
      .pipe(map(response => response.data!));
  }

  getRate(id: number): Observable<ShippingRateDetail> {
    return this.http
      .get<ApiResponse<ShippingRateDetail>>(`${this.ratesBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createRate(request: CreateShippingRateRequest): Observable<ShippingRateDetail> {
    return this.http
      .post<ApiResponse<ShippingRateDetail>>(this.ratesBase, request)
      .pipe(map(response => response.data!));
  }

  updateRate(id: number, request: UpdateShippingRateRequest): Observable<ShippingRateDetail> {
    return this.http
      .put<ApiResponse<ShippingRateDetail>>(`${this.ratesBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteRate(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.ratesBase}/${id}`)
      .pipe(map(() => undefined));
  }
}
