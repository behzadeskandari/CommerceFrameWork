import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerAccountApi, LoyaltyAccount, LoyaltyReward, LoyaltyTransaction } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [RouterLink, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>Loyalty & Rewards</h1>
    <p><a routerLink="/account">← Account</a></p>
    @if (state === 'loading') { <cmr-loading-state /> } @else {
      @if (loyalty) {
        <p><strong>Points balance:</strong> {{ loyalty.pointsBalance }}</p>
      }
      <h2>Available rewards</h2>
      @if (!rewards.length) { <p>No rewards available.</p> }
      <ul>
        @for (reward of rewards; track reward.id) {
          <li>
            {{ reward.name }} — {{ reward.pointsCost }} pts
            <button type="button" (click)="redeem(reward.id)" [disabled]="redeeming">Redeem</button>
          </li>
        }
      </ul>
      <h2>Recent transactions</h2>
      <ul>
        @for (tx of transactions; track tx.id) {
          <li>{{ tx.type }}: {{ tx.pointsDelta }} (balance {{ tx.balanceAfter }})</li>
        }
      </ul>
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
    }
  `
})
export class AccountLoyaltyPageComponent implements OnInit {
  private readonly api = inject(CustomerAccountApi);

  state: PageState = 'loading';
  errorMessage = '';
  redeeming = false;
  loyalty: LoyaltyAccount | null = null;
  rewards: LoyaltyReward[] = [];
  transactions: LoyaltyTransaction[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    try {
      [this.loyalty, this.rewards, this.transactions] = await Promise.all([
        firstValueFrom(this.api.getLoyalty()),
        firstValueFrom(this.api.listRewards()),
        firstValueFrom(this.api.listLoyaltyTransactions())
      ]);
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load loyalty.';
      this.state = 'error';
    }
  }

  async redeem(rewardId: number): Promise<void> {
    this.redeeming = true;
    try {
      await firstValueFrom(this.api.redeemReward({ rewardId }, crypto.randomUUID()));
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Redemption failed.';
    } finally {
      this.redeeming = false;
    }
  }
}
