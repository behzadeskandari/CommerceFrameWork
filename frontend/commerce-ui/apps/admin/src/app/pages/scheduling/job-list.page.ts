import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BackgroundJobSummary, RecurringJobScheduleSummary, SchedulingApi } from '@commerce/api';
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
      { label: ('scheduling.jobs.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'scheduling.jobs.title' | translate }}</h1>
      <a routerLink="/scheduling/recurring" class="btn-secondary">{{ 'scheduling.recurring.title' | translate }}</a>
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>{{ 'scheduling.jobs.type' | translate }}</th>
              <th>{{ 'scheduling.jobs.status' | translate }}</th>
              <th>{{ 'scheduling.jobs.attempts' | translate }}</th>
              <th>{{ 'scheduling.jobs.nextExecution' | translate }}</th>
              <th>{{ 'scheduling.jobs.createdAt' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.jobType }}</td>
                <td>{{ item.status }}</td>
                <td>{{ item.attemptCount }}/{{ item.maxAttempts }}</td>
                <td>{{ (item.nextRetryAtUtc ?? item.executeAtUtc) | date: 'short' }}</td>
                <td>{{ item.createdAtUtc | date: 'short' }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Scheduling.Manage')) {
                    @if (item.status !== 'Completed' && item.status !== 'Cancelled') {
                      <button type="button" (click)="cancel(item)">{{ 'action.cancel' | translate }}</button>
                    }
                    @if (item.status === 'Failed' || item.status === 'DeadLetter') {
                      <button type="button" (click)="retry(item)">{{ 'scheduling.jobs.retry' | translate }}</button>
                    }
                  }
                </td>
              </tr>
              @if (item.lastError) {
                <tr class="error-row"><td colspan="6">{{ item.lastError }}</td></tr>
              }
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .btn-secondary { padding: 0.5rem 0.875rem; border: 1px solid #d1d5db; border-radius: 0.375rem; text-decoration: none; color: #111827; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 0.625rem; border-bottom: 1px solid #e5e7eb; }
    .error-row td { color: #b91c1c; font-size: 0.875rem; }
    .actions { display: flex; gap: 0.5rem; }
  `]
})
export class BackgroundJobListPageComponent implements OnInit {
  private readonly api = inject(SchedulingApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: BackgroundJobSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.items = await firstValueFrom(this.api.listJobs());
      this.state = this.items.length === 0 ? 'empty' : 'ready';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load jobs.';
      this.state = 'error';
    }
  }

  async cancel(item: BackgroundJobSummary): Promise<void> {
    await firstValueFrom(this.api.cancelJob(item.id));
    await this.load();
  }

  async retry(item: BackgroundJobSummary): Promise<void> {
    await firstValueFrom(this.api.retryJob(item.id));
    await this.load();
  }
}

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink, BreadcrumbsComponent, TranslatePipe, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('scheduling.jobs.title' | translate), link: '/scheduling/jobs' },
      { label: ('scheduling.recurring.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'scheduling.recurring.title' | translate }}</h1>
      <a routerLink="/scheduling/jobs" class="btn-secondary">{{ 'scheduling.jobs.title' | translate }}</a>
    </header>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>{{ 'scheduling.recurring.key' | translate }}</th>
              <th>{{ 'scheduling.jobs.type' | translate }}</th>
              <th>{{ 'scheduling.recurring.interval' | translate }}</th>
              <th>{{ 'scheduling.recurring.enabled' | translate }}</th>
              <th>{{ 'scheduling.jobs.nextExecution' | translate }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.scheduleKey }}</td>
                <td>{{ item.jobType }}</td>
                <td>{{ item.intervalSeconds }}s</td>
                <td>{{ item.isEnabled ? ('pricing.active' | translate) : ('pricing.inactive' | translate) }}</td>
                <td>{{ item.nextRunAtUtc | date: 'short' }}</td>
                <td class="actions">
                  @if (permissions.hasPermission('Scheduling.Manage')) {
                    @if (item.isEnabled) {
                      <button type="button" (click)="disable(item)">{{ 'pricing.deactivate' | translate }}</button>
                    } @else {
                      <button type="button" (click)="enable(item)">{{ 'pricing.activate' | translate }}</button>
                    }
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
    .btn-secondary { padding: 0.5rem 0.875rem; border: 1px solid #d1d5db; border-radius: 0.375rem; text-decoration: none; color: #111827; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 0.625rem; border-bottom: 1px solid #e5e7eb; }
    .actions button { cursor: pointer; }
  `]
})
export class RecurringJobListPageComponent implements OnInit {
  private readonly api = inject(SchedulingApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: RecurringJobScheduleSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.items = await firstValueFrom(this.api.listRecurring());
      this.state = this.items.length === 0 ? 'empty' : 'ready';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load recurring schedules.';
      this.state = 'error';
    }
  }

  async enable(item: RecurringJobScheduleSummary): Promise<void> {
    await firstValueFrom(this.api.enableRecurring(item.scheduleKey));
    await this.load();
  }

  async disable(item: RecurringJobScheduleSummary): Promise<void> {
    await firstValueFrom(this.api.disableRecurring(item.scheduleKey));
    await this.load();
  }
}
