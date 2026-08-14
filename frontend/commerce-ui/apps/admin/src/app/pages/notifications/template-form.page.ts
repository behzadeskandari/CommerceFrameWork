import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateNotificationTemplateRequest,
  NotificationChannel,
  NotificationEventType,
  NotificationsApi,
  UpdateNotificationTemplateRequest
} from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('notifications.templates.title' | translate), link: '/notifications/templates' },
      { label: isEdit ? form.systemName : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.systemName : ('notifications.templates.create' | translate) }}</h1>
          @if (!isEdit) {
            <label>{{ 'notifications.templates.systemName' | translate }}
              <input [(ngModel)]="form.systemName" name="systemName" required />
            </label>
            <label>{{ 'notifications.templates.eventType' | translate }}
              <select [(ngModel)]="form.eventType" name="eventType" required>
                @for (event of eventTypes; track event) {
                  <option [value]="event">{{ event }}</option>
                }
              </select>
            </label>
            <label>{{ 'notifications.templates.channel' | translate }}
              <select [(ngModel)]="form.channel" name="channel" required>
                @for (channel of channels; track channel) {
                  <option [value]="channel">{{ channel }}</option>
                }
              </select>
            </label>
          }
          <label>{{ 'notifications.templates.subject' | translate }}
            <input [(ngModel)]="form.subject" name="subject" required />
          </label>
          <label>{{ 'notifications.templates.body' | translate }}
            <textarea [(ngModel)]="form.body" name="body" rows="8" required></textarea>
          </label>
          <label>{{ 'notifications.templates.variablesJson' | translate }}
            <textarea [(ngModel)]="form.variablesJson" name="variablesJson" rows="3" placeholder='["orderNumber","grandTotal"]'></textarea>
          </label>
          <label>{{ 'notifications.templates.languageId' | translate }}
            <input type="number" [(ngModel)]="form.languageId" name="languageId" />
          </label>
          <label>{{ 'notifications.templates.storeId' | translate }}
            <input type="number" [(ngModel)]="form.storeId" name="storeId" />
          </label>
          <label><input type="checkbox" [(ngModel)]="form.isEnabled" name="isEnabled" /> {{ 'notifications.templates.enabled' | translate }}</label>
          <div class="actions">
            <button type="submit">{{ 'action.save' | translate }}</button>
            <a routerLink="/notifications/templates">{{ 'action.cancel' | translate }}</a>
          </div>
        </form>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.875rem; max-width: 40rem; }
    label { display: grid; gap: 0.25rem; }
    .actions { display: flex; gap: 0.75rem; align-items: center; }
  `]
})
export class NotificationTemplateFormPageComponent implements OnInit {
  readonly id = input<string | undefined>();
  private readonly api = inject(NotificationsApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;

  readonly eventTypes: NotificationEventType[] = [
    'CustomerRegistered',
    'OrderCreated',
    'PaymentSucceeded',
    'PaymentFailed',
    'OrderCancelled',
    'ShipmentCreated',
    'RefundCreated',
    'DownloadAvailable'
  ];

  readonly channels: NotificationChannel[] = ['Email', 'Sms', 'InApp'];

  form = {
    systemName: '',
    eventType: 'OrderCreated' as NotificationEventType,
    channel: 'Email' as NotificationChannel,
    subject: '',
    body: '',
    variablesJson: '',
    languageId: null as number | null,
    storeId: null as number | null,
    isEnabled: true
  };

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    const rawId = this.id();
    if (!rawId || rawId === 'new') {
      this.isEdit = false;
      this.state = 'ready';
      return;
    }

    this.isEdit = true;
    this.state = 'loading';
    try {
      const detail = await firstValueFrom(this.api.getTemplate(Number(rawId)));
      this.form = {
        systemName: detail.systemName,
        eventType: detail.eventType,
        channel: detail.channel,
        subject: detail.subject,
        body: detail.body,
        variablesJson: detail.variablesJson ?? '',
        languageId: detail.languageId,
        storeId: detail.storeId,
        isEnabled: detail.isEnabled
      };
      this.state = 'ready';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load template.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      if (this.isEdit) {
        const request: UpdateNotificationTemplateRequest = {
          subject: this.form.subject,
          body: this.form.body,
          languageId: this.form.languageId,
          storeId: this.form.storeId,
          variablesJson: this.form.variablesJson || null,
          isEnabled: this.form.isEnabled
        };
        await firstValueFrom(this.api.updateTemplate(Number(this.id()), request));
      } else {
        const request: CreateNotificationTemplateRequest = {
          systemName: this.form.systemName,
          eventType: this.form.eventType,
          channel: this.form.channel,
          subject: this.form.subject,
          body: this.form.body,
          languageId: this.form.languageId,
          storeId: this.form.storeId,
          variablesJson: this.form.variablesJson || null,
          isEnabled: this.form.isEnabled
        };
        await firstValueFrom(this.api.createTemplate(request));
      }
      await this.router.navigateByUrl('/notifications/templates');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to save template.';
      this.state = 'error';
    }
  }
}
