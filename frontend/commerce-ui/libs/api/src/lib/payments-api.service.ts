import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CreatePaymentMethodRequest,
  CreatePaymentRequest,
  CreatePaymentResult,
  PartialRefundPaymentRequest,
  PaymentDetail,
  PaymentListQuery,
  PaymentMethodDetail,
  PaymentMethodSummary,
  PaymentTransaction,
  PagedPaymentSummaryResult,
  RefundPaymentRequest,
  UpdatePaymentMethodRequest
} from './models/payment.models';

@Injectable({ providedIn: 'root' })
export class PaymentsApi {
  private readonly http = inject(HttpClient);
  private readonly paymentsBase = '/api/payments';
  private readonly adminPaymentsBase = '/api/admin/payments';
  private readonly methodsBase = '/api/admin/payment-methods';

  createPayment(request: CreatePaymentRequest, idempotencyKey: string): Observable<CreatePaymentResult> {
    return this.http
      .post<ApiResponse<CreatePaymentResult>>(this.paymentsBase, request, {
        headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey })
      })
      .pipe(map(response => response.data!));
  }

  getPayment(id: number): Observable<PaymentDetail> {
    return this.http
      .get<ApiResponse<PaymentDetail>>(`${this.paymentsBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  getPaymentByOrder(orderId: number): Observable<PaymentDetail> {
    return this.http
      .get<ApiResponse<PaymentDetail>>(`${this.paymentsBase}/by-order/${orderId}`)
      .pipe(map(response => response.data!));
  }

  listPayments(query: PaymentListQuery = {}): Observable<PagedPaymentSummaryResult> {
    let params = new HttpParams();
    if (query.page != null) params = params.set('page', query.page);
    if (query.pageSize != null) params = params.set('pageSize', query.pageSize);
    if (query.storeId != null) params = params.set('storeId', query.storeId);
    if (query.orderId != null) params = params.set('orderId', query.orderId);
    if (query.status != null) params = params.set('status', query.status);
    return this.http
      .get<ApiResponse<PagedPaymentSummaryResult>>(this.adminPaymentsBase, { params })
      .pipe(map(response => response.data!));
  }

  getAdminPayment(id: number): Observable<PaymentDetail> {
    return this.http
      .get<ApiResponse<PaymentDetail>>(`${this.adminPaymentsBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  getTransactions(id: number): Observable<PaymentTransaction[]> {
    return this.http
      .get<ApiResponse<PaymentTransaction[]>>(`${this.adminPaymentsBase}/${id}/transactions`)
      .pipe(map(response => response.data!));
  }

  capturePayment(id: number): Observable<PaymentDetail> {
    return this.http
      .post<ApiResponse<PaymentDetail>>(`${this.adminPaymentsBase}/${id}/capture`, {})
      .pipe(map(response => response.data!));
  }

  voidPayment(id: number): Observable<PaymentDetail> {
    return this.http
      .post<ApiResponse<PaymentDetail>>(`${this.adminPaymentsBase}/${id}/void`, {})
      .pipe(map(response => response.data!));
  }

  refundPayment(id: number, request: RefundPaymentRequest = {}): Observable<PaymentDetail> {
    return this.http
      .post<ApiResponse<PaymentDetail>>(`${this.adminPaymentsBase}/${id}/refund`, request)
      .pipe(map(response => response.data!));
  }

  partialRefundPayment(id: number, request: PartialRefundPaymentRequest): Observable<PaymentDetail> {
    return this.http
      .post<ApiResponse<PaymentDetail>>(`${this.adminPaymentsBase}/${id}/partial-refund`, request)
      .pipe(map(response => response.data!));
  }

  listMethods(storeId?: number | null): Observable<PaymentMethodSummary[]> {
    const params = storeId != null ? new HttpParams().set('storeId', storeId) : undefined;
    return this.http
      .get<ApiResponse<PaymentMethodSummary[]>>(this.methodsBase, { params })
      .pipe(map(response => response.data!));
  }

  getMethod(id: number): Observable<PaymentMethodDetail> {
    return this.http
      .get<ApiResponse<PaymentMethodDetail>>(`${this.methodsBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createMethod(request: CreatePaymentMethodRequest): Observable<PaymentMethodDetail> {
    return this.http
      .post<ApiResponse<PaymentMethodDetail>>(this.methodsBase, request)
      .pipe(map(response => response.data!));
  }

  updateMethod(id: number, request: UpdatePaymentMethodRequest): Observable<PaymentMethodDetail> {
    return this.http
      .put<ApiResponse<PaymentMethodDetail>>(`${this.methodsBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteMethod(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.methodsBase}/${id}`)
      .pipe(map(() => undefined));
  }
}
