import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG } from '@commerce/core';
import { ApiResponse } from '@commerce/core';
import {
  CreateCurrencyRequest,
  CreateLanguageRequest,
  CreateStoreRequest,
  CurrencySummary,
  LanguageSummary,
  SettingEntry,
  StoreContext,
  StoreDetail,
  StoreSummary,
  UpdateCurrencyRequest,
  UpdateLanguageRequest,
  UpdateSettingsRequest,
  UpdateStoreRequest
} from './models/store.models';

@Injectable({ providedIn: 'root' })
export class StoreApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  getContext(): Observable<StoreContext> {
    return this.http
      .get<ApiResponse<StoreContext>>(`${this.config.apiBaseUrl}/api/store/context`)
      .pipe(map(response => response.data!));
  }

  listStores(): Observable<StoreSummary[]> {
    return this.http
      .get<ApiResponse<StoreSummary[]>>(`${this.config.apiBaseUrl}/api/stores`)
      .pipe(map(response => response.data ?? []));
  }

  getStore(id: number): Observable<StoreDetail> {
    return this.http
      .get<ApiResponse<StoreDetail>>(`${this.config.apiBaseUrl}/api/stores/${id}`)
      .pipe(map(response => response.data!));
  }

  createStore(request: CreateStoreRequest): Observable<StoreDetail> {
    return this.http
      .post<ApiResponse<StoreDetail>>(`${this.config.apiBaseUrl}/api/stores`, request)
      .pipe(map(response => response.data!));
  }

  updateStore(id: number, request: UpdateStoreRequest): Observable<StoreDetail> {
    return this.http
      .put<ApiResponse<StoreDetail>>(`${this.config.apiBaseUrl}/api/stores/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteStore(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.config.apiBaseUrl}/api/stores/${id}`)
      .pipe(map(() => undefined));
  }

  listLanguages(): Observable<LanguageSummary[]> {
    return this.http
      .get<ApiResponse<LanguageSummary[]>>(`${this.config.apiBaseUrl}/api/languages`)
      .pipe(map(response => response.data ?? []));
  }

  createLanguage(request: CreateLanguageRequest): Observable<LanguageSummary> {
    return this.http
      .post<ApiResponse<LanguageSummary>>(`${this.config.apiBaseUrl}/api/languages`, request)
      .pipe(map(response => response.data!));
  }

  updateLanguage(id: number, request: UpdateLanguageRequest): Observable<LanguageSummary> {
    return this.http
      .put<ApiResponse<LanguageSummary>>(`${this.config.apiBaseUrl}/api/languages/${id}`, request)
      .pipe(map(response => response.data!));
  }

  selectLanguage(languageCode: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.config.apiBaseUrl}/api/languages/select/${languageCode}`, null)
      .pipe(map(() => undefined));
  }

  listCurrencies(): Observable<CurrencySummary[]> {
    return this.http
      .get<ApiResponse<CurrencySummary[]>>(`${this.config.apiBaseUrl}/api/currencies`)
      .pipe(map(response => response.data ?? []));
  }

  createCurrency(request: CreateCurrencyRequest): Observable<CurrencySummary> {
    return this.http
      .post<ApiResponse<CurrencySummary>>(`${this.config.apiBaseUrl}/api/currencies`, request)
      .pipe(map(response => response.data!));
  }

  updateCurrency(id: number, request: UpdateCurrencyRequest): Observable<CurrencySummary> {
    return this.http
      .put<ApiResponse<CurrencySummary>>(`${this.config.apiBaseUrl}/api/currencies/${id}`, request)
      .pipe(map(response => response.data!));
  }

  listSettings(): Observable<SettingEntry[]> {
    return this.http
      .get<ApiResponse<SettingEntry[]>>(`${this.config.apiBaseUrl}/api/settings`)
      .pipe(map(response => response.data ?? []));
  }

  updateSettings(request: UpdateSettingsRequest): Observable<void> {
    return this.http
      .put<ApiResponse<unknown>>(`${this.config.apiBaseUrl}/api/settings`, request)
      .pipe(map(() => undefined));
  }
}
