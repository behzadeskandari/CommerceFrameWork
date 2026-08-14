import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DiscountSummary, DiscountsApi } from '@commerce/api';
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
      { label: ('pricing.discounts.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'pricing.discounts.title' | translate }}</h1>
      @if (permissions.hasPermission('Discounts.Create')) {
        <a routerLink="/pricing/discounts/new" class="btn">{{ 'action.create' | translate }}</a>
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
              <th>{{ 'pricing.discounts.name' | translate }}</th>
              <th>{{ 'pricing.discounts.type' | translate }}</th>
              <th>{{ 'pricing.discounts.value' | translate }}</th>
              <th>{{ 'pricing.discounts.priority' | translate }}</th>
              <th>{{ 'pricing.discounts.active' | translate }}</th>
              <th>{{ 'pricing.discounts.scope' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.name }}</td>
                <td>{{ discountTypeLabel(item.discountType) | translate }}</td>
                <td>
                  @if (item.discountType === 'Percentage') {
                    {{ item.value }}%
                  } @else {
                    {{ item.value }} {{ item.currencyCode ?? '' }}
                  }
                </td>
                <td>{{ item.priority }}</td>
                <td>{{ item.isActive ? ('pricing.active' | translate) : ('pricing.inactive' | translate) }}</td>
                <td>{{ scopeLabel(item.applicationScope) | translate }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Discounts.Update')) {
                    <a [routerLink]="['/pricing/discounts', item.id]">{{ 'action.edit' | translate }}</a>
                  }
                  @if (permissions.hasPermission('Discounts.Manage')) {
                    @if (item.isActive) {
                      <button type="button" (click)="deactivate(item)">{{ 'pricing.deactivate' | translate }}</button>
                    } @else {
                      <button type="button" (click)="activate(item)">{{ 'pricing.activate' | translate }}</button>
                    }
                  }
                  @if (permissions.hasPermission('Discounts.Delete')) {
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
      [title]="'pricing.discounts.deleteTitle' | translate"
      [message]="('pricing.discounts.deleteMessage' | translate) + ' ' + (deleteTarget?.name ?? '')"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null" />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .btn { padding: 0.5rem 1rem; background: #2563eb; color: #fff; text-decoration: none; border-radius: 0.375rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .actions button, .actions a { font-size: 0.875rem; }
    .actions button { background: none; border: none; color: #2563eb; cursor: pointer; text-decoration: underline; }
    .actions button.danger { color: #dc2626; }
  `]
})
export class DiscountListPageComponent implements OnInit {
  private readonly discountsApi = inject(DiscountsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: DiscountSummary[] = [];
  deleteTarget: DiscountSummary | null = null;

  ngOnInit(): void {
    void this.load();
  }

  discountTypeLabel(type: DiscountSummary['discountType']): string {
    return type === 'Percentage' ? 'pricing.discounts.percentage' : 'pricing.discounts.fixedAmount';
  }

  scopeLabel(scope: DiscountSummary['applicationScope']): string {
    return scope === 'Line' ? 'pricing.discounts.scopeLine' : 'pricing.discounts.scopeCart';
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      this.items = await firstValueFrom(this.discountsApi.listDiscounts());
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load discounts.';
      this.state = 'error';
    }
  }

  async activate(item: DiscountSummary): Promise<void> {
    try {
      await firstValueFrom(this.discountsApi.activateDiscount(item.id));
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to activate discount.';
      this.state = 'error';
    }
  }

  async deactivate(item: DiscountSummary): Promise<void> {
    try {
      await firstValueFrom(this.discountsApi.deactivateDiscount(item.id));
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to deactivate discount.';
      this.state = 'error';
    }
  }

  confirmDelete(item: DiscountSummary): void {
    this.deleteTarget = item;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.discountsApi.deleteDiscount(this.deleteTarget.id));
      this.deleteTarget = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';
      this.state = 'error';
    }
  }
}
