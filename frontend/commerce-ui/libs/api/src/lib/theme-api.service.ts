import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { APP_CONFIG, ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  StoreThemeAssignment,
  ThemeDetail,
  ThemeRuntime,
  ThemeSummary,
  UpdateStoreThemeAssignmentRequest
} from './models/theme.models';

@Injectable({ providedIn: 'root' })
export class ThemeApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  getRuntime(): Observable<ThemeRuntime> {
    return this.http.get<ApiResponse<ThemeRuntime>>(`${this.config.apiBaseUrl}/api/themes/runtime`).pipe(map(r => r.data!));
  }

  listThemes(): Observable<ThemeSummary[]> {
    return this.http.get<ApiResponse<ThemeSummary[]>>(`${this.config.apiBaseUrl}/api/admin/themes`).pipe(map(r => r.data ?? []));
  }

  getTheme(systemName: string): Observable<ThemeDetail> {
    return this.http.get<ApiResponse<ThemeDetail>>(`${this.config.apiBaseUrl}/api/admin/themes/${encodeURIComponent(systemName)}`).pipe(map(r => r.data!));
  }

  getStoreAssignment(storeId: number): Observable<StoreThemeAssignment | null> {
    return this.http.get<ApiResponse<StoreThemeAssignment | null>>(`${this.config.apiBaseUrl}/api/admin/themes/store/${storeId}`).pipe(map(r => r.data ?? null));
  }

  saveStoreAssignment(storeId: number, body: UpdateStoreThemeAssignmentRequest): Observable<StoreThemeAssignment> {
    return this.http.put<ApiResponse<StoreThemeAssignment>>(`${this.config.apiBaseUrl}/api/admin/themes/store/${storeId}`, body).pipe(map(r => r.data!));
  }
}
