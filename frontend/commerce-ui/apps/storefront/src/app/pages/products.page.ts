import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SearchApi, ProductSearchResultItem } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { debounceTime, distinctUntilChanged, Subject, switchMap, firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'catalog.products.title' | translate }}</h1>
    <form class="search-bar" (ngSubmit)="search()">
      <label>
        {{ 'action.search' | translate }}
        <input type="search" [(ngModel)]="term" name="term" (ngModelChange)="onTermChange($event)" [attr.list]="'suggestions'" />
      </label>
      <datalist id="suggestions">
        @for (item of suggestions; track item.productId) {
          <option [value]="item.text"></option>
        }
      </datalist>
      <label>
        Sort
        <select [(ngModel)]="sortField" name="sortField" (ngModelChange)="search()">
          <option value="Relevance">Relevance</option>
          <option value="Price">Price</option>
          <option value="Newest">Newest</option>
        </select>
      </label>
      <button type="submit">{{ 'action.search' | translate }}</button>
    </form>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="search()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <p class="meta">{{ totalCount }} results</p>
        <div class="grid">
          @for (product of products; track product.productId) {
            <article class="card">
              <h2><a [routerLink]="['/product', product.slug || product.productId]">{{ product.name }}</a></h2>
              <p>{{ product.sku }}</p>
              @if (product.price != null) { <p class="price">{{ product.price }}</p> }
              <p class="meta">{{ product.productType }}</p>
            </article>
          }
        </div>
      }
    }
  `,
  styles: [`
    .search-bar { display: flex; flex-wrap: wrap; gap: 0.75rem; margin-bottom: 1rem; align-items: end; }
    .search-bar label { display: grid; gap: 0.25rem; }
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 1rem; }
    .card { padding: 1rem; border: 1px solid #e5e7eb; border-radius: 0.5rem; background: #fff; }
    .meta { color: #6b7280; font-size: 0.875rem; }
    .price { font-weight: 600; color: var(--primary, #0f766e); }
    a { color: inherit; text-decoration: none; }
  `]
})
export class ProductsPageComponent implements OnInit {
  private readonly searchApi = inject(SearchApi);
  private readonly termChanges = new Subject<string>();
  state: PageState = 'loading';
  errorMessage = '';
  products: ProductSearchResultItem[] = [];
  suggestions: { text: string; productId: number }[] = [];
  term = '';
  sortField: 'Relevance' | 'Price' | 'Newest' = 'Relevance';
  totalCount = 0;

  ngOnInit(): void {
    this.termChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(term => this.searchApi.suggest(term))
    ).subscribe(result => {
      this.suggestions = result.suggestions;
    });
    void this.search();
  }

  onTermChange(value: string): void {
    if (value.trim().length >= 2) {
      this.termChanges.next(value.trim());
    } else {
      this.suggestions = [];
    }
  }

  async search(): Promise<void> {
    this.state = 'loading';
    try {
      const result = await firstValueFrom(this.searchApi.searchProducts({
        term: this.term || undefined,
        sortField: this.sortField,
        sortDirection: this.sortField === 'Price' ? 'Asc' : 'Desc'
      }));
      this.products = result.items;
      this.totalCount = result.totalCount;
      this.state = this.products.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Search failed.';
      this.state = 'error';
    }
  }
}
