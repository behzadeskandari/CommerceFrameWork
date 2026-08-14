import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CreatePromotionRequest,
  PromotionDetail,
  PromotionSummary,
  UpdatePromotionRequest
} from './models/promotions.models';

@Injectable({ providedIn: 'root' })
export class PromotionsApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/admin/promotions';

  list(storeId?: number): Observable<PromotionSummary[]> {
    const query = storeId != null ? `?storeId=${storeId}` : '';
    return this.http
      .get<ApiResponse<PromotionSummary[]>>(`${this.base}${query}`)
      .pipe(map(response => response.data!));
  }

  get(id: number): Observable<PromotionDetail> {
    return this.http
      .get<ApiResponse<PromotionDetail>>(`${this.base}/${id}`)
      .pipe(map(response => response.data!));
  }

  create(request: CreatePromotionRequest): Observable<PromotionDetail> {
    return this.http
      .post<ApiResponse<PromotionDetail>>(this.base, request)
      .pipe(map(response => response.data!));
  }

  update(id: number, request: UpdatePromotionRequest): Observable<PromotionDetail> {
    return this.http
      .put<ApiResponse<PromotionDetail>>(`${this.base}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  activate(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.base}/${id}/activate`, {}).pipe(map(() => undefined));
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.base}/${id}/deactivate`, {}).pipe(map(() => undefined));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`).pipe(map(() => undefined));
  }
}
