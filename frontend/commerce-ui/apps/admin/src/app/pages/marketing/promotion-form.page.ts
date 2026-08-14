import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CreatePromotionRequest, PromotionCombinationRule, PromotionsApi, UpdatePromotionRequest } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('promotions.title' | translate), link: '/marketing/promotions' },
      { label: isEdit ? form.name : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.name : ('promotions.create' | translate) }}</h1>
          @if (!isEdit) {
            <label>{{ 'promotions.systemName' | translate }}
              <input [(ngModel)]="form.systemName" name="systemName" required />
            </label>
          }
          <label>{{ 'promotions.name' | translate }}<input [(ngModel)]="form.name" name="name" required /></label>
          <label>{{ 'promotions.description' | translate }}
            <textarea [(ngModel)]="form.description" name="description" rows="3"></textarea>
          </label>
          <label>{{ 'promotions.priority' | translate }}<input type="number" [(ngModel)]="form.priority" name="priority" /></label>
          <label>{{ 'promotions.combinationRule' | translate }}
            <select [(ngModel)]="form.combinationRule" name="combinationRule">
              <option value="Exclusive">Exclusive</option>
              <option value="Stackable">Stackable</option>
              <option value="SameGroupExclusive">SameGroupExclusive</option>
            </select>
          </label>
          <label>{{ 'promotions.combinationGroup' | translate }}
            <input [(ngModel)]="form.combinationGroup" name="combinationGroup" />
          </label>
          <label>{{ 'promotions.storeId' | translate }}
            <input type="number" [(ngModel)]="form.storeId" name="storeId" />
          </label>
          <label>{{ 'promotions.globalUsageLimit' | translate }}
            <input type="number" [(ngModel)]="form.globalUsageLimit" name="globalUsageLimit" />
          </label>
          <label>{{ 'promotions.perCustomerUsageLimit' | translate }}
            <input type="number" [(ngModel)]="form.perCustomerUsageLimit" name="perCustomerUsageLimit" />
          </label>
          <label><input type="checkbox" [(ngModel)]="form.isActive" name="isActive" /> {{ 'promotions.active' | translate }}</label>
          <label><input type="checkbox" [(ngModel)]="form.requiresCouponCode" name="requiresCouponCode" /> {{ 'promotions.requiresCoupon' | translate }}</label>
          @if (form.requiresCouponCode) {
            <label>{{ 'promotions.couponCode' | translate }}<input [(ngModel)]="form.couponCode" name="couponCode" /></label>
          }
          <label>{{ 'promotions.startsAt' | translate }}
            <input type="datetime-local" [(ngModel)]="form.startsAtUtc" name="startsAtUtc" />
          </label>
          <label>{{ 'promotions.endsAt' | translate }}
            <input type="datetime-local" [(ngModel)]="form.endsAtUtc" name="endsAtUtc" />
          </label>
          <fieldset>
            <legend>{{ 'promotions.conditions' | translate }}</legend>
            <label>{{ 'promotions.minCartSubtotal' | translate }}
              <input type="number" [(ngModel)]="minCartSubtotal" name="minCartSubtotal" />
            </label>
          </fieldset>
          <fieldset>
            <legend>{{ 'promotions.actions' | translate }}</legend>
            <label>{{ 'promotions.discountPercent' | translate }}
              <input type="number" [(ngModel)]="discountPercent" name="discountPercent" />
            </label>
            <label>{{ 'promotions.actionScope' | translate }}
              <select [(ngModel)]="actionScope" name="actionScope">
                <option value="Cart">Cart</option>
                <option value="Line">Line</option>
              </select>
            </label>
          </fieldset>
          <div class="actions">
            <button type="submit">{{ 'action.save' | translate }}</button>
            <a routerLink="/marketing/promotions">{{ 'action.cancel' | translate }}</a>
          </div>
        </form>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.875rem; max-width: 40rem; }
    label { display: grid; gap: 0.25rem; }
    fieldset { border: 1px solid #e5e7eb; padding: 0.875rem; border-radius: 0.375rem; }
    .actions { display: flex; gap: 0.75rem; align-items: center; }
  `]
})
export class PromotionFormPageComponent implements OnInit {
  readonly id = input<string | undefined>();
  private readonly api = inject(PromotionsApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;
  minCartSubtotal: number | null = null;
  discountPercent = 10;
  actionScope: 'Cart' | 'Line' = 'Cart';

  form = {
    name: '',
    systemName: '',
    description: '',
    isActive: true,
    priority: 50,
    combinationRule: 'Stackable' as PromotionCombinationRule,
    combinationGroup: '',
    storeId: null as number | null,
    globalUsageLimit: null as number | null,
    perCustomerUsageLimit: null as number | null,
    requiresCouponCode: false,
    couponCode: '',
    startsAtUtc: '',
    endsAtUtc: ''
  };

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    const rawId = this.id();
    if (!rawId || rawId === 'new') {
      this.state = 'success';
      return;
    }

    this.isEdit = true;
    this.state = 'loading';
    try {
      const detail = await firstValueFrom(this.api.get(Number(rawId)));
      this.form = {
        name: detail.name,
        systemName: detail.systemName,
        description: detail.description ?? '',
        isActive: detail.isActive,
        priority: detail.priority,
        combinationRule: detail.combinationRule,
        combinationGroup: detail.combinationGroup ?? '',
        storeId: detail.storeId,
        globalUsageLimit: detail.globalUsageLimit,
        perCustomerUsageLimit: detail.perCustomerUsageLimit,
        requiresCouponCode: detail.requiresCouponCode,
        couponCode: detail.couponCode ?? '',
        startsAtUtc: detail.startsAtUtc ?? '',
        endsAtUtc: detail.endsAtUtc ?? ''
      };
      const minCondition = detail.conditions.find(c => c.conditionType === 'MinimumCartSubtotal');
      if (minCondition) {
        const parsed = JSON.parse(minCondition.parametersJson) as { minimum?: number };
        this.minCartSubtotal = parsed.minimum ?? null;
      }
      const percentAction = detail.actions.find(a => a.actionType === 'PercentageDiscount');
      if (percentAction) {
        const parsed = JSON.parse(percentAction.parametersJson) as { percent?: number };
        this.discountPercent = parsed.percent ?? 10;
        this.actionScope = percentAction.targetScope;
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load promotion.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    const conditions = this.minCartSubtotal != null && this.minCartSubtotal > 0
      ? [{ conditionType: 'MinimumCartSubtotal' as const, parametersJson: JSON.stringify({ minimum: this.minCartSubtotal }) }]
      : [];

    const actions = [{
      actionType: 'PercentageDiscount' as const,
      targetScope: this.actionScope,
      parametersJson: JSON.stringify({ percent: this.discountPercent })
    }];

    const payload = {
      name: this.form.name,
      description: this.form.description || null,
      isActive: this.form.isActive,
      startsAtUtc: this.form.startsAtUtc || null,
      endsAtUtc: this.form.endsAtUtc || null,
      storeId: this.form.storeId,
      priority: this.form.priority,
      combinationRule: this.form.combinationRule,
      combinationGroup: this.form.combinationGroup || null,
      globalUsageLimit: this.form.globalUsageLimit,
      perCustomerUsageLimit: this.form.perCustomerUsageLimit,
      requiresCouponCode: this.form.requiresCouponCode,
      couponCode: this.form.couponCode || null,
      conditions,
      actions
    };

    try {
      if (this.isEdit) {
        await firstValueFrom(this.api.update(Number(this.id()), payload as UpdatePromotionRequest));
      } else {
        await firstValueFrom(this.api.create({ ...payload, systemName: this.form.systemName } as CreatePromotionRequest));
      }
      await this.router.navigateByUrl('/marketing/promotions');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to save promotion.';
      this.state = 'error';
    }
  }
}
