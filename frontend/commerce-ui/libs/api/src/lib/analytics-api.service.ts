import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  ConversionReport,
  DashboardSummary,
  ReportFilterQuery,
  ReportType,
  RevenueReport
} from './models/analytics.models';

@Injectable({ providedIn: 'root' })
export class AnalyticsApi {
  private readonly http = inject(HttpClient);
  private readonly dashboardBase = '/api/admin/dashboard';
  private readonly reportsBase = '/api/admin/reports';

  getDashboardSummary(query: ReportFilterQuery = {}): Observable<DashboardSummary> {
    return this.http
      .get<ApiResponse<DashboardSummary>>(this.dashboardBase, { params: this.buildParams(query) })
      .pipe(map(response => response.data!));
  }

  getRevenueReport(query: ReportFilterQuery = {}): Observable<RevenueReport> {
    return this.http
      .get<ApiResponse<RevenueReport>>(`${this.reportsBase}/revenue`, { params: this.buildParams(query) })
      .pipe(map(response => response.data!));
  }

  getConversionReport(query: ReportFilterQuery = {}): Observable<ConversionReport> {
    return this.http
      .get<ApiResponse<ConversionReport>>(`${this.reportsBase}/conversion`, { params: this.buildParams(query) })
      .pipe(map(response => response.data!));
  }

  exportReport(reportType: ReportType, query: ReportFilterQuery = {}): Observable<Blob> {
    return this.http.get(`${this.reportsBase}/${reportType}/export`, {
      params: this.buildParams(query),
      responseType: 'blob'
    });
  }

  private buildParams(query: ReportFilterQuery): HttpParams {
    let params = new HttpParams();
    if (query.storeId != null) params = params.set('storeId', query.storeId);
    if (query.fromUtc) params = params.set('fromUtc', query.fromUtc);
    if (query.toUtc) params = params.set('toUtc', query.toUtc);
    if (query.productId != null) params = params.set('productId', query.productId);
    if (query.customerId != null) params = params.set('customerId', query.customerId);
    if (query.granularity) params = params.set('granularity', query.granularity);
    if (query.topProductsLimit != null) params = params.set('topProductsLimit', query.topProductsLimit);
    return params;
  }
}
