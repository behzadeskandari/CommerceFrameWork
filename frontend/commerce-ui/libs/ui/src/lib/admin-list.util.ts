export interface AdminListState<T> {
  allItems: T[];
  filtered: T[];
  pageItems: T[];
  page: number;
  pageSize: number;
  totalPages: number;
  search: string;
  sortKey: string;
  sortDirection: 'asc' | 'desc';
}

export function createAdminListState<T>(pageSize = 10): AdminListState<T> {
  return {
    allItems: [],
    filtered: [],
    pageItems: [],
    page: 1,
    pageSize,
    totalPages: 1,
    search: '',
    sortKey: '',
    sortDirection: 'asc'
  };
}

export function applyAdminList<T>(
  state: AdminListState<T>,
  options: {
    search?: string;
    searchFields?: Array<(item: T) => string>;
    sortKey?: string;
    sortDirection?: 'asc' | 'desc';
    sortAccessor?: (item: T, key: string) => string | number;
    page?: number;
    pageSize?: number;
  }
): AdminListState<T> {
  const search = options.search ?? state.search;
  const sortKey = options.sortKey ?? state.sortKey;
  const sortDirection = options.sortDirection ?? state.sortDirection;
  const pageSize = options.pageSize ?? state.pageSize;
  const term = search.trim().toLowerCase();

  let filtered = [...state.allItems];
  if (term && options.searchFields?.length) {
    filtered = filtered.filter(item =>
      options.searchFields!.some(field => field(item).toLowerCase().includes(term))
    );
  }

  if (sortKey && options.sortAccessor) {
    filtered.sort((left, right) => {
      const a = options.sortAccessor!(left, sortKey);
      const b = options.sortAccessor!(right, sortKey);
      if (a === b) return 0;
      const direction = sortDirection === 'asc' ? 1 : -1;
      return a > b ? direction : -direction;
    });
  }

  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const page = Math.min(Math.max(1, options.page ?? state.page), totalPages);
  const start = (page - 1) * pageSize;

  return {
    ...state,
    search,
    sortKey,
    sortDirection,
    pageSize,
    filtered,
    page,
    totalPages,
    pageItems: filtered.slice(start, start + pageSize)
  };
}

export function exportCsv(filename: string, headers: string[], rows: string[][]): void {
  const escape = (value: string) => `"${value.replace(/"/g, '""')}"`;
  const content = [headers.map(escape).join(','), ...rows.map(row => row.map(escape).join(','))].join('\n');
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}
