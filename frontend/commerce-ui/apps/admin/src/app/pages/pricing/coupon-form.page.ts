import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateCouponRequest,
  DiscountSummary,
  DiscountsApi,
  UpdateCouponRequest
} from '@commerce/api';
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
      { label: ('pricing.coupons.title' | translate), link: '/pricing/coupons' },
      { label: isEdit ? form.code : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.code : ('pricing.coupons.create' | translate) }}</h1>

          @if (!isEdit) {
            <label>{{ 'pricing.coupons.code' | translate }}
              <input [(ngModel)]="form.code" name="code" required autocomplete="off" />
            </label>
            <label>{{ 'pricing.coupons.discount' | translate }}
              <select [(ngModel)]="form.discountId" name="discountId" required>
                <option [ngValue]="0" disabled>{{ 'pricing.coupons.selectDiscount' | translate }}</option>
                @for (discount of discounts; track discount.id) {
                  <option [ngValue]="discount.id">{{ discount.name }}</option>
                }
              </select>
            </label>
          } @else {
            <p>{{ 'pricing.coupons.discount' | translate }}: <strong>{{ discountName }}</strong></p>
          }

          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
            {{ 'pricing.discounts.active' | translate }}
          </label>
          <label>{{ 'pricing.coupons.globalUsageLimit' | translate }}
            <input type="number" [(ngModel)]="form.globalUsageLimit" name="globalUsageLimit" />
          </label>
          <label>{{ 'pricing.coupons.perCustomerUsageLimit' | translate }}
            <input type="number" [(ngModel)]="form.perCustomerUsageLimit" name="perCustomerUsageLimit" />
          </label>
          <label>{{ 'pricing.discounts.startsAt' | translate }}
            <input type="datetime-local" [(ngModel)]="form.startsAtLocal" name="startsAtLocal" />
          </label>
          <label>{{ 'pricing.discounts.endsAt' | translate }}
            <input type="datetime-local" [(ngModel)]="form.endsAtLocal" name="endsAtLocal" />
          </label>
          <label>{{ 'pricing.discounts.storeId' | translate }}
            <input type="number" [(ngModel)]="form.storeId" name="storeId" />
          </label>

          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/pricing/coupons">{{ 'action.cancel' | translate }}</a>
          </div>
        </form>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.75rem; max-width: 36rem; background: #fff; padding: 1rem; border-radius: 0.5rem; }
    label { display: grid; gap: 0.25rem; }
    label.checkbox { display: flex; align-items: center; gap: 0.5rem; }
    input, select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .actions { display: flex; gap: 0.75rem; align-items: center; margin-top: 0.5rem; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer; }
    .btn--primary { background: #2563eb; color: #fff; }
  `]
})
export class CouponFormPageComponent implements OnInit {
  readonly id = input<number | undefined>();

  private readonly discountsApi = inject(DiscountsApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;
  discounts: DiscountSummary[] = [];
  discountName = '';

  form = {
    code: '',
    discountId: 0,
    isActive: true,
    globalUsageLimit: null as number | null,
    perCustomerUsageLimit: null as number | null,
    storeId: null as number | null,
    startsAtLocal: '',
    endsAtLocal: ''
  };

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const couponId = this.id();
      if (!couponId) {
        this.discounts = await firstValueFrom(this.discountsApi.listDiscounts());
      }

      if (couponId) {
        this.isEdit = true;
        const detail = await firstValueFrom(this.discountsApi.getCoupon(couponId));
        this.discountName = detail.discountName;
        this.form = {
          code: detail.code,
          discountId: detail.discountId,
          isActive: detail.isActive,
          globalUsageLimit: detail.globalUsageLimit,
          perCustomerUsageLimit: detail.perCustomerUsageLimit,
          storeId: detail.storeId,
          startsAtLocal: this.toLocalInput(detail.startsAtUtc),
          endsAtLocal: this.toLocalInput(detail.endsAtUtc)
        };
      }

      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load coupon.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      const startsAtUtc = this.fromLocalInput(this.form.startsAtLocal);
      const endsAtUtc = this.fromLocalInput(this.form.endsAtLocal);

      if (this.isEdit && this.id()) {
        const request: UpdateCouponRequest = {
          isActive: this.form.isActive,
          startsAtUtc,
          endsAtUtc,
          storeId: this.form.storeId,
          globalUsageLimit: this.form.globalUsageLimit,
          perCustomerUsageLimit: this.form.perCustomerUsageLimit
        };
        await firstValueFrom(this.discountsApi.updateCoupon(this.id()!, request));
      } else {
        const request: CreateCouponRequest = {
          code: this.form.code.trim(),
          discountId: this.form.discountId,
          isActive: this.form.isActive,
          startsAtUtc,
          endsAtUtc,
          storeId: this.form.storeId,
          globalUsageLimit: this.form.globalUsageLimit,
          perCustomerUsageLimit: this.form.perCustomerUsageLimit
        };
        await firstValueFrom(this.discountsApi.createCoupon(request));
      }
      await this.router.navigateByUrl('/pricing/coupons');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }

  private toLocalInput(value: string | null): string {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  private fromLocalInput(value: string): string | null {
    if (!value) return null;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }
}
