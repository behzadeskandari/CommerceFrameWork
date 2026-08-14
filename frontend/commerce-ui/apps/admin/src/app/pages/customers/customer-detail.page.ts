import { Component, OnInit, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import {
  CustomerAccountApi,
  CustomerActivity,
  CustomerDetail,
  CustomerPurchaseHistoryItem,
  CustomersApi,
  LoyaltyAccount
} from '@commerce/api';
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
      <p>{{ customer.email }} · Group: {{ customer.customerGroupId ?? 'None' }} · {{ customer.active ? 'Active' : 'Inactive' }}</p>
      @if (permissions.hasPermission('Customers.Update')) {
        <form [formGroup]="form" (ngSubmit)="save()">
          <label>First name<input formControlName="firstName" /></label>
          <label>Last name<input formControlName="lastName" /></label>
          <label>Phone<input formControlName="phoneNumber" /></label>
          <button type="submit" [disabled]="saving">Save profile</button>
        </form>
      }
      @if (permissions.hasPermission('Customers.Manage')) {
        <form [formGroup]="groupForm" (ngSubmit)="assignGroup()">
          <label>Customer group ID<input type="number" formControlName="customerGroupId" /></label>
          <button type="submit">Assign group</button>
        </form>
      }
      <section>
        <h2>Purchase history</h2>
        <ul>
          @for (order of purchaseHistory; track order.orderId) {
            <li>{{ order.orderNumber }} — {{ order.grandTotal }} {{ order.currencyCode }} ({{ order.status }})</li>
          }
        </ul>
      </section>
      <section>
        <h2>Loyalty</h2>
        @if (loyalty) { <p>Points: {{ loyalty.pointsBalance }}</p> }
      </section>
      <section>
        <h2>Activity</h2>
        <ul>
          @for (item of activity; track item.id) {
            <li>{{ item.summary }}</li>
          }
        </ul>
      </section>
      <section>
        <h2>Addresses</h2>
        <ul>
          @for (address of customer.addresses; track address.id) {
            <li>{{ address.label }} — {{ address.address1 }}, {{ address.city }}</li>
          }
        </ul>
      </section>
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `
})
export class CustomerDetailPageComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly customersApi = inject(CustomersApi);
  private readonly accountApi = inject(CustomerAccountApi);
  readonly permissions = inject(PermissionService);
  private readonly fb = inject(FormBuilder);

  state: PageState = 'loading';
  errorMessage = '';
  saving = false;
  customer: CustomerDetail | null = null;
  purchaseHistory: CustomerPurchaseHistoryItem[] = [];
  loyalty: LoyaltyAccount | null = null;
  activity: CustomerActivity[] = [];

  readonly form = this.fb.nonNullable.group({ firstName: [''], lastName: [''], phoneNumber: [''] });
  readonly groupForm = this.fb.nonNullable.group({ customerGroupId: 0 });

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const customerId = Number(this.id());
      this.customer = await firstValueFrom(this.customersApi.getCustomerAdmin(customerId));
      this.form.patchValue({
        firstName: this.customer.firstName,
        lastName: this.customer.lastName,
        phoneNumber: this.customer.phoneNumber ?? ''
      });
      this.groupForm.patchValue({ customerGroupId: this.customer.customerGroupId ?? 0 });
      [this.purchaseHistory, this.activity] = await Promise.all([
        firstValueFrom(this.accountApi.getPurchaseHistoryAdmin(customerId)).catch(() => []),
        firstValueFrom(this.accountApi.listActivityAdmin(customerId)).catch(() => [])
      ]);
      if (this.customer.customerGroupId) {
        this.loyalty = await firstValueFrom(this.accountApi.getLoyaltyAdmin(customerId, 1)).catch(() => null);
      }
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

  async assignGroup(): Promise<void> {
    if (!this.customer) return;
    const value = this.groupForm.getRawValue();
    await firstValueFrom(this.accountApi.assignCustomerGroupAdmin(this.customer.id, {
      customerGroupId: value.customerGroupId > 0 ? value.customerGroupId : null
    }));
    await this.load();
  }
}
