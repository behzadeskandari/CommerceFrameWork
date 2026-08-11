import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductSummary } from '@commerce/api';
import { TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { ApiClientError } from '@commerce/core';
import { StorefrontCatalogFacade } from '../services/storefront-catalog.facade';

@Component({
  standalone: true,
  imports: [RouterLink, TranslatePipe, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <section class="hero">
      <h1>{{ 'app.title' | translate }}</h1>
      <p>Discover products from the Commerce catalog.</p>
      <a routerLink="/products" class="btn">Shop now</a>
    </section>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <section class="grid">
          @for (product of featured; track product.id) {
            <article class="card">
              <h2><a [routerLink]="['/product', product.slug || product.id]">{{ product.name }}</a></h2>
              <p>{{ product.sku }}</p>
            </article>
          }
        </section>
      }
    }
  `,
  styles: [`
    .hero { padding: 2rem 0; }
    .btn { display: inline-block; padding: 0.75rem 1rem; background: var(--primary, #0f766e); color: #fff; text-decoration: none; border-radius: 0.375rem; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 1rem; }
    .card { padding: 1rem; border: 1px solid #e5e7eb; border-radius: 0.5rem; background: #fff; }
    a { color: inherit; text-decoration: none; }
  `]
})
export class HomePageComponent implements OnInit {
  private readonly catalog = inject(StorefrontCatalogFacade);
  state: PageState = 'loading';
  errorMessage = '';
  featured: ProductSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.featured = (await this.catalog.listPublishedProducts()).slice(0, 6);
      this.state = this.featured.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load products.';
      this.state = 'error';
    }
  }
}
