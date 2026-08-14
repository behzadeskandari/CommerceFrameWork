import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationTemplateSummary, NotificationsApi } from '@commerce/api';
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
      { label: ('notifications.templates.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'notifications.templates.title' | translate }}</h1>
      <div class="header-actions">
        <a routerLink="/notifications/logs" class="btn-secondary">{{ 'notifications.logs.title' | translate }}</a>
        @if (permissions.hasPermission('Notifications.Manage')) {
          <a routerLink="/notifications/templates/new" class="btn">{{ 'action.create' | translate }}</a>
        }
      </div>
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>{{ 'notifications.templates.systemName' | translate }}</th>
              <th>{{ 'notifications.templates.eventType' | translate }}</th>
              <th>{{ 'notifications.templates.channel' | translate }}</th>
              <th>{{ 'notifications.templates.storeId' | translate }}</th>
              <th>{{ 'notifications.templates.languageId' | translate }}</th>
              <th>{{ 'notifications.templates.enabled' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.systemName }}</td>
                <td>{{ item.eventType }}</td>
                <td>{{ item.channel }}</td>
                <td>{{ item.storeId ?? '—' }}</td>
                <td>{{ item.languageId ?? '—' }}</td>
                <td>{{ item.isEnabled ? ('pricing.active' | translate) : ('pricing.inactive' | translate) }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Notifications.Manage')) {
                    <a [routerLink]="['/notifications/templates', item.id]">{{ 'action.edit' | translate }}</a>
                    @if (item.isEnabled) {
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
      [title]="'notifications.templates.deleteTitle' | translate"
      [message]="deleteTarget?.systemName ?? ''"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null"
    />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .header-actions { display: flex; gap: 0.75rem; align-items: center; }
    .btn, .btn-secondary { padding: 0.5rem 0.875rem; border-radius: 0.375rem; text-decoration: none; }
    .btn { background: #111827; color: #fff; }
    .btn-secondary { border: 1px solid #d1d5db; color: #111827; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 0.625rem; border-bottom: 1px solid #e5e7eb; }
    .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
  `]
})
export class NotificationTemplateListPageComponent implements OnInit {
  private readonly api = inject(NotificationsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: NotificationTemplateSummary[] = [];
  deleteTarget: NotificationTemplateSummary | null = null;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.items = await firstValueFrom(this.api.listTemplates());
      this.state = this.items.length === 0 ? 'empty' : 'ready';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load templates.';
      this.state = 'error';
    }
  }

  async activate(item: NotificationTemplateSummary): Promise<void> {
    await firstValueFrom(this.api.activateTemplate(item.id));
    await this.load();
  }

  async deactivate(item: NotificationTemplateSummary): Promise<void> {
    await firstValueFrom(this.api.deactivateTemplate(item.id));
    await this.load();
  }

  confirmDelete(item: NotificationTemplateSummary): void {
    this.deleteTarget = item;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) {
      return;
    }
    await firstValueFrom(this.api.deleteTemplate(this.deleteTarget.id));
    this.deleteTarget = null;
    await this.load();
  }
}
