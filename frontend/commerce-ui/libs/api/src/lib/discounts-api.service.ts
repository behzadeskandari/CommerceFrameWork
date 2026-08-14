import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CouponDetail,
  CouponSummary,
  CreateCouponRequest,
  CreateDiscountRequest,
  DiscountDetail,
  DiscountSummary,
  UpdateCouponRequest,
  UpdateDiscountRequest
} from './models/pricing.models';

@Injectable({ providedIn: 'root' })
export class DiscountsApi {
  private readonly http = inject(HttpClient);
  private readonly discountsBase = '/api/admin/discounts';
  private readonly couponsBase = '/api/admin/coupons';

  listDiscounts(): Observable<DiscountSummary[]> {
    return this.http
      .get<ApiResponse<DiscountSummary[]>>(this.discountsBase)
      .pipe(map(response => response.data!));
  }

  getDiscount(id: number): Observable<DiscountDetail> {
    return this.http
      .get<ApiResponse<DiscountDetail>>(`${this.discountsBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createDiscount(request: CreateDiscountRequest): Observable<DiscountDetail> {
    return this.http
      .post<ApiResponse<DiscountDetail>>(this.discountsBase, request)
      .pipe(map(response => response.data!));
  }

  updateDiscount(id: number, request: UpdateDiscountRequest): Observable<DiscountDetail> {
    return this.http
      .put<ApiResponse<DiscountDetail>>(`${this.discountsBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteDiscount(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.discountsBase}/${id}`)
      .pipe(map(() => undefined));
  }

  activateDiscount(id: number): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.discountsBase}/${id}/activate`, {})
      .pipe(map(() => undefined));
  }

  deactivateDiscount(id: number): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.discountsBase}/${id}/deactivate`, {})
      .pipe(map(() => undefined));
  }

  listCoupons(): Observable<CouponSummary[]> {
    return this.http
      .get<ApiResponse<CouponSummary[]>>(this.couponsBase)
      .pipe(map(response => response.data!));
  }

  getCoupon(id: number): Observable<CouponDetail> {
    return this.http
      .get<ApiResponse<CouponDetail>>(`${this.couponsBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createCoupon(request: CreateCouponRequest): Observable<CouponDetail> {
    return this.http
      .post<ApiResponse<CouponDetail>>(this.couponsBase, request)
      .pipe(map(response => response.data!));
  }

  updateCoupon(id: number, request: UpdateCouponRequest): Observable<CouponDetail> {
    return this.http
      .put<ApiResponse<CouponDetail>>(`${this.couponsBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteCoupon(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.couponsBase}/${id}`)
      .pipe(map(() => undefined));
  }
}
