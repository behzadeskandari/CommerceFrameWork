import { Component, OnInit, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CustomerDetail, CustomersApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    @if (state === 'loading') { <cmr-loading-state /> } @else if (customer) {
      <cmr-breadcrumbs [items]="[
        { label: 'Dashboard', link: '/dashboard' },
        { label: 'Customers', link: '/customers' },
        { label: customer.firstName + ' ' + customer.lastName }
      ]" />
      <h1>{{ customer.firstName }} {{ customer.lastName }}</h1>
      <p>{{ customer.email }} · {{ customer.active ? 'Active' : 'Inactive' }}</p>
      @if (permissions.hasPermission('Customers.Update')) {
        <form [formGroup]="form" (ngSubmit)="save()">
          <label>First name<input formControlName="firstName" /></label>
          <label>Last name<input formControlName="lastName" /></label>
          <label>Phone<input formControlName="phoneNumber" /></label>
          <button type="submit" [disabled]="saving">Save</button>
        </form>
      }
      <section>
        <h2>Addresses</h2>
        @if (!customer.addresses.length) { <p>No addresses.</p> }
        <ul>
          @for (address of customer.addresses; track address.id) {
            <li>{{ address.label }} — {{ address.address1 }}, {{ address.city }}</li>
          }
        </ul>
      </section>
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `,
  styles: [`
    form { display: grid; gap: 1rem; max-width: 28rem; margin: 1rem 0; }
    label { display: grid; gap: 0.375rem; }
    input { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    button { width: fit-content; padding: 0.625rem 1rem; background: #2563eb; color: #fff; border: none; border-radius: 0.375rem; }
  `]
})
export class CustomerDetailPageComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly customersApi = inject(CustomersApi);
  readonly permissions = inject(PermissionService);
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
    this.state = 'loading';
    try {
      this.customer = await firstValueFrom(this.customersApi.getCustomerAdmin(Number(this.id())));
      this.form.patchValue({
        firstName: this.customer.firstName,
        lastName: this.customer.lastName,
        phoneNumber: this.customer.phoneNumber ?? ''
      });
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load customer.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    if (!this.customer) return;
    this.saving = true;
    const value = this.form.getRawValue();
    try {
      this.customer = await firstValueFrom(this.customersApi.updateCustomerAdmin(this.customer.id, {
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
