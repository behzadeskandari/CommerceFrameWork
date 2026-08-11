import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SettingEntry, StoreApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: ('nav.settings' | translate) }]" />
    <header class="page-header"><h1>{{ 'nav.settings' | translate }}</h1></header>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="settings-form" (ngSubmit)="save()">
          @for (group of groupedSettings; track group.name) {
            <section>
              <h2>{{ group.name }}</h2>
              @for (setting of group.items; track setting.key) {
                <label>
                  {{ setting.description || setting.key }}
                  <input [(ngModel)]="setting.value" [name]="setting.key" />
                </label>
              }
            </section>
          }
          <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
        </form>
      }
    }
  `,
  styles: [`
    .settings-form { display: grid; gap: 1.25rem; max-width: 40rem; }
    section { background: #fff; padding: 1rem; border-radius: 0.5rem; display: grid; gap: 0.75rem; }
    label { display: grid; gap: 0.25rem; }
    input { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .btn { padding: 0.5rem 1rem; border: none; border-radius: 0.375rem; cursor: pointer; }
    .btn--primary { background: #2563eb; color: #fff; }
  `]
})
export class SettingsPageComponent implements OnInit {
  private readonly storeApi = inject(StoreApi);

  state: PageState = 'loading';
  errorMessage = '';
  settings: SettingEntry[] = [];
  groupedSettings: Array<{ name: string; items: SettingEntry[] }> = [];

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.settings = await firstValueFrom(this.storeApi.listSettings());
      this.groupedSettings = this.groupSettings(this.settings);
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load settings.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      await firstValueFrom(this.storeApi.updateSettings({
        settings: this.settings.map(setting => ({ key: setting.key, value: setting.value }))
      }));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }

  private groupSettings(settings: SettingEntry[]): Array<{ name: string; items: SettingEntry[] }> {
    const groups = new Map<string, SettingEntry[]>();
    for (const setting of settings) {
      const prefix = setting.key.split('.')[0] ?? 'System';
      const items = groups.get(prefix) ?? [];
      items.push(setting);
      groups.set(prefix, items);
    }

    return [...groups.entries()].map(([name, items]) => ({ name, items }));
  }
}
