import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CustomerAccountApi, LoyaltyReward } from '@commerce/api';
import { BreadcrumbsComponent } from '@commerce/layout';
import { LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, BreadcrumbsComponent, LoadingStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: 'Loyalty Rewards' }]" />
    <h1>Loyalty Rewards</h1>
    @if (state === 'loading') { <cmr-loading-state /> } @else {
      <form [formGroup]="form" (ngSubmit)="create()">
        <label>Name<input formControlName="name" /></label>
        <label>Store ID<input type="number" formControlName="storeId" /></label>
        <label>Points cost<input type="number" formControlName="pointsCost" /></label>
        <button type="submit">Create reward</button>
      </form>
      <ul>
        @for (reward of rewards; track reward.id) {
          <li>{{ reward.name }} — {{ reward.pointsCost }} pts</li>
        }
      </ul>
    }
  `
})
export class LoyaltyRewardListPageComponent implements OnInit {
  private readonly api = inject(CustomerAccountApi);
  private readonly fb = inject(FormBuilder);
  state: PageState = 'loading';
  rewards: LoyaltyReward[] = [];

  readonly form = this.fb.nonNullable.group({
    name: '',
    storeId: 1,
    pointsCost: 100
  });

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.rewards = await firstValueFrom(this.api.listRewardsAdmin());
    this.state = 'success';
  }

  async create(): Promise<void> {
    const value = this.form.getRawValue();
    await firstValueFrom(this.api.createRewardAdmin(value));
    await this.load();
  }
}
