import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationLogSummary, NotificationsApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
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
      { label: ('notifications.templates.title' | translate), link: '/notifications/templates' },
      { label: ('notifications.logs.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'notifications.logs.title' | translate }}</h1>
      <a routerLink="/notifications/templates" class="btn-secondary">{{ 'notifications.templates.title' | translate }}</a>
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>{{ 'notifications.logs.eventType' | translate }}</th>
              <th>{{ 'notifications.templates.channel' | translate }}</th>
              <th>{{ 'notifications.logs.recipient' | translate }}</th>
              <th>{{ 'notifications.templates.subject' | translate }}</th>
              <th>{{ 'notifications.logs.status' | translate }}</th>
              <th>{{ 'notifications.logs.attempts' | translate }}</th>
              <th>{{ 'notifications.logs.createdAt' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.eventType }}</td>
                <td>{{ item.channel }}</td>
                <td>{{ item.recipient }}</td>
                <td>{{ item.subject }}</td>
                <td>{{ item.status }}</td>
                <td>{{ item.attemptCount }}</td>
                <td>{{ item.createdAtUtc | date: 'short' }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Notifications.Manage') && item.status !== 'Sent') {
                    <button type="button" (click)="retry(item)">{{ 'notifications.logs.retry' | translate }}</button>
                  }
                </td>
              </tr>
              @if (item.lastError) {
                <tr class="error-row">
                  <td colspan="8">{{ item.lastError }}</td>
                </tr>
              }
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .btn-secondary { padding: 0.5rem 0.875rem; border-radius: 0.375rem; border: 1px solid #d1d5db; text-decoration: none; color: #111827; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 0.625rem; border-bottom: 1px solid #e5e7eb; }
    .error-row td { color: #b91c1c; font-size: 0.875rem; }
    .actions button { cursor: pointer; }
  `]
})
export class NotificationLogListPageComponent implements OnInit {
  private readonly api = inject(NotificationsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: NotificationLogSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.items = await firstValueFrom(this.api.listLogs());
      this.state = this.items.length === 0 ? 'empty' : 'ready';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load notification history.';
      this.state = 'error';
    }
  }

  async retry(item: NotificationLogSummary): Promise<void> {
    await firstValueFrom(this.api.retryLog(item.id));
    await this.load();
  }
}
