import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomerAccountApi, CustomerPreference } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>Preferences</h1>
    <p><a routerLink="/account">← Account</a></p>
    @if (state === 'loading') { <cmr-loading-state /> } @else {
      <form [formGroup]="form" (ngSubmit)="save()">
        <label>Marketing emails
          <select formControlName="marketingEmails">
            <option value="true">Enabled</option>
            <option value="false">Disabled</option>
          </select>
        </label>
        <label>Preferred language<input formControlName="preferredLanguage" /></label>
        <button type="submit" [disabled]="saving">Save preferences</button>
      </form>
      @if (preferences.length) {
        <ul>
          @for (pref of preferences; track pref.id) {
            <li>{{ pref.preferenceKey }}: {{ pref.preferenceValue }}</li>
          }
        </ul>
      }
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `,
  styles: [`form { display: grid; gap: 1rem; max-width: 28rem; } label { display: grid; gap: 0.375rem; }`]
})
export class AccountPreferencesPageComponent implements OnInit {
  private readonly api = inject(CustomerAccountApi);
  private readonly fb = inject(FormBuilder);

  state: PageState = 'loading';
  saving = false;
  errorMessage = '';
  preferences: CustomerPreference[] = [];

  readonly form = this.fb.nonNullable.group({
    marketingEmails: 'true',
    preferredLanguage: 'en'
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    try {
      this.preferences = await firstValueFrom(this.api.listPreferences());
      const marketing = this.preferences.find(p => p.preferenceKey === 'marketing.emails');
      const language = this.preferences.find(p => p.preferenceKey === 'preferred.language');
      this.form.patchValue({
        marketingEmails: marketing?.preferenceValue ?? 'true',
        preferredLanguage: language?.preferenceValue ?? 'en'
      });
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load preferences.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    this.saving = true;
    const value = this.form.getRawValue();
    try {
      await firstValueFrom(this.api.upsertPreference({ preferenceKey: 'marketing.emails', preferenceValue: value.marketingEmails }));
      await firstValueFrom(this.api.upsertPreference({ preferenceKey: 'preferred.language', preferenceValue: value.preferredLanguage }));
      this.preferences = await firstValueFrom(this.api.listPreferences());
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
    } finally {
      this.saving = false;
    }
  }
}
