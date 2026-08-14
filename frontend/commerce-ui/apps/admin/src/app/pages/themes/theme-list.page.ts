import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeApi, ThemeSummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [RouterLink, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'themes.title' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <table>
          <thead><tr><th>Name</th><th>Version</th><th>Author</th><th></th></tr></thead>
          <tbody>
            @for (theme of themes; track theme.systemName) {
              <tr>
                <td>{{ theme.name }}</td>
                <td>{{ theme.version }}</td>
                <td>{{ theme.author }}</td>
                <td><a [routerLink]="['/themes', theme.systemName]">{{ 'action.configure' | translate }}</a></td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`table { width: 100%; border-collapse: collapse; } th, td { border: 1px solid #e5e7eb; padding: 0.5rem; }`]
})
export class ThemeListPageComponent implements OnInit {
  private readonly api = inject(ThemeApi);
  state: PageState = 'loading';
  errorMessage = '';
  themes: ThemeSummary[] = [];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.themes = await firstValueFrom(this.api.listThemes());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load themes.';
      this.state = 'error';
    }
  }
}
