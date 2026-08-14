import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  AdjustInventoryStockRequest,
  CreateInventoryItemRequest,
  InventoryItemDetail,
  InventoryListQuery,
  InventoryMovement,
  InventoryReservation,
  PagedInventorySummaryResult
} from './models/inventory.models';
import {
  ReceiveIncomingStockRequest,
  SetLowStockThresholdRequest,
  TransferInventoryStockRequest
} from './models/warehouse.models';

@Injectable({ providedIn: 'root' })
export class InventoryApi {
  private readonly http = inject(HttpClient);

  list(query: InventoryListQuery = {}): Observable<PagedInventorySummaryResult> {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 20));

    if (query.storeId != null) params = params.set('storeId', String(query.storeId));
    if (query.offerId != null) params = params.set('offerId', String(query.offerId));
    if (query.productId != null) params = params.set('productId', String(query.productId));
    if (query.warehouseId != null) params = params.set('warehouseId', String(query.warehouseId));
    if (query.availabilityStatus) params = params.set('availabilityStatus', query.availabilityStatus);

    return this.http
      .get<ApiResponse<PagedInventorySummaryResult>>('/api/admin/inventory', { params })
      .pipe(map(response => response.data!));
  }

  getById(id: number): Observable<InventoryItemDetail> {
    return this.http
      .get<ApiResponse<InventoryItemDetail>>(`/api/admin/inventory/${id}`)
      .pipe(map(response => response.data!));
  }

  create(request: CreateInventoryItemRequest): Observable<InventoryItemDetail> {
    return this.http
      .post<ApiResponse<InventoryItemDetail>>('/api/admin/inventory', request)
      .pipe(map(response => response.data!));
  }

  adjust(id: number, request: AdjustInventoryStockRequest): Observable<InventoryItemDetail> {
    return this.http
      .post<ApiResponse<InventoryItemDetail>>(`/api/admin/inventory/${id}/adjust`, request)
      .pipe(map(response => response.data!));
  }

  transfer(request: TransferInventoryStockRequest): Observable<{ sourceMovement: InventoryMovement; destinationMovement: InventoryMovement }> {
    return this.http
      .post<ApiResponse<{ sourceMovement: InventoryMovement; destinationMovement: InventoryMovement }>>('/api/admin/inventory/transfer', request)
      .pipe(map(response => response.data!));
  }

  receiveIncoming(id: number, request: ReceiveIncomingStockRequest): Observable<InventoryItemDetail> {
    return this.http
      .post<ApiResponse<InventoryItemDetail>>(`/api/admin/inventory/${id}/receive-incoming`, request)
      .pipe(map(response => response.data!));
  }

  setLowStockThreshold(id: number, request: SetLowStockThresholdRequest): Observable<InventoryItemDetail> {
    return this.http
      .post<ApiResponse<InventoryItemDetail>>(`/api/admin/inventory/${id}/low-stock-threshold`, request)
      .pipe(map(response => response.data!));
  }

  listMovements(id: number): Observable<InventoryMovement[]> {
    return this.http
      .get<ApiResponse<InventoryMovement[]>>(`/api/admin/inventory/${id}/movements`)
      .pipe(map(response => response.data!));
  }

  listReservations(id: number): Observable<InventoryReservation[]> {
    return this.http
      .get<ApiResponse<InventoryReservation[]>>(`/api/admin/inventory/${id}/reservations`)
      .pipe(map(response => response.data!));
  }
}
