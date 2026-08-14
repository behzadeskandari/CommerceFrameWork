import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerAccountApi, CustomerActivity, StoreCreditAccount } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [RouterLink, DecimalPipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>Activity & Store Credit</h1>
    <p><a routerLink="/account">← Account</a></p>
    @if (state === 'loading') { <cmr-loading-state /> } @else {
      @if (storeCredit) {
        <p><strong>Store credit:</strong> {{ storeCredit.balance | number:'1.2-2' }} {{ storeCredit.currencyCode }}</p>
      }
      <h2>Recent activity</h2>
      @if (!activity.length) { <p>No activity yet.</p> }
      <ul>
        @for (item of activity; track item.id) {
          <li>{{ item.createdAtUtc }} — {{ item.summary }}</li>
        }
      </ul>
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `
})
export class AccountActivityPageComponent implements OnInit {
  private readonly api = inject(CustomerAccountApi);

  state: PageState = 'loading';
  errorMessage = '';
  storeCredit: StoreCreditAccount | null = null;
  activity: CustomerActivity[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    try {
      [this.storeCredit, this.activity] = await Promise.all([
        firstValueFrom(this.api.getStoreCredit()).catch(() => null),
        firstValueFrom(this.api.listActivity())
      ]);
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load activity.';
      this.state = 'error';
    }
  }
}
