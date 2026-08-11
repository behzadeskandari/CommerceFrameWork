import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CategorySummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { StorefrontCatalogFacade } from '../services/storefront-catalog.facade';

@Component({
  standalone: true,
  imports: [RouterLink, TranslatePipe, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'catalog.categories.title' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <ul class="list">
          @for (category of categories; track category.id) {
            <li><a [routerLink]="['/category', category.slug || category.id]">{{ category.name }}</a></li>
          }
        </ul>
      }
    }
  `,
  styles: [`
    .list { list-style: none; padding: 0; display: grid; gap: 0.5rem; }
    a { text-decoration: none; color: var(--primary, #0f766e); }
  `]
})
export class CategoriesPageComponent implements OnInit {
  private readonly catalog = inject(StorefrontCatalogFacade);
  state: PageState = 'loading';
  errorMessage = '';
  categories: CategorySummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    try {
      this.categories = await this.catalog.listPublishedCategories();
      this.state = this.categories.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load categories.';
      this.state = 'error';
    }
  }
}
