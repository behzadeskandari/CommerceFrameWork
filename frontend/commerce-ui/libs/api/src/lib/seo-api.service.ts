import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  SeoMetadataDto,
  SeoSettingsDto,
  UpdateSeoSettingsRequest,
  UpsertSeoMetadataRequest,
  UpsertUrlRecordRequest,
  UrlRecordDto
} from './models/seo.models';

@Injectable({ providedIn: 'root' })
export class SeoApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/admin/seo';

  listUrlRecords(storeId?: number): Observable<UrlRecordDto[]> {
    const query = storeId != null ? `?storeId=${storeId}` : '';
    return this.http
      .get<ApiResponse<UrlRecordDto[]>>(`${this.base}/url-records${query}`)
      .pipe(map(response => response.data!));
  }

  upsertUrlRecord(request: UpsertUrlRecordRequest): Observable<UrlRecordDto> {
    return this.http
      .put<ApiResponse<UrlRecordDto>>(`${this.base}/url-records`, request)
      .pipe(map(response => response.data!));
  }

  upsertMetadata(request: UpsertSeoMetadataRequest): Observable<SeoMetadataDto> {
    return this.http
      .put<ApiResponse<SeoMetadataDto>>(`${this.base}/metadata`, request)
      .pipe(map(response => response.data!));
  }

  getSettings(storeId: number): Observable<SeoSettingsDto> {
    return this.http
      .get<ApiResponse<SeoSettingsDto>>(`${this.base}/settings/${storeId}`)
      .pipe(map(response => response.data!));
  }

  updateSettings(storeId: number, request: UpdateSeoSettingsRequest): Observable<SeoSettingsDto> {
    return this.http
      .put<ApiResponse<SeoSettingsDto>>(`${this.base}/settings/${storeId}`, request)
      .pipe(map(response => response.data!));
  }
}
