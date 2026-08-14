export type SearchSortField = 'Relevance' | 'Price' | 'Newest' | 'Popularity' | 'Rating';
export type SearchSortDirection = 'Asc' | 'Desc';

export interface ProductSearchRequest {
  term?: string | null;
  page?: number;
  pageSize?: number;
  sortField?: SearchSortField;
  sortDirection?: SearchSortDirection;
  categoryId?: number | null;
  manufacturer?: string | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  productType?: string | null;
  isAvailable?: boolean | null;
}

export interface ProductSearchResultItem {
  productId: number;
  name: string;
  sku: string;
  slug?: string | null;
  shortDescription?: string | null;
  productType: string;
  price?: number | null;
  categoryIds: number[];
}

export interface ProductSearchResponse {
  items: ProductSearchResultItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  facets: SearchFacet[];
}

export interface SearchFacet {
  name: string;
  values: SearchFacetValue[];
}

export interface SearchFacetValue {
  value: string;
  count: number;
}

export interface SearchSuggestionResponse {
  suggestions: SearchSuggestionItem[];
}

export interface SearchSuggestionItem {
  text: string;
  productId: number;
  slug?: string | null;
}

export interface SearchIndexStatus {
  totalEntries: number;
  pendingJobs: number;
  failedJobs: number;
  lastIndexedAtUtc?: string | null;
}
