import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CustomerAccountApi, CustomerSegmentSummary } from '@commerce/api';
import { BreadcrumbsComponent } from '@commerce/layout';
import { LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, BreadcrumbsComponent, LoadingStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: 'Customer Segments' }]" />
    <h1>Customer Segments</h1>
    @if (state === 'loading') { <cmr-loading-state /> } @else {
      <form [formGroup]="form" (ngSubmit)="create()">
        <label>Name<input formControlName="name" /></label>
        <label>Store ID<input type="number" formControlName="storeId" /></label>
        <label>Customer Group ID<input type="number" formControlName="customerGroupId" /></label>
        <button type="submit">Create segment</button>
      </form>
      <ul>
        @for (segment of segments; track segment.id) {
          <li>{{ segment.name }} (store {{ segment.storeId }}) — {{ segment.isActive ? 'Active' : 'Inactive' }}</li>
        }
      </ul>
    }
  `
})
export class SegmentListPageComponent implements OnInit {
  private readonly api = inject(CustomerAccountApi);
  private readonly fb = inject(FormBuilder);
  state: PageState = 'loading';
  segments: CustomerSegmentSummary[] = [];

  readonly form = this.fb.nonNullable.group({
    name: '',
    storeId: 1,
    customerGroupId: 1
  });

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.segments = await firstValueFrom(this.api.listSegmentsAdmin());
    this.state = 'success';
  }

  async create(): Promise<void> {
    const value = this.form.getRawValue();
    await firstValueFrom(this.api.createSegmentAdmin({
      storeId: value.storeId,
      name: value.name,
      rules: [{ ruleType: 'CustomerGroup', customerGroupId: value.customerGroupId }]
    }));
    await this.load();
  }
}
