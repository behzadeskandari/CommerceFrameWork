import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdvancedPricingApi, CustomerGroup } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'pricing.customerGroups' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <form [formGroup]="form" (ngSubmit)="create()" class="inline-form">
          <input formControlName="name" [placeholder]="'common.name' | translate" />
          <input formControlName="code" placeholder="Code" />
          <button type="submit">{{ 'action.create' | translate }}</button>
        </form>
        <table>
          <thead><tr><th>Name</th><th>Code</th><th>Active</th><th></th></tr></thead>
          <tbody>
            @for (group of groups; track group.id) {
              <tr>
                <td>{{ group.name }}</td>
                <td>{{ group.code }}</td>
                <td>{{ group.isActive ? 'Yes' : 'No' }}</td>
                <td><button type="button" (click)="remove(group.id)">{{ 'action.delete' | translate }}</button></td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .inline-form { display: flex; gap: 0.5rem; margin-bottom: 1rem; flex-wrap: wrap; }
    table { width: 100%; border-collapse: collapse; }
    th, td { border: 1px solid #e5e7eb; padding: 0.5rem; text-align: start; }
  `]
})
export class CustomerGroupListPageComponent implements OnInit {
  private readonly api = inject(AdvancedPricingApi);
  private readonly fb = inject(FormBuilder);
  state: PageState = 'loading';
  errorMessage = '';
  groups: CustomerGroup[] = [];
  readonly form = this.fb.group({
    name: ['', Validators.required],
    code: ['', Validators.required]
  });

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.groups = await firstValueFrom(this.api.listCustomerGroups());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load customer groups.';
      this.state = 'error';
    }
  }

  async create(): Promise<void> {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    try {
      await firstValueFrom(this.api.createCustomerGroup({
        storeId: 1,
        name: value.name!,
        code: value.code!,
        isActive: true,
        displayOrder: this.groups.length
      }));
      this.form.reset();
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to create customer group.';
      this.state = 'error';
    }
  }

  async remove(id: number): Promise<void> {
    try {
      await firstValueFrom(this.api.deleteCustomerGroup(id));
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to delete customer group.';
      this.state = 'error';
    }
  }
}
