import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CmsApi, ContentPageSummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [RouterLink, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'cms.pages' | translate }}</h1>
    <p><a routerLink="/cms/pages/new">{{ 'action.create' | translate }}</a></p>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <table>
          <thead><tr><th>Title</th><th>Slug</th><th>Published</th><th></th></tr></thead>
          <tbody>
            @for (page of pages; track page.id) {
              <tr>
                <td>{{ page.defaultTitle }}</td>
                <td>{{ page.defaultSlug }}</td>
                <td>{{ page.isPublished ? 'Yes' : 'No' }}</td>
                <td><a [routerLink]="['/cms/pages', page.id]">{{ 'action.edit' | translate }}</a></td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`table { width: 100%; border-collapse: collapse; } th, td { border: 1px solid #e5e7eb; padding: 0.5rem; }`]
})
export class CmsPageListPageComponent implements OnInit {
  private readonly api = inject(CmsApi);
  state: PageState = 'loading';
  errorMessage = '';
  pages: ContentPageSummary[] = [];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.pages = await firstValueFrom(this.api.listPages());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load pages.';
      this.state = 'error';
    }
  }
}
