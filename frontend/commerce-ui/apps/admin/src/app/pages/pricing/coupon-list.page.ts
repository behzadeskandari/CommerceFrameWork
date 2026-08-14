import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CouponSummary, DiscountsApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  ConfirmDialogComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    BreadcrumbsComponent,
    TranslatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ConfirmDialogComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('pricing.coupons.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'pricing.coupons.title' | translate }}</h1>
      @if (permissions.hasPermission('Coupons.Manage')) {
        <a routerLink="/pricing/coupons/new" class="btn">{{ 'action.create' | translate }}</a>
      }
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>{{ 'pricing.coupons.code' | translate }}</th>
              <th>{{ 'pricing.coupons.discount' | translate }}</th>
              <th>{{ 'pricing.coupons.usageCount' | translate }}</th>
              <th>{{ 'pricing.coupons.limits' | translate }}</th>
              <th>{{ 'pricing.discounts.active' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td><code>{{ item.code }}</code></td>
                <td>{{ item.discountName }}</td>
                <td>{{ item.usageCount }}</td>
                <td>
                  @if (item.globalUsageLimit != null || item.perCustomerUsageLimit != null) {
                    {{ item.globalUsageLimit ?? '∞' }} / {{ item.perCustomerUsageLimit ?? '∞' }}
                  } @else {
                    —
                  }
                </td>
                <td>{{ item.isActive ? ('pricing.active' | translate) : ('pricing.inactive' | translate) }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Coupons.Manage')) {
                    <a [routerLink]="['/pricing/coupons', item.id]">{{ 'action.edit' | translate }}</a>
                    <button type="button" class="danger" (click)="confirmDelete(item)">{{ 'action.delete' | translate }}</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    }

    <cmr-confirm-dialog
      [open]="deleteTarget !== null"
      [title]="'pricing.coupons.deleteTitle' | translate"
      [message]="('pricing.coupons.deleteMessage' | translate) + ' ' + (deleteTarget?.code ?? '')"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null" />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .btn { padding: 0.5rem 1rem; background: #2563eb; color: #fff; text-decoration: none; border-radius: 0.375rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    code { background: #f3f4f6; padding: 0.125rem 0.375rem; border-radius: 0.25rem; }
    .actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .actions button { background: none; border: none; color: #dc2626; cursor: pointer; text-decoration: underline; font-size: 0.875rem; }
  `]
})
export class CouponListPageComponent implements OnInit {
  private readonly discountsApi = inject(DiscountsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: CouponSummary[] = [];
  deleteTarget: CouponSummary | null = null;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      this.items = await firstValueFrom(this.discountsApi.listCoupons());
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load coupons.';
      this.state = 'error';
    }
  }

  confirmDelete(item: CouponSummary): void {
    this.deleteTarget = item;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.discountsApi.deleteCoupon(this.deleteTarget.id));
      this.deleteTarget = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';
      this.state = 'error';
    }
  }
}
