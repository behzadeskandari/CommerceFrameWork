import { Component, OnInit, inject } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GiftCardSummary, GiftCardsApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DecimalPipe,
    DatePipe,
    BreadcrumbsComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: 'Gift Cards' }
    ]" />
    <header class="page-header">
      <h1>Gift Cards</h1>
    </header>

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state message="No gift cards yet." /> }
      @default {
        <table>
          <thead>
            <tr>
              <th>Code</th>
              <th>Balance</th>
              <th>Initial</th>
              <th>Currency</th>
              <th>Active</th>
              <th>Expires</th>
            </tr>
          </thead>
          <tbody>
            @for (item of items; track item.id) {
              <tr>
                <td><code>{{ item.code }}</code></td>
                <td>{{ item.balance | number:'1.2-2' }}</td>
                <td>{{ item.initialAmount | number:'1.2-2' }}</td>
                <td>{{ item.currencyCode }}</td>
                <td>{{ item.isActive ? 'Yes' : 'No' }}</td>
                <td>{{ item.expiresAtUtc ? (item.expiresAtUtc | date:'mediumDate') : '—' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    }
  `
})
export class GiftCardListPageComponent implements OnInit {
  private readonly api = inject(GiftCardsApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: GiftCardSummary[] = [];

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
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load gift cards.';
      this.state = 'error';
    }
  }
}
