import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AffiliateSummary, AffiliatesApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    BreadcrumbsComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: 'Customers', link: '/customers' },
      { label: 'Affiliates' }
    ]" />
    <header class="page-header">
      <h1>Affiliates</h1>
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state message="No affiliates yet." /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>Referral Code</th>
              <th>Customer ID</th>
              <th>Commission %</th>
              <th>Active</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td><code>{{ item.referralCode }}</code></td>
                <td>{{ item.customerId }}</td>
                <td>{{ item.commissionRatePercent }}%</td>
                <td>{{ item.isActive ? 'Yes' : 'No' }}</td>
                <td>{{ item.createdAtUtc | date:'mediumDate' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `
})
export class AffiliateListPageComponent implements OnInit {
  private readonly api = inject(AffiliatesApi);

  state: PageState = 'loading';
  errorMessage = '';
  items: AffiliateSummary[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const response = await this.api.list();
      this.items = response.data ?? [];
      this.state = this.items.length === 0 ? 'empty' : 'ready';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load affiliates.';
      this.state = 'error';
    }
  }
}
