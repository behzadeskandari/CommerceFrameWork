import { Component, OnInit, inject } from '@angular/core';
import { CmsApi, MenuSummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'cms.menus' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <table>
          <thead><tr><th>System name</th><th>Name</th><th>Published</th></tr></thead>
          <tbody>
            @for (menu of menus; track menu.id) {
              <tr>
                <td>{{ menu.systemName }}</td>
                <td>{{ menu.name }}</td>
                <td>{{ menu.isPublished ? 'Yes' : 'No' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`table { width: 100%; border-collapse: collapse; } th, td { border: 1px solid #e5e7eb; padding: 0.5rem; }`]
})
export class CmsMenuListPageComponent implements OnInit {
  private readonly api = inject(CmsApi);
  state: PageState = 'loading';
  errorMessage = '';
  menus: MenuSummary[] = [];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.menus = await firstValueFrom(this.api.listMenus());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load menus.';
      this.state = 'error';
    }
  }
}
