import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminWishlistSummary, ReviewsApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    BreadcrumbsComponent,
    TranslatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('reviews.title' | translate), link: '/reviews' },
      { label: ('reviews.wishlists' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'reviews.wishlists' | translate }}</h1>
      <a routerLink="/reviews">{{ 'reviews.title' | translate }}</a>
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @case ('success') {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{{ 'reviews.customer' | translate }}</th>
                <th>{{ 'reviews.store' | translate }}</th>
                <th>{{ 'reviews.itemCount' | translate }}</th>
                <th>{{ 'reviews.lastAdded' | translate }}</th>
              </tr>
            </thead>
            <tbody>
              @for (item of items; track item.id) {
                <tr>
                  <td>{{ item.customerDisplayName || ('#' + item.customerId) }}</td>
                  <td>#{{ item.storeId }}</td>
                  <td>{{ item.itemCount }}</td>
                  <td>{{ item.lastAddedAtUtc ? (item.lastAddedAtUtc | date:'medium') : '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .table-wrap { overflow-x: auto; background: #fff; border-radius: 0.5rem; border: 1px solid #e5e7eb; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.75rem; text-align: left; border-bottom: 1px solid #e5e7eb; }
  `]
})
export class WishlistListPageComponent implements OnInit {
  private readonly reviewsApi = inject(ReviewsApi);

  state: PageState = 'loading';
  errorMessage = '';
  items: AdminWishlistSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const result = await firstValueFrom(this.reviewsApi.listAdminWishlists({ pageSize: 100 }));
      this.items = result.items;
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load wishlists.';
      this.state = 'error';
    }
  }
}
