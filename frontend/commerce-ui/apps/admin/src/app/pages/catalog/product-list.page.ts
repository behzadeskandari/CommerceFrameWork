import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CatalogApi, ProductSummary } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
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
import { firstValueFrom } from 'rxjs';
import { CatalogAdminFacade } from '../../services/catalog-admin.facade';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    TranslatePipe,
    BreadcrumbsComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent,
    ConfirmDialogComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('catalog.products.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'catalog.products.title' | translate }}</h1>
      @if (permissions.hasPermission('Catalog.Products.Create')) {
        <a routerLink="/catalog/products/new" class="btn btn--primary">Create product</a>
      }
    </header>
    <label class="search">
      Search
      <input type="search" [(ngModel)]="search" (ngModelChange)="applyFilter()" aria-label="Search products" />
    </label>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state messageKey="state.empty" /> }
      @default {
        <table>
          <thead>
            <tr><th>Name</th><th>SKU</th><th>Type</th><th>Status</th><th>Visibility</th><th>Actions</th></tr>
          </thead>
          <tbody>
            @for (product of pageItems; track product.id) {
              <tr>
                <td>{{ product.name }}</td>
                <td>{{ product.sku }}</td>
                <td>{{ product.productType }}</td>
                <td>{{ product.published ? 'Published' : 'Draft' }}</td>
                <td>{{ product.isVisible ? 'Visible' : 'Hidden' }}{{ product.isAvailable ? '' : ' · Unavailable' }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Catalog.Products.Update')) {
                    <a [routerLink]="['/catalog/products', product.id]">Edit</a>
                  }
                  @if (permissions.hasPermission('Catalog.Products.Delete')) {
                    <button type="button" (click)="confirmDelete(product)">Delete</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
        <cmr-pagination [page]="page" [totalPages]="totalPages" (pageChange)="setPage($event)" />
      }
    }
    <cmr-confirm-dialog
      [open]="deleteTarget !== null"
      title="Delete product"
      [message]="'Delete ' + (deleteTarget?.name ?? '') + '?'"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null" />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; text-decoration: none; }
    .btn--primary { background: #2563eb; color: #fff; }
    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 0.5rem; overflow: hidden; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .actions { display: flex; gap: 0.5rem; }
    .search { display: grid; gap: 0.375rem; margin: 1rem 0; max-width: 20rem; }
    input { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
  `]
})
export class ProductListPageComponent implements OnInit {
  private readonly facade = inject(CatalogAdminFacade);
  private readonly catalogApi = inject(CatalogApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  allProducts: ProductSummary[] = [];
  filtered: ProductSummary[] = [];
  pageItems: ProductSummary[] = [];
  search = '';
  page = 1;
  pageSize = 10;
  totalPages = 1;
  deleteTarget: ProductSummary | null = null;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.allProducts = await this.facade.listProducts();
      this.applyFilter();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load products.';
      this.state = 'error';
    }
  }

  applyFilter(): void {
    const term = this.search.trim().toLowerCase();
    this.filtered = this.allProducts.filter(product =>
      !term || product.name.toLowerCase().includes(term) || product.sku.toLowerCase().includes(term)
    );
    this.setPage(1);
  }

  setPage(page: number): void {
    this.page = page;
    this.totalPages = Math.max(1, Math.ceil(this.filtered.length / this.pageSize));
    const start = (this.page - 1) * this.pageSize;
    this.pageItems = this.filtered.slice(start, start + this.pageSize);
    this.state = this.filtered.length ? 'success' : 'empty';
  }

  confirmDelete(product: ProductSummary): void {
    this.deleteTarget = product;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.catalogApi.deleteProduct(this.deleteTarget.id));
      this.deleteTarget = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';
      this.state = 'error';
    }
  }
}
