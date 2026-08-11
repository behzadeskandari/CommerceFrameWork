import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomerDetail, CustomersApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'nav.account' | translate }}</h1>
    @if (state === 'loading') { <cmr-loading-state /> } @else if (customer) {
      <form [formGroup]="form" (ngSubmit)="save()">
        <label>{{ 'auth.firstName' | translate }}<input formControlName="firstName" /></label>
        <label>{{ 'auth.lastName' | translate }}<input formControlName="lastName" /></label>
        <label>{{ 'auth.email' | translate }}<input [value]="customer.email" readonly /></label>
        <label>{{ 'auth.phone' | translate }}<input formControlName="phoneNumber" /></label>
        <button type="submit" [disabled]="saving">{{ 'action.save' | translate }}</button>
      </form>
      <p><a routerLink="/account/addresses">{{ 'nav.addresses' | translate }}</a></p>
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `,
  styles: [`
    form { display: grid; gap: 1rem; max-width: 28rem; }
    label { display: grid; gap: 0.375rem; }
    input { padding: 0.625rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    button { width: fit-content; padding: 0.625rem 1rem; background: var(--primary, #0f766e); color: #fff; border: none; border-radius: 0.375rem; }
  `]
})
export class AccountPageComponent implements OnInit {
  private readonly customersApi = inject(CustomersApi);
  private readonly fb = inject(FormBuilder);

  state: PageState = 'loading';
  errorMessage = '';
  saving = false;
  customer: CustomerDetail | null = null;

  readonly form = this.fb.nonNullable.group({
    firstName: [''],
    lastName: [''],
    phoneNumber: ['']
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    try {
      this.customer = await firstValueFrom(this.customersApi.getCurrentCustomer());
      this.form.patchValue({
        firstName: this.customer.firstName,
        lastName: this.customer.lastName,
        phoneNumber: this.customer.phoneNumber ?? ''
      });
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load profile.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    this.saving = true;
    const value = this.form.getRawValue();
    try {
      this.customer = await firstValueFrom(this.customersApi.updateCurrentCustomer({
        firstName: value.firstName,
        lastName: value.lastName,
        phoneNumber: value.phoneNumber || null
      }));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
    } finally {
      this.saving = false;
    }
  }
}
