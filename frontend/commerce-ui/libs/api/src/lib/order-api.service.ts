import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG, ApiResponse } from '@commerce/core';
import {
  CancelOrderRequest,
  CreateOrderRequest,
  CreateOrderResult,
  OrderDetail,
  OrderListQuery,
  PagedOrderSummaryResult
} from './models/order.models';

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly base = `${this.config.apiBaseUrl}/api/orders`;
  private readonly adminBase = `${this.config.apiBaseUrl}/api/admin/orders`;

  create(request: CreateOrderRequest, idempotencyKey: string): Observable<CreateOrderResult> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http
      .post<ApiResponse<CreateOrderResult>>(this.base, request, { headers })
      .pipe(map(response => response.data!));
  }

  list(query: OrderListQuery = {}): Observable<PagedOrderSummaryResult> {
    return this.http
      .get<ApiResponse<PagedOrderSummaryResult>>(this.base, { params: this.toParams(query) })
      .pipe(map(response => response.data!));
  }

  getById(id: number): Observable<OrderDetail> {
    return this.http
      .get<ApiResponse<OrderDetail>>(`${this.base}/${id}`)
      .pipe(map(response => response.data!));
  }

  getByNumber(orderNumber: string, accessToken?: string | null): Observable<OrderDetail> {
    let params = new HttpParams();
    if (accessToken) {
      params = params.set('accessToken', accessToken);
    }
    return this.http
      .get<ApiResponse<OrderDetail>>(`${this.base}/by-number/${encodeURIComponent(orderNumber)}`, { params })
      .pipe(map(response => response.data!));
  }

  cancel(id: number, request: CancelOrderRequest = {}): Observable<OrderDetail> {
    return this.http
      .post<ApiResponse<OrderDetail>>(`${this.base}/${id}/cancel`, request)
      .pipe(map(response => response.data!));
  }

  listAdmin(query: OrderListQuery = {}): Observable<PagedOrderSummaryResult> {
    return this.http
      .get<ApiResponse<PagedOrderSummaryResult>>(this.adminBase, { params: this.toParams(query) })
      .pipe(map(response => response.data!));
  }

  getByIdAdmin(id: number): Observable<OrderDetail> {
    return this.http
      .get<ApiResponse<OrderDetail>>(`${this.adminBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  cancelAdmin(id: number, request: CancelOrderRequest = {}): Observable<OrderDetail> {
    return this.http
      .post<ApiResponse<OrderDetail>>(`${this.adminBase}/${id}/cancel`, request)
      .pipe(map(response => response.data!));
  }

  private toParams(query: OrderListQuery): HttpParams {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', String(query.page));
    if (query.pageSize != null) params = params.set('pageSize', String(query.pageSize));
    if (query.orderNumber) params = params.set('orderNumber', query.orderNumber);
    if (query.email) params = params.set('email', query.email);
    if (query.customerId != null) params = params.set('customerId', String(query.customerId));
    if (query.storeId != null) params = params.set('storeId', String(query.storeId));
    if (query.status) params = params.set('status', query.status);
    if (query.createdFromUtc) params = params.set('createdFromUtc', query.createdFromUtc);
    if (query.createdToUtc) params = params.set('createdToUtc', query.createdToUtc);
    return params;
  }
}
