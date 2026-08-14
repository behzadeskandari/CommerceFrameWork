import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SeoApi } from '@commerce/api';
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
      { label: ('seo.title' | translate) }
    ]" />
    <h1>{{ 'seo.settings' | translate }}</h1>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <label>{{ 'seo.storeId' | translate }}<input type="number" [(ngModel)]="storeId" name="storeId" (change)="load()" /></label>
          <label>{{ 'seo.defaultMetaTitle' | translate }}<input [(ngModel)]="form.defaultMetaTitle" name="defaultMetaTitle" /></label>
          <label>{{ 'seo.defaultMetaDescription' | translate }}
            <textarea [(ngModel)]="form.defaultMetaDescription" name="defaultMetaDescription" rows="3"></textarea>
          </label>
          <label>{{ 'seo.robotsTxt' | translate }}
            <textarea [(ngModel)]="form.robotsTxt" name="robotsTxt" rows="8"></textarea>
          </label>
          <label><input type="checkbox" [(ngModel)]="form.sitemapEnabled" name="sitemapEnabled" /> {{ 'seo.sitemapEnabled' | translate }}</label>
          <button type="submit">{{ 'action.save' | translate }}</button>
        </form>
        <p class="hint">{{ 'seo.publicEndpoints' | translate }}</p>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.875rem; max-width: 40rem; }
    label { display: grid; gap: 0.25rem; }
    .hint { color: #6b7280; margin-top: 1rem; }
  `]
})
export class SeoSettingsPageComponent implements OnInit {
  private readonly api = inject(SeoApi);
  state: PageState = 'loading';
  errorMessage = '';
  storeId = 1;
  form = {
    defaultMetaTitle: '',
    defaultMetaDescription: '',
    robotsTxt: '',
    sitemapEnabled: true
  };

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const settings = await firstValueFrom(this.api.getSettings(this.storeId));
      this.form = {
        defaultMetaTitle: settings.defaultMetaTitle ?? '',
        defaultMetaDescription: settings.defaultMetaDescription ?? '',
        robotsTxt: settings.robotsTxt ?? '',
        sitemapEnabled: settings.sitemapEnabled
      };
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load SEO settings.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    await firstValueFrom(this.api.updateSettings(this.storeId, {
      defaultMetaTitle: this.form.defaultMetaTitle || null,
      defaultMetaDescription: this.form.defaultMetaDescription || null,
      robotsTxt: this.form.robotsTxt || null,
      sitemapEnabled: this.form.sitemapEnabled
    }));
    await this.load();
  }
}
