import { Component, OnInit, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CategorySummary, ProductSummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { StorefrontCatalogFacade } from '../services/storefront-catalog.facade';

@Component({
  standalone: true,
  imports: [RouterLink, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    @if (state === 'loading') { <cmr-loading-state /> }
    @else if (!category) { <cmr-empty-state messageKey="state.empty" /> }
    @else {
      <h1>{{ category.name }}</h1>
      @if (products.length) {
        <ul class="list">
          @for (product of products; track product.id) {
            <li><a [routerLink]="['/product', product.slug || product.id]">{{ product.name }}</a></li>
          }
        </ul>
      } @else {
        <cmr-empty-state />
      }
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `,
  styles: [`
    .list { list-style: none; padding: 0; display: grid; gap: 0.5rem; }
    a { color: var(--primary, #0f766e); text-decoration: none; }
  `]
})
export class CategoryDetailPageComponent implements OnInit {
  readonly slug = input.required<string>();
  private readonly catalog = inject(StorefrontCatalogFacade);

  state: PageState = 'loading';
  errorMessage = '';
  category: CategorySummary | null = null;
  products: ProductSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.category = await this.catalog.findCategoryBySlug(this.slug());
      if (!this.category && /^\d+$/.test(this.slug())) {
        const detail = await this.catalog.getCategoryDetail(Number(this.slug()));
        this.category = detail;
      }
      if (!this.category) {
        this.state = 'empty';
        return;
      }
      const detail = await this.catalog.getCategoryDetail(this.category.id);
      const allProducts = await this.catalog.listPublishedProducts();
      this.products = allProducts.filter(product => detail.productIds.includes(product.id));
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load category.';
      this.state = 'error';
    }
  }
}
