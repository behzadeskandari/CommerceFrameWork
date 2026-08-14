import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  PluginDetail,
  PluginMigrationStatus,
  PluginPermissionEntry,
  PluginSettingEntry,
  PluginStoreConfiguration,
  PluginSummary,
  PluginUiMetadata,
  PluginUninstallMode
} from './models/plugin.models';

@Injectable({ providedIn: 'root' })
export class PluginsApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/admin/plugins';

  list(): Observable<PluginSummary[]> {
    return this.http
      .get<ApiResponse<PluginSummary[]>>(this.base)
      .pipe(map(response => response.data!));
  }

  get(systemName: string): Observable<PluginDetail> {
    return this.http
      .get<ApiResponse<PluginDetail>>(`${this.base}/${encodeURIComponent(systemName)}`)
      .pipe(map(response => response.data!));
  }

  getSettings(systemName: string, storeId?: number): Observable<PluginSettingEntry[]> {
    const query = storeId != null ? `?storeId=${storeId}` : '';
    return this.http
      .get<ApiResponse<PluginSettingEntry[]>>(`${this.base}/${encodeURIComponent(systemName)}/settings${query}`)
      .pipe(map(response => response.data ?? []));
  }

  saveSettings(systemName: string, values: Record<string, string>, storeId?: number): Observable<void> {
    const query = storeId != null ? `?storeId=${storeId}` : '';
    return this.http
      .put<ApiResponse<unknown>>(`${this.base}/${encodeURIComponent(systemName)}/settings${query}`, values)
      .pipe(map(() => undefined));
  }

  getPermissions(systemName: string): Observable<PluginPermissionEntry[]> {
    return this.http
      .get<ApiResponse<PluginPermissionEntry[]>>(`${this.base}/${encodeURIComponent(systemName)}/permissions`)
      .pipe(map(response => response.data ?? []));
  }

  getStoreConfigurations(systemName: string): Observable<PluginStoreConfiguration[]> {
    return this.http
      .get<ApiResponse<PluginStoreConfiguration[]>>(`${this.base}/${encodeURIComponent(systemName)}/stores`)
      .pipe(map(response => response.data ?? []));
  }

  saveStoreConfiguration(systemName: string, configuration: PluginStoreConfiguration): Observable<void> {
    return this.http
      .put<ApiResponse<unknown>>(`${this.base}/${encodeURIComponent(systemName)}/stores`, configuration)
      .pipe(map(() => undefined));
  }

  getMigrationStatus(systemName: string): Observable<PluginMigrationStatus[]> {
    return this.http
      .get<ApiResponse<PluginMigrationStatus[]>>(`${this.base}/${encodeURIComponent(systemName)}/migrations`)
      .pipe(map(response => response.data ?? []));
  }

  getUiMetadata(systemName: string): Observable<PluginUiMetadata> {
    return this.http
      .get<ApiResponse<PluginUiMetadata>>(`${this.base}/${encodeURIComponent(systemName)}/ui`)
      .pipe(map(response => response.data ?? { adminNavItems: [], contributions: [] }));
  }

  getLocalization(systemName: string, culture: string): Observable<Record<string, string>> {
    return this.http
      .get<ApiResponse<Record<string, string>>>(
        `${this.base}/${encodeURIComponent(systemName)}/localization/${encodeURIComponent(culture)}`
      )
      .pipe(map(response => response.data ?? {}));
  }

  install(systemName: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/${encodeURIComponent(systemName)}/install`, {})
      .pipe(map(() => undefined));
  }

  enable(systemName: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/${encodeURIComponent(systemName)}/enable`, {})
      .pipe(map(() => undefined));
  }

  disable(systemName: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/${encodeURIComponent(systemName)}/disable`, {})
      .pipe(map(() => undefined));
  }

  uninstall(systemName: string, mode: PluginUninstallMode = 'KeepData'): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(
        `${this.base}/${encodeURIComponent(systemName)}/uninstall?uninstallMode=${mode}`,
        {}
      )
      .pipe(map(() => undefined));
  }

  reload(systemName: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/${encodeURIComponent(systemName)}/reload`, {})
      .pipe(map(() => undefined));
  }

  installPackage(file: File): Observable<void> {
    const formData = new FormData();
    formData.append('package', file);
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/install-package`, formData)
      .pipe(map(() => undefined));
  }
}
