import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PromotionSummary, PromotionsApi } from '@commerce/api';
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
      { label: ('promotions.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'promotions.title' | translate }}</h1>
      @if (permissions.hasPermission('Promotions.Manage')) {
        <a routerLink="/marketing/promotions/new" class="btn">{{ 'action.create' | translate }}</a>
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
              <th>{{ 'promotions.name' | translate }}</th>
              <th>{{ 'promotions.priority' | translate }}</th>
              <th>{{ 'promotions.combinationRule' | translate }}</th>
              <th>{{ 'promotions.usage' | translate }}</th>
              <th>{{ 'promotions.active' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.name }}</td>
                <td>{{ item.priority }}</td>
                <td>{{ item.combinationRule }}</td>
                <td>{{ item.usageCount }}@if (item.globalUsageLimit) { / {{ item.globalUsageLimit }} }</td>
                <td>{{ item.isActive ? ('pricing.active' | translate) : ('pricing.inactive' | translate) }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Promotions.Manage')) {
                    <a [routerLink]="['/marketing/promotions', item.id]">{{ 'action.edit' | translate }}</a>
                    @if (item.isActive) {
                      <button type="button" (click)="deactivate(item)">{{ 'pricing.deactivate' | translate }}</button>
                    } @else {
                      <button type="button" (click)="activate(item)">{{ 'pricing.activate' | translate }}</button>
                    }
                    <button type="button" (click)="confirmDelete(item)">{{ 'action.delete' | translate }}</button>
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
      [title]="'promotions.deleteTitle' | translate"
      [message]="deleteTarget?.name ?? ''"
      (confirmed)="deleteConfirmed()"
      (cancelled)="deleteTarget = null" />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; }
    th, td { padding: 0.625rem; border-bottom: 1px solid #e5e7eb; text-align: left; }
    .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
    .btn { padding: 0.5rem 0.875rem; background: #1d4ed8; color: #fff; text-decoration: none; border-radius: 0.375rem; }
  `]
})
export class PromotionListPageComponent implements OnInit {
  private readonly api = inject(PromotionsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: PromotionSummary[] = [];
  deleteTarget: PromotionSummary | null = null;

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.items = await firstValueFrom(this.api.list());
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load promotions.';
      this.state = 'error';
    }
  }

  async activate(item: PromotionSummary): Promise<void> {
    await firstValueFrom(this.api.activate(item.id));
    await this.load();
  }

  async deactivate(item: PromotionSummary): Promise<void> {
    await firstValueFrom(this.api.deactivate(item.id));
    await this.load();
  }

  confirmDelete(item: PromotionSummary): void { this.deleteTarget = item; }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    await firstValueFrom(this.api.delete(this.deleteTarget.id));
    this.deleteTarget = null;
    await this.load();
  }
}
