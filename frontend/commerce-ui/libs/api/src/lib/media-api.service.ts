import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiResponse } from '@commerce/core';
import { MediaSummary, ProductMediaSummary } from './models/media.models';

@Injectable({ providedIn: 'root' })
export class MediaApiService {
  private readonly http = inject(HttpClient);

  list(term?: string): Observable<MediaSummary[]> {
    const query = term ? `?term=${encodeURIComponent(term)}` : '';
    return this.http
      .get<ApiResponse<MediaSummary[]>>(`/api/media${query}`)
      .pipe(map(r => r.data ?? []));
  }

  upload(file: File, isPublic = true): Observable<MediaSummary> {
    const form = new FormData();
    form.append('file', file);
    form.append('isPublic', String(isPublic));
    return this.http
      .post<ApiResponse<MediaSummary>>('/api/media/upload', form)
      .pipe(map(r => r.data!));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/media/${id}`);
  }

  getProductMedia(productId: number): Observable<ProductMediaSummary[]> {
    return this.http
      .get<ApiResponse<ProductMediaSummary[]>>(`/api/catalog/products/${productId}/media`)
      .pipe(map(r => r.data ?? []));
  }

  assignProductMedia(productId: number, mediaAssetId: number, role: string, displayOrder = 0): Observable<void> {
    return this.http.post<void>(`/api/catalog/products/${productId}/media`, {
      mediaAssetId,
      role,
      displayOrder
    });
  }

  removeProductMedia(productId: number, mediaAssetId: number): Observable<void> {
    return this.http.delete<void>(`/api/catalog/products/${productId}/media/${mediaAssetId}`);
  }
}
