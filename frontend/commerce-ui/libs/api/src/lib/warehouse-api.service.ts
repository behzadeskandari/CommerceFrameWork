import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CreateStockLocationRequest,
  CreateWarehouseRequest,
  UpdateWarehouseRequest,
  WarehouseDetail,
  WarehouseSummary
} from './models/warehouse.models';

@Injectable({ providedIn: 'root' })
export class WarehouseApi {
  private readonly http = inject(HttpClient);

  list(storeId?: number): Observable<WarehouseSummary[]> {
    let params = new HttpParams();
    if (storeId != null) params = params.set('storeId', String(storeId));

    return this.http
      .get<ApiResponse<WarehouseSummary[]>>('/api/admin/inventory/warehouses', { params })
      .pipe(map(response => response.data!));
  }

  getById(id: number): Observable<WarehouseDetail> {
    return this.http
      .get<ApiResponse<WarehouseDetail>>(`/api/admin/inventory/warehouses/${id}`)
      .pipe(map(response => response.data!));
  }

  create(request: CreateWarehouseRequest): Observable<WarehouseDetail> {
    return this.http
      .post<ApiResponse<WarehouseDetail>>('/api/admin/inventory/warehouses', request)
      .pipe(map(response => response.data!));
  }

  update(id: number, request: UpdateWarehouseRequest): Observable<WarehouseDetail> {
    return this.http
      .put<ApiResponse<WarehouseDetail>>(`/api/admin/inventory/warehouses/${id}`, request)
      .pipe(map(response => response.data!));
  }

  activate(id: number): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`/api/admin/inventory/warehouses/${id}/activate`, {})
      .pipe(map(() => undefined));
  }

  deactivate(id: number): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`/api/admin/inventory/warehouses/${id}/deactivate`, {})
      .pipe(map(() => undefined));
  }

  createLocation(warehouseId: number, request: CreateStockLocationRequest): Observable<unknown> {
    return this.http
      .post<ApiResponse<unknown>>(`/api/admin/inventory/warehouses/${warehouseId}/locations`, request)
      .pipe(map(response => response.data!));
  }
}
