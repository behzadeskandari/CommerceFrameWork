import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogApi, ProductSummary } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  ConfirmDialogComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState,
  PaginationComponent
} from '@commerce/shared';
import {
  AdminContextService,
  AdminDataTableComponent,
  AdminPageShellComponent,
  AdminTableColumn,
  BulkActionBarComponent,
  FilterBarComponent,
  ToastService,
  applyAdminList,
  createAdminListState,
  exportCsv,
  resolveAdminError
} from '@commerce/ui';
import { firstValueFrom } from 'rxjs';
import { CatalogAdminFacade } from '../../services/catalog-admin.facade';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    TranslatePipe,
    BreadcrumbsComponent,
    AdminPageShellComponent,
    FilterBarComponent,
    AdminDataTableComponent,
    BulkActionBarComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent,
    ConfirmDialogComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: ('nav.dashboard' | translate), link: '/dashboard' },
      { label: ('catalog.products.title' | translate) }
    ]" />

    <cmr-admin-page-shell [title]="'catalog.products.title' | translate">
      @if (canCreate) {
        <a actions routerLink="/catalog/products/new" class="btn btn--primary">
          {{ 'catalog.products.create' | translate }}
        </a>
      }
      @if (listState.allItems.length) {
        <button actions type="button" class="btn btn--secondary" (click)="exportAll()">
          {{ 'catalog.products.export' | translate }}
        </button>
      }

      <div toolbar>
        <cmr-filter-bar
          [search]="listState.search"
          (searchChange)="onSearch($event)"
          (reset)="resetFilters()" />
      </div>

      <cmr-bulk-action-bar [selectedCount]="selectedIds.size">
        @if (canDelete) {
          <button type="button" class="btn btn--danger" (click)="confirmBulkDelete()">
            {{ 'admin.bulk.delete' | translate }}
          </button>
        }
        <button type="button" class="btn btn--secondary" (click)="exportSelected()">
          {{ 'admin.bulk.export' | translate }}
        </button>
      </cmr-bulk-action-bar>

      @switch (state) {
        @case ('loading') { <cmr-loading-state /> }
        @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
        @case ('empty') { <cmr-empty-state messageKey="state.empty" /> }
        @default {
          <cmr-admin-data-table
            [columns]="columns"
            [rows]="listState.pageItems"
            [sortKey]="listState.sortKey"
            [sortDirection]="listState.sortDirection"
            [selectable]="true"
            [allSelected]="allSelected"
            [someSelected]="someSelected"
            [trackBy]="trackProduct"
            [isSelected]="isSelected"
            (sortChange)="onSort($event)"
            (toggleAll)="toggleAll($event)"
            (toggleRow)="toggleRow($event.row, $event.selected)">
            <ng-template #cell let-row let-column="column">
              @switch (column.key) {
                @case ('name') { {{ row.name }} }
                @case ('sku') { {{ row.sku }} }
                @case ('productType') { {{ row.productType }} }
                @case ('status') { {{ row.published ? ('common.published' | translate) : ('common.draft' | translate) }} }
                @case ('visibility') {
                  {{ row.isVisible ? ('common.visible' | translate) : ('common.hidden' | translate) }}
                  {{ row.isAvailable ? '' : ' · ' + ('inventory.outOfStock' | translate) }}
                }
              }
            </ng-template>
            <ng-template #actions let-row>
              @if (canUpdate) {
                <a [routerLink]="['/catalog/products', row.id]">{{ 'action.edit' | translate }}</a>
              }
              @if (canDelete) {
                <button type="button" (click)="confirmDelete(row)">{{ 'action.delete' | translate }}</button>
              }
            </ng-template>
          </cmr-admin-data-table>

          <cmr-pagination
            [page]="listState.page"
            [totalPages]="listState.totalPages"
            [pageSize]="listState.pageSize"
            [totalItems]="listState.filtered.length"
            (pageChange)="setPage($event)"
            (pageSizeChange)="setPageSize($event)" />
        }
      }
    </cmr-admin-page-shell>

    <cmr-confirm-dialog
      [open]="deleteTarget !== null"
      [title]="'catalog.products.deleteTitle' | translate"
      [message]="deleteTarget ? (('catalog.products.deleteMessage' | translate) + ' ' + deleteTarget.name) : ''"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null" />

    <cmr-confirm-dialog
      [open]="bulkDeleteOpen"
      [title]="'admin.bulk.delete' | translate"
      [message]="selectedIds.size + ' ' + ('admin.bulk.itemsSelected' | translate)"
      (confirm)="bulkDeleteConfirmed()"
      (cancel)="bulkDeleteOpen = false" />
  `,
  styles: [`
    a, button { margin-inline-end: 0.5rem; }
  `]
})
export class ProductListPageComponent implements OnInit {
  private readonly facade = inject(CatalogAdminFacade);
  private readonly catalogApi = inject(CatalogApi);
  private readonly toast = inject(ToastService);
  readonly permissions = inject(PermissionService);
  readonly adminContext = inject(AdminContextService);

  state: PageState = 'loading';
  errorMessage = '';
  listState = createAdminListState<ProductSummary>();
  selectedIds = new Set<number>();
  deleteTarget: ProductSummary | null = null;
  bulkDeleteOpen = false;

  readonly columns: AdminTableColumn<ProductSummary>[] = [
    { key: 'name', labelKey: 'catalog.products.name', sortable: true },
    { key: 'sku', labelKey: 'catalog.products.sku', sortable: true },
    { key: 'productType', labelKey: 'catalog.products.type', sortable: true },
    { key: 'status', labelKey: 'common.status', sortable: true },
    { key: 'visibility', labelKey: 'catalog.products.visibility' }
  ];

  get canCreate(): boolean {
    return this.permissions.hasPermission('Catalog.Products.Create');
  }

  get canUpdate(): boolean {
    return this.permissions.hasPermission('Catalog.Products.Update');
  }

  get canDelete(): boolean {
    return this.permissions.hasPermission('Catalog.Products.Delete');
  }

  get allSelected(): boolean {
    return this.listState.pageItems.length > 0 && this.listState.pageItems.every(item => this.selectedIds.has(item.id));
  }

  get someSelected(): boolean {
    return this.listState.pageItems.some(item => this.selectedIds.has(item.id));
  }

  ngOnInit(): void {
    void this.load();
  }

  trackProduct = (row: ProductSummary) => row.id;
  isSelected = (row: ProductSummary) => this.selectedIds.has(row.id);

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.listState.allItems = await this.facade.listProducts();
      this.refreshList();
    } catch (error) {
      this.errorMessage = resolveAdminError(error, 'Failed to load products.');
      this.state = 'error';
    }
  }

  onSearch(term: string): void {
    this.listState = applyAdminList(this.listState, {
      search: term,
      searchFields: [item => item.name, item => item.sku],
      page: 1,
      sortKey: this.listState.sortKey,
      sortDirection: this.listState.sortDirection,
      sortAccessor: (item, key) => String((item as unknown as Record<string, unknown>)[key] ?? '')
    });
    this.state = this.listState.filtered.length ? 'success' : 'empty';
  }

  onSort(key: string): void {
    const sortDirection = this.listState.sortKey === key && this.listState.sortDirection === 'asc' ? 'desc' : 'asc';
    this.listState = applyAdminList(this.listState, {
      sortKey: key,
      sortDirection,
      sortAccessor: (item, sortKey) => String((item as unknown as Record<string, unknown>)[sortKey] ?? '').toLowerCase()
    });
  }

  setPage(page: number): void {
    this.listState = applyAdminList(this.listState, { page });
  }

  setPageSize(pageSize: number): void {
    this.listState = applyAdminList(this.listState, { pageSize, page: 1 });
  }

  resetFilters(): void {
    this.listState = applyAdminList(this.listState, { search: '', page: 1 });
    this.state = this.listState.filtered.length ? 'success' : 'empty';
  }

  toggleAll(selected: boolean): void {
    for (const item of this.listState.pageItems) {
      if (selected) this.selectedIds.add(item.id);
      else this.selectedIds.delete(item.id);
    }
  }

  toggleRow(row: ProductSummary, selected: boolean): void {
    if (selected) this.selectedIds.add(row.id);
    else this.selectedIds.delete(row.id);
  }

  confirmDelete(product: ProductSummary): void {
    this.deleteTarget = product;
  }

  confirmBulkDelete(): void {
    this.bulkDeleteOpen = true;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.catalogApi.deleteProduct(this.deleteTarget.id));
      this.deleteTarget = null;
      this.toast.success('Deleted.');
      await this.load();
    } catch (error) {
      this.toast.error(resolveAdminError(error, 'Delete failed.'));
    }
  }

  async bulkDeleteConfirmed(): Promise<void> {
    this.bulkDeleteOpen = false;
    const ids = [...this.selectedIds];
    try {
      for (const id of ids) {
        await firstValueFrom(this.catalogApi.deleteProduct(id));
      }
      this.selectedIds.clear();
      this.toast.success('Deleted.');
      await this.load();
    } catch (error) {
      this.toast.error(resolveAdminError(error, 'Bulk delete failed.'));
    }
  }

  exportAll(): void {
    this.exportRows(this.listState.filtered);
  }

  exportSelected(): void {
    const rows = this.listState.allItems.filter(item => this.selectedIds.has(item.id));
    this.exportRows(rows);
  }

  private exportRows(rows: ProductSummary[]): void {
    exportCsv(
      'products.csv',
      ['Name', 'SKU', 'Type', 'Published', 'Visible', 'Available'],
      rows.map(row => [
        row.name,
        row.sku,
        row.productType,
        String(row.published),
        String(row.isVisible),
        String(row.isAvailable)
      ])
    );
    this.toast.success('Export completed.');
  }

  private refreshList(): void {
    this.listState = applyAdminList(this.listState, {
      search: this.listState.search,
      searchFields: [item => item.name, item => item.sku],
      sortKey: this.listState.sortKey || 'name',
      sortDirection: this.listState.sortDirection,
      sortAccessor: (item, key) => String((item as unknown as Record<string, unknown>)[key] ?? '').toLowerCase()
    });
    this.state = this.listState.filtered.length ? 'success' : 'empty';
  }
}
