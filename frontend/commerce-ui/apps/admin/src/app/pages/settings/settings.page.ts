import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SettingEntry, StoreApi } from '@commerce/api';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import {
  AdminPageShellComponent,
  FilterBarComponent,
  FormFieldComponent,
  ToastService,
  resolveAdminError
} from '@commerce/ui';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    TranslatePipe,
    BreadcrumbsComponent,
    AdminPageShellComponent,
    FilterBarComponent,
    FormFieldComponent,
    LoadingStateComponent,
    ErrorStateComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: ('nav.dashboard' | translate), link: '/dashboard' },
      { label: ('nav.settings' | translate) }
    ]" />

    <cmr-admin-page-shell [title]="'nav.settings' | translate">
      <div toolbar>
        <cmr-filter-bar
          [search]="search"
          [searchPlaceholderKey]="'admin.settings.search'"
          [showReset]="true"
          (searchChange)="search = $event"
          (reset)="search = ''" />
      </div>

      @switch (state) {
        @case ('loading') { <cmr-loading-state /> }
        @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
        @default {
          <form class="settings-form" (ngSubmit)="save()">
            @for (group of visibleGroups; track group.name) {
              <section>
                <h2>{{ group.name }}</h2>
                @for (setting of group.items; track setting.key) {
                  <cmr-form-field [label]="setting.description || setting.key">
                    @switch (setting.valueType) {
                      @case ('Boolean') {
                        <select [(ngModel)]="setting.value" [name]="setting.key">
                          <option value="true">{{ 'common.yes' | translate }}</option>
                          <option value="false">{{ 'common.no' | translate }}</option>
                        </select>
                      }
                      @case ('boolean') {
                        <select [(ngModel)]="setting.value" [name]="setting.key">
                          <option value="true">{{ 'common.yes' | translate }}</option>
                          <option value="false">{{ 'common.no' | translate }}</option>
                        </select>
                      }
                      @case ('Integer') {
                        <input type="number" [(ngModel)]="setting.value" [name]="setting.key" />
                      }
                      @case ('integer') {
                        <input type="number" [(ngModel)]="setting.value" [name]="setting.key" />
                      }
                      @case ('Decimal') {
                        <input type="number" step="0.01" [(ngModel)]="setting.value" [name]="setting.key" />
                      }
                      @case ('decimal') {
                        <input type="number" step="0.01" [(ngModel)]="setting.value" [name]="setting.key" />
                      }
                      @default {
                        <input type="text" [(ngModel)]="setting.value" [name]="setting.key" />
                      }
                    }
                  </cmr-form-field>
                }
              </section>
            }
            @if (!visibleGroups.length) {
              <p>{{ 'admin.settings.noResults' | translate }}</p>
            }
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
          </form>
        }
      }
    </cmr-admin-page-shell>
  `,
  styles: [`
    .settings-form { display: grid; gap: 1.25rem; max-width: 48rem; }
    section {
      background: var(--surface-elevated, #fff);
      padding: 1rem;
      border-radius: var(--radius-lg, 0.75rem);
      border: 1px solid #e5e7eb;
      display: grid;
      gap: 0.75rem;
    }
    h2 { margin: 0; font-size: 1.1rem; }
  `]
})
export class SettingsPageComponent implements OnInit {
  private readonly storeApi = inject(StoreApi);
  private readonly toast = inject(ToastService);

  state: PageState = 'loading';
  errorMessage = '';
  settings: SettingEntry[] = [];
  search = '';

  ngOnInit(): void { void this.load(); }

  get visibleGroups(): Array<{ name: string; items: SettingEntry[] }> {
    const term = this.search.trim().toLowerCase();
    const filtered = term
      ? this.settings.filter(setting =>
          setting.key.toLowerCase().includes(term) ||
          (setting.description ?? '').toLowerCase().includes(term))
      : this.settings;
    return this.groupSettings(filtered);
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.settings = await firstValueFrom(this.storeApi.listSettings());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = resolveAdminError(error, 'Failed to load settings.');
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      await firstValueFrom(this.storeApi.updateSettings({
        settings: this.settings.map(setting => ({ key: setting.key, value: setting.value }))
      }));
      this.toast.success('Settings saved.');
    } catch (error) {
      this.toast.error(resolveAdminError(error, 'Save failed.'));
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
