import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductSummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { StorefrontCatalogFacade } from '../services/storefront-catalog.facade';

@Component({
  standalone: true,
  imports: [RouterLink, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <h1>Products</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <div class="grid">
          @for (product of products; track product.id) {
            <article class="card">
              <h2><a [routerLink]="['/product', product.slug || product.id]">{{ product.name }}</a></h2>
              <p>{{ product.sku }}</p>
              <p class="meta">{{ product.productType }}</p>
            </article>
          }
        </div>
      }
    }
  `,
  styles: [`
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 1rem; }
    .card { padding: 1rem; border: 1px solid #e5e7eb; border-radius: 0.5rem; background: #fff; }
    .meta { color: #6b7280; font-size: 0.875rem; }
    a { color: inherit; text-decoration: none; }
  `]
})
export class ProductsPageComponent implements OnInit {
  private readonly catalog = inject(StorefrontCatalogFacade);
  state: PageState = 'loading';
  errorMessage = '';
  products: ProductSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.products = await this.catalog.listPublishedProducts();
      this.state = this.products.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load products.';
      this.state = 'error';
    }
  }
}
