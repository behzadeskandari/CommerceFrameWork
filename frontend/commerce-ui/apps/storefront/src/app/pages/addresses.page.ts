import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomerAddress, CustomersApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import {
  ConfirmDialogComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ConfirmDialogComponent
  ],
  template: `
    <h1>{{ 'nav.addresses' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <ul class="addresses">
          @for (address of addresses; track address.id) {
            <li>
              <strong>{{ address.label }}</strong>
              <p>{{ address.firstName }} {{ address.lastName }}</p>
              <p>{{ address.address1 }}, {{ address.city }}, {{ address.country }}</p>
              <button type="button" (click)="confirmDelete(address)">Delete</button>
            </li>
          }
        </ul>
      }
    }
    <form [formGroup]="form" (ngSubmit)="addAddress()">
      <h2>Add address</h2>
      <label>Label<input formControlName="label" /></label>
      <label>First name<input formControlName="firstName" /></label>
      <label>Last name<input formControlName="lastName" /></label>
      <label>Country<input formControlName="country" /></label>
      <label>City<input formControlName="city" /></label>
      <label>Address<input formControlName="address1" /></label>
      <label>Postal code<input formControlName="postalCode" /></label>
      <button type="submit" [disabled]="form.invalid || saving">Add</button>
    </form>
    <cmr-confirm-dialog
      [open]="deleteTarget !== null"
      title="Delete address"
      message="Remove this address?"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null" />
  `,
  styles: [`
    .addresses { list-style: none; padding: 0; display: grid; gap: 1rem; }
    li { padding: 1rem; border: 1px solid #e5e7eb; border-radius: 0.5rem; background: #fff; }
    form { display: grid; gap: 0.75rem; max-width: 28rem; margin-top: 2rem; }
    label { display: grid; gap: 0.25rem; }
    input { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    button { width: fit-content; padding: 0.5rem 0.875rem; border-radius: 0.375rem; border: 1px solid #d1d5db; background: #fff; cursor: pointer; }
  `]
})
export class AddressesPageComponent implements OnInit {
  private readonly customersApi = inject(CustomersApi);
  private readonly fb = inject(FormBuilder);

  state: PageState = 'loading';
  errorMessage = '';
  saving = false;
  addresses: CustomerAddress[] = [];
  deleteTarget: CustomerAddress | null = null;

  readonly form = this.fb.nonNullable.group({
    label: ['Home', Validators.required],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    country: ['', Validators.required],
    city: ['', Validators.required],
    address1: ['', Validators.required],
    postalCode: ['', Validators.required]
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.addresses = await firstValueFrom(this.customersApi.listAddresses());
      this.state = this.addresses.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load addresses.';
      this.state = 'error';
    }
  }

  async addAddress(): Promise<void> {
    if (this.form.invalid) return;
    this.saving = true;
    try {
      await firstValueFrom(this.customersApi.addAddress(this.form.getRawValue()));
      this.form.reset({ label: 'Home' });
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to add address.';
      this.state = 'error';
    } finally {
      this.saving = false;
    }
  }

  confirmDelete(address: CustomerAddress): void {
    this.deleteTarget = address;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.customersApi.deleteAddress(this.deleteTarget.id));
      this.deleteTarget = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';
      this.state = 'error';
    }
  }
}
