import { Component, OnInit, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ThemeApi, ThemeDetail, ThemeSettingDefinition } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <p><a routerLink="/themes">{{ 'themes.backToList' | translate }}</a></p>
    <h1>{{ theme?.name ?? ('themes.configure' | translate) }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        @if (theme) {
          <p>{{ theme.description }}</p>
          <form [formGroup]="form" (ngSubmit)="save()">
            <fieldset>
              <legend>{{ 'themes.storeAssignment' | translate }}</legend>
              <label>Store ID<input type="number" formControlName="storeId" min="1" /></label>
            </fieldset>
            <fieldset>
              <legend>{{ 'themes.branding' | translate }}</legend>
              @for (setting of theme.settings; track setting.key) {
                <label>{{ setting.label }}<input [formControlName]="setting.key" /></label>
              }
            </fieldset>
            <button type="submit">{{ 'action.save' | translate }}</button>
          </form>
          <h2>{{ 'themes.layouts' | translate }}</h2>
          <ul>
            @for (layout of theme.layouts; track layout.layoutType) {
              <li>{{ layout.layoutType }} — {{ layout.zones.join(', ') }}</li>
            }
          </ul>
        }
      }
    }
  `,
  styles: [`form { display: grid; gap: 1rem; max-width: 40rem; } fieldset { border: 1px solid #e5e7eb; padding: 1rem; } label { display: grid; gap: 0.375rem; margin-bottom: 0.75rem; }`]
})
export class ThemeDetailPageComponent implements OnInit {
  readonly systemName = input.required<string>();
  private readonly api = inject(ThemeApi);
  private readonly fb = inject(FormBuilder);
  state: PageState = 'loading';
  errorMessage = '';
  theme: ThemeDetail | null = null;
  readonly form = this.fb.group<{ storeId: number; [key: string]: unknown }>({ storeId: 1 });

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.theme = await firstValueFrom(this.api.getTheme(this.systemName()));
      this.buildForm(this.theme.settings);
      const assignment = await firstValueFrom(this.api.getStoreAssignment(this.form.value.storeId ?? 1));
      if (assignment) {
        for (const setting of this.theme.settings) {
          const value = assignment.settings[setting.key] ?? setting.defaultValue;
          this.form.patchValue({ [setting.key]: value });
        }
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load theme.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    if (!this.theme) return;
    const raw = this.form.getRawValue() as Record<string, string | number>;
    const storeId = Number(raw['storeId'] ?? 1);
    const settings: Record<string, string> = {};
    for (const setting of this.theme.settings) {
      settings[setting.key] = String(raw[setting.key] ?? setting.defaultValue);
    }

    try {
      await firstValueFrom(this.api.saveStoreAssignment(storeId, {
        themeSystemName: this.theme.systemName,
        settings
      }));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to save theme settings.';
      this.state = 'error';
    }
  }

  private buildForm(settings: ThemeSettingDefinition[]): void {
    for (const setting of settings) {
      (this.form as ReturnType<FormBuilder['group']>).addControl(setting.key, this.fb.control(setting.defaultValue));
    }
  }
}
