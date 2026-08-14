import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SeoApi, UpsertUrlRecordRequest, UrlRecordDto } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('seo.title' | translate), link: '/marketing/seo/settings' },
      { label: ('seo.urlRecords' | translate) }
    ]" />
    <h1>{{ 'seo.urlRecords' | translate }}</h1>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="upsert()">
          <h2>{{ 'seo.upsertUrlRecord' | translate }}</h2>
          <label>{{ 'seo.entityName' | translate }}<input [(ngModel)]="draft.entityName" name="entityName" required /></label>
          <label>{{ 'seo.entityId' | translate }}<input type="number" [(ngModel)]="draft.entityId" name="entityId" required /></label>
          <label>{{ 'seo.slug' | translate }}<input [(ngModel)]="draft.slug" name="slug" required /></label>
          <label>{{ 'seo.storeId' | translate }}<input type="number" [(ngModel)]="draft.storeId" name="storeId" /></label>
          <label><input type="checkbox" [(ngModel)]="draft.isActive" name="isActive" /> {{ 'seo.active' | translate }}</label>
          <button type="submit">{{ 'action.save' | translate }}</button>
        </form>

        <table>
          <thead>
            <tr>
              <th>{{ 'seo.entityName' | translate }}</th>
              <th>{{ 'seo.entityId' | translate }}</th>
              <th>{{ 'seo.slug' | translate }}</th>
              <th>{{ 'seo.storeId' | translate }}</th>
              <th>{{ 'seo.active' | translate }}</th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td>{{ item.entityName }}</td>
                <td>{{ item.entityId }}</td>
                <td>{{ item.slug }}</td>
                <td>{{ item.storeId ?? '—' }}</td>
                <td>{{ item.isActive ? ('pricing.active' | translate) : ('pricing.inactive' | translate) }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.75rem; max-width: 32rem; margin-bottom: 1.5rem; }
    label { display: grid; gap: 0.25rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; }
    th, td { padding: 0.625rem; border-bottom: 1px solid #e5e7eb; text-align: left; }
  `]
})
export class SeoUrlRecordsPageComponent implements OnInit {
  private readonly api = inject(SeoApi);
  state: PageState = 'loading';
  errorMessage = '';
  items: UrlRecordDto[] = [];
  draft: UpsertUrlRecordRequest = {
    entityName: 'Product',
    entityId: 1,
    slug: '',
    storeId: 1,
    isActive: true
  };

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.items = await firstValueFrom(this.api.listUrlRecords());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load URL records.';
      this.state = 'error';
    }
  }

  async upsert(): Promise<void> {
    await firstValueFrom(this.api.upsertUrlRecord(this.draft));
    this.draft.slug = '';
    await this.load();
  }
}
