import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CustomerDownloadEntitlement,
  DownloadHistoryEntry,
  ProductDownloadFile,
  ProductDownloadSettings
} from './models/downloads.models';

@Injectable({ providedIn: 'root' })
export class DownloadsApi {
  private readonly http = inject(HttpClient);

  getProductSettings(productId: number): Observable<ProductDownloadSettings | null> {
    return this.http
      .get<ApiResponse<ProductDownloadSettings | null>>(`/api/admin/downloads/products/${productId}/settings`)
      .pipe(map(r => r.data ?? null));
  }

  saveProductSettings(
    productId: number,
    settings: Pick<ProductDownloadSettings, 'isEnabled' | 'maxDownloadCount' | 'expirationDays'>
  ): Observable<ProductDownloadSettings> {
    return this.http
      .put<ApiResponse<ProductDownloadSettings>>(`/api/admin/downloads/products/${productId}/settings`, settings)
      .pipe(map(r => r.data!));
  }

  listProductFiles(productId: number): Observable<ProductDownloadFile[]> {
    return this.http
      .get<ApiResponse<ProductDownloadFile[]>>(`/api/admin/downloads/products/${productId}/files`)
      .pipe(map(r => r.data ?? []));
  }

  addProductFile(
    productId: number,
    request: { mediaAssetId: number; displayName?: string | null; displayOrder: number; isActive?: boolean }
  ): Observable<ProductDownloadFile> {
    return this.http
      .post<ApiResponse<ProductDownloadFile>>(`/api/admin/downloads/products/${productId}/files`, request)
      .pipe(map(r => r.data!));
  }

  removeProductFile(productId: number, fileId: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`/api/admin/downloads/products/${productId}/files/${fileId}`)
      .pipe(map(() => undefined));
  }

  getProductHistory(productId: number): Observable<DownloadHistoryEntry[]> {
    return this.http
      .get<ApiResponse<DownloadHistoryEntry[]>>(`/api/admin/downloads/products/${productId}/history`)
      .pipe(map(r => r.data ?? []));
  }

  listCustomerDownloads(): Observable<CustomerDownloadEntitlement[]> {
    return this.http
      .get<ApiResponse<CustomerDownloadEntitlement[]>>('/api/downloads')
      .pipe(map(r => r.data ?? []));
  }

  downloadUrl(entitlementId: number, fileId: number): string {
    return `/api/downloads/${entitlementId}/files/${fileId}`;
  }
}
