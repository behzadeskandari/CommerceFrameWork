import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG, ApiResponse } from '@commerce/core';
import {
  AddCustomerAddressRequest,
  AuthenticationResult,
  CustomerAddress,
  CustomerDetail,
  CustomerSummary,
  LoginRequest,
  RegisterCustomerRequest,
  UpdateCustomerAddressRequest,
  UpdateCustomerRequest
} from './models/customer.models';

@Injectable({ providedIn: 'root' })
export class CustomersApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly base = `${this.config.apiBaseUrl}/api/customers`;
  private readonly adminBase = `${this.config.apiBaseUrl}/api/admin/customers`;

  register(request: RegisterCustomerRequest): Observable<AuthenticationResult> {
    return this.http
      .post<ApiResponse<AuthenticationResult>>(`${this.base}/register`, request)
      .pipe(map(response => response.data!));
  }

  login(request: LoginRequest): Observable<AuthenticationResult> {
    return this.http
      .post<ApiResponse<AuthenticationResult>>(`${this.base}/login`, request)
      .pipe(map(response => response.data!));
  }

  logout(): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/logout`, {})
      .pipe(map(() => undefined));
  }

  getCurrentCustomer(): Observable<CustomerDetail> {
    return this.http
      .get<ApiResponse<CustomerDetail>>(`${this.base}/me`)
      .pipe(map(response => response.data!));
  }

  updateCurrentCustomer(request: UpdateCustomerRequest): Observable<CustomerDetail> {
    return this.http
      .put<ApiResponse<CustomerDetail>>(`${this.base}/me`, request)
      .pipe(map(response => response.data!));
  }

  listAddresses(): Observable<CustomerAddress[]> {
    return this.http
      .get<ApiResponse<CustomerAddress[]>>(`${this.base}/me/addresses`)
      .pipe(map(response => response.data ?? []));
  }

  addAddress(request: AddCustomerAddressRequest): Observable<CustomerAddress> {
    return this.http
      .post<ApiResponse<CustomerAddress>>(`${this.base}/me/addresses`, request)
      .pipe(map(response => response.data!));
  }

  updateAddress(id: number, request: UpdateCustomerAddressRequest): Observable<CustomerAddress> {
    return this.http
      .put<ApiResponse<CustomerAddress>>(`${this.base}/me/addresses/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteAddress(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.base}/me/addresses/${id}`)
      .pipe(map(() => undefined));
  }

  listCustomersAdmin(): Observable<CustomerSummary[]> {
    return this.http
      .get<ApiResponse<CustomerSummary[]>>(this.adminBase)
      .pipe(map(response => response.data ?? []));
  }

  getCustomerAdmin(id: number): Observable<CustomerDetail> {
    return this.http
      .get<ApiResponse<CustomerDetail>>(`${this.adminBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  updateCustomerAdmin(id: number, request: UpdateCustomerRequest): Observable<CustomerDetail> {
    return this.http
      .put<ApiResponse<CustomerDetail>>(`${this.adminBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }
}
