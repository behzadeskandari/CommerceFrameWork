import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminProductReview, ReviewsApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
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
      { label: ('reviews.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'reviews.title' | translate }}</h1>
      <a routerLink="/reviews/wishlists">{{ 'reviews.wishlists' | translate }}</a>
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
                <th>{{ 'reviews.product' | translate }}</th>
                <th>{{ 'reviews.customer' | translate }}</th>
                <th>{{ 'reviews.rating' | translate }}</th>
                <th>{{ 'reviews.titleField' | translate }}</th>
                <th>{{ 'reviews.status' | translate }}</th>
                <th>{{ 'reviews.verified' | translate }}</th>
                <th>{{ 'reviews.created' | translate }}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (item of items; track item.id) {
                <tr>
                  <td>{{ item.productName || ('#' + item.productId) }}</td>
                  <td>{{ item.customerDisplayName || ('#' + item.customerId) }}</td>
                  <td>{{ item.rating }}/5</td>
                  <td>{{ item.title }}</td>
                  <td><span class="badge" [class]="badgeClass(item)">{{ item.moderationStatus }}</span></td>
                  <td>{{ item.isVerifiedPurchase ? ('tax.yes' | translate) : ('tax.no' | translate) }}</td>
                  <td>{{ item.createdAtUtc | date:'medium' }}</td>
                  <td class="actions">
                    @if (permissions.hasPermission('Reviews.Manage')) {
                      @if (item.moderationStatus === 'Pending') {
                        <button type="button" (click)="approve(item)">{{ 'reviews.approve' | translate }}</button>
                        <button type="button" class="danger" (click)="reject(item)">{{ 'reviews.reject' | translate }}</button>
                      }
                      <button type="button" class="danger" (click)="remove(item)">{{ 'action.delete' | translate }}</button>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
    @if (actionError) {
      <p class="action-error" role="alert">{{ actionError }}</p>
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    .table-wrap { overflow-x: auto; background: #fff; border-radius: 0.5rem; border: 1px solid #e5e7eb; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.75rem; text-align: left; border-bottom: 1px solid #e5e7eb; vertical-align: top; }
    .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    button { padding: 0.375rem 0.625rem; border: 1px solid #d1d5db; border-radius: 0.375rem; background: #fff; cursor: pointer; }
    button.danger { color: #b91c1c; border-color: #fecaca; }
    .badge { padding: 0.125rem 0.5rem; border-radius: 999px; font-size: 0.75rem; background: #f3f4f6; }
    .badge.approved { background: #d1fae5; color: #047857; }
    .badge.pending { background: #fef3c7; color: #b45309; }
    .badge.rejected { background: #fee2e2; color: #b91c1c; }
    .action-error { color: #b91c1c; }
  `]
})
export class ReviewListPageComponent implements OnInit {
  private readonly reviewsApi = inject(ReviewsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  actionError = '';
  items: AdminProductReview[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.actionError = '';
    try {
      const result = await firstValueFrom(this.reviewsApi.listAdminReviews({ pageSize: 100 }));
      this.items = result.items;
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load reviews.';
      this.state = 'error';
    }
  }

  badgeClass(item: AdminProductReview): string {
    return item.moderationStatus.toLowerCase();
  }

  async approve(item: AdminProductReview): Promise<void> {
    try {
      await firstValueFrom(this.reviewsApi.approveReview(item.id));
      await this.load();
    } catch (error) {
      this.actionError = error instanceof ApiClientError ? error.message : 'Approve failed.';
    }
  }

  async reject(item: AdminProductReview): Promise<void> {
    try {
      await firstValueFrom(this.reviewsApi.rejectReview(item.id));
      await this.load();
    } catch (error) {
      this.actionError = error instanceof ApiClientError ? error.message : 'Reject failed.';
    }
  }

  async remove(item: AdminProductReview): Promise<void> {
    if (!confirm('Delete this review?')) return;
    try {
      await firstValueFrom(this.reviewsApi.deleteReview(item.id));
      await this.load();
    } catch (error) {
      this.actionError = error instanceof ApiClientError ? error.message : 'Delete failed.';
    }
  }
}
