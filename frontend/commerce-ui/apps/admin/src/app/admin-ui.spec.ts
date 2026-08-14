import { applyAdminList, createAdminListState, exportCsv } from '@commerce/ui';

describe('admin-list.util', () => {
  it('filters and paginates items', () => {
    const state = createAdminListState<{ id: number; name: string }>(2);
    state.allItems = [
      { id: 1, name: 'Alpha' },
      { id: 2, name: 'Beta' },
      { id: 3, name: 'Gamma' }
    ];

    const filtered = applyAdminList(state, {
      search: 'a',
      searchFields: [(item: { id: number; name: string }) => item.name],
      sortKey: 'name',
      sortDirection: 'asc',
      sortAccessor: (item: { id: number; name: string }, key: string) => {
        const record = item as unknown as Record<string, string | number>;
        return String(record[key] ?? '').toLowerCase();
      }
    });

    expect(filtered.filtered.length).toBe(3);
    expect(filtered.pageItems.length).toBe(2);
  });

  it('exports csv without throwing', () => {
    const anchor = document.createElement('a');
    spyOn(document, 'createElement').and.returnValue(anchor);
    spyOn(anchor, 'click');

    exportCsv('test.csv', ['Name'], [['Alpha']]);
    expect(anchor.click).toHaveBeenCalled();
  });
});

describe('Admin UI smoke', () => {
  it('loads shared admin utilities', () => {
    expect(typeof exportCsv).toBe('function');
    expect(typeof applyAdminList).toBe('function');
  });
});
