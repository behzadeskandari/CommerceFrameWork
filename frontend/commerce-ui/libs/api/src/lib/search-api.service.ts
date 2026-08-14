import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  ProductSearchRequest,
  ProductSearchResponse,
  SearchIndexStatus,
  SearchSuggestionResponse
} from './models/search.models';

@Injectable({ providedIn: 'root' })
export class SearchApi {
  private readonly http = inject(HttpClient);

  searchProducts(request: ProductSearchRequest = {}): Observable<ProductSearchResponse> {
    let params = new HttpParams();
    if (request.term) params = params.set('term', request.term);
    if (request.page != null) params = params.set('page', String(request.page));
    if (request.pageSize != null) params = params.set('pageSize', String(request.pageSize));
    if (request.sortField) params = params.set('sortField', request.sortField);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);
    if (request.categoryId != null) params = params.set('categoryId', String(request.categoryId));
    if (request.manufacturer) params = params.set('manufacturer', request.manufacturer);
    if (request.minPrice != null) params = params.set('minPrice', String(request.minPrice));
    if (request.maxPrice != null) params = params.set('maxPrice', String(request.maxPrice));
    if (request.productType) params = params.set('productType', request.productType);
    if (request.isAvailable != null) params = params.set('isAvailable', String(request.isAvailable));

    return this.http.get<ApiResponse<ProductSearchResponse>>('/api/search/products', { params }).pipe(map(r => r.data!));
  }

  suggest(term: string): Observable<SearchSuggestionResponse> {
    return this.http.get<ApiResponse<SearchSuggestionResponse>>('/api/search/suggest', {
      params: { q: term }
    }).pipe(map(r => r.data!));
  }

  getStatus(): Observable<SearchIndexStatus> {
    return this.http.get<ApiResponse<SearchIndexStatus>>('/api/admin/search/status').pipe(map(r => r.data!));
  }

  rebuildIndex(): Observable<SearchIndexStatus> {
    return this.http.post<ApiResponse<SearchIndexStatus>>('/api/admin/search/rebuild', {}).pipe(map(r => r.data!));
  }
}
