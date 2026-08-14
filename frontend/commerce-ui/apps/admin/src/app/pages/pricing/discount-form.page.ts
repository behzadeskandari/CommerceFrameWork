import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateDiscountRequest,
  DiscountTarget,
  DiscountTargetType,
  DiscountsApi,
  UpdateDiscountRequest
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
      { label: ('pricing.discounts.title' | translate), link: '/pricing/discounts' },
      { label: isEdit ? form.name : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.name : ('pricing.discounts.create' | translate) }}</h1>

          @if (!isEdit) {
            <label>{{ 'pricing.discounts.systemName' | translate }}
              <input [(ngModel)]="form.systemName" name="systemName" required />
            </label>
          }
          <label>{{ 'pricing.discounts.name' | translate }}
            <input [(ngModel)]="form.name" name="name" required />
          </label>
          <label>{{ 'pricing.discounts.description' | translate }}
            <textarea [(ngModel)]="form.description" name="description" rows="3"></textarea>
          </label>
          <label>{{ 'pricing.discounts.type' | translate }}
            <select [(ngModel)]="form.discountType" name="discountType" required>
              <option value="Percentage">{{ 'pricing.discounts.percentage' | translate }}</option>
              <option value="FixedAmount">{{ 'pricing.discounts.fixedAmount' | translate }}</option>
            </select>
          </label>
          <label>{{ 'pricing.discounts.value' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.value" name="value" required />
          </label>
          @if (form.discountType === 'FixedAmount') {
            <label>{{ 'pricing.discounts.currency' | translate }}
              <input [(ngModel)]="form.currencyCode" name="currencyCode" />
            </label>
          }
          <label>{{ 'pricing.discounts.priority' | translate }}
            <input type="number" [(ngModel)]="form.priority" name="priority" required />
          </label>
          <label>{{ 'pricing.discounts.scope' | translate }}
            <select [(ngModel)]="form.applicationScope" name="applicationScope" required>
              <option value="Line">{{ 'pricing.discounts.scopeLine' | translate }}</option>
              <option value="Cart">{{ 'pricing.discounts.scopeCart' | translate }}</option>
            </select>
          </label>
          <label>{{ 'pricing.discounts.stackingMode' | translate }}
            <select [(ngModel)]="form.stackingMode" name="stackingMode" required>
              <option value="NonStackable">{{ 'pricing.discounts.nonStackable' | translate }}</option>
              <option value="Stackable">{{ 'pricing.discounts.stackable' | translate }}</option>
            </select>
          </label>
          <label>{{ 'pricing.discounts.customerEligibility' | translate }}
            <select [(ngModel)]="form.customerEligibility" name="customerEligibility" required>
              <option value="All">{{ 'pricing.discounts.eligibilityAll' | translate }}</option>
              <option value="Authenticated">{{ 'pricing.discounts.eligibilityAuthenticated' | translate }}</option>
              <option value="Guest">{{ 'pricing.discounts.eligibilityGuest' | translate }}</option>
              <option value="SpecificCustomer">{{ 'pricing.discounts.eligibilitySpecific' | translate }}</option>
            </select>
          </label>
          @if (form.customerEligibility === 'SpecificCustomer') {
            <label>{{ 'pricing.discounts.specificCustomerId' | translate }}
              <input type="number" [(ngModel)]="form.specificCustomerId" name="specificCustomerId" />
            </label>
          }
          <label>{{ 'pricing.discounts.minimumCartSubtotal' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.minimumCartSubtotal" name="minimumCartSubtotal" />
          </label>
          <label>{{ 'pricing.discounts.minimumQuantity' | translate }}
            <input type="number" [(ngModel)]="form.minimumQuantity" name="minimumQuantity" />
          </label>
          <label>{{ 'pricing.discounts.maximumDiscountAmount' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.maximumDiscountAmount" name="maximumDiscountAmount" />
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
          @if (!isEdit) {
            <label class="checkbox">
              <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
              {{ 'pricing.discounts.active' | translate }}
            </label>
          }

          <section class="targets">
            <h2>{{ 'pricing.discounts.targets' | translate }}</h2>
            @for (target of targets; track $index; let i = $index) {
              <div class="target-row">
                <select [(ngModel)]="target.targetType" [name]="'targetType-' + i">
                  @for (type of targetTypes; track type) {
                    <option [value]="type">{{ targetTypeLabel(type) | translate }}</option>
                  }
                </select>
                <input type="number" [(ngModel)]="target.targetId" [name]="'targetId-' + i" placeholder="ID" />
                <button type="button" (click)="removeTarget(i)">{{ 'action.delete' | translate }}</button>
              </div>
            }
            <button type="button" class="secondary" (click)="addTarget()">{{ 'pricing.discounts.addTarget' | translate }}</button>
          </section>

          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/pricing/discounts">{{ 'action.cancel' | translate }}</a>
          </div>
        </form>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.75rem; max-width: 40rem; background: #fff; padding: 1rem; border-radius: 0.5rem; }
    label { display: grid; gap: 0.25rem; }
    label.checkbox { display: flex; align-items: center; gap: 0.5rem; }
    input, select, textarea { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .targets { display: grid; gap: 0.5rem; margin-top: 0.5rem; }
    .target-row { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
    .target-row button { background: none; border: none; color: #dc2626; cursor: pointer; }
    .actions { display: flex; gap: 0.75rem; align-items: center; margin-top: 0.5rem; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer; }
    .btn--primary { background: #2563eb; color: #fff; }
    button.secondary { width: fit-content; padding: 0.375rem 0.75rem; border: 1px solid #d1d5db; background: #fff; border-radius: 0.375rem; cursor: pointer; }
  `]
})
export class DiscountFormPageComponent implements OnInit {
  readonly id = input<number | undefined>();

  private readonly discountsApi = inject(DiscountsApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;
  readonly targetTypes: DiscountTargetType[] = ['Product', 'Variant', 'Offer', 'Category', 'Cart'];
  targets: DiscountTarget[] = [];

  form = {
    systemName: '',
    name: '',
    description: '' as string | null,
    discountType: 'Percentage' as CreateDiscountRequest['discountType'],
    value: 0,
    currencyCode: null as string | null,
    priority: 0,
    isActive: true,
    applicationScope: 'Cart' as CreateDiscountRequest['applicationScope'],
    stackingMode: 'NonStackable' as CreateDiscountRequest['stackingMode'],
    customerEligibility: 'All' as CreateDiscountRequest['customerEligibility'],
    specificCustomerId: null as number | null,
    minimumCartSubtotal: null as number | null,
    minimumQuantity: null as number | null,
    maximumDiscountAmount: null as number | null,
    storeId: null as number | null,
    startsAtLocal: '',
    endsAtLocal: ''
  };

  ngOnInit(): void {
    void this.load();
  }

  targetTypeLabel(type: DiscountTargetType): string {
    const map: Record<DiscountTargetType, string> = {
      Product: 'pricing.discounts.targetProduct',
      Variant: 'pricing.discounts.targetVariant',
      Offer: 'pricing.discounts.targetOffer',
      Category: 'pricing.discounts.targetCategory',
      Cart: 'pricing.discounts.targetCart'
    };
    return map[type];
  }

  addTarget(): void {
    this.targets.push({ targetType: 'Cart', targetId: 0 });
  }

  removeTarget(index: number): void {
    this.targets.splice(index, 1);
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const discountId = this.id();
      if (discountId) {
        this.isEdit = true;
        const detail = await firstValueFrom(this.discountsApi.getDiscount(discountId));
        this.form = {
          systemName: detail.systemName,
          name: detail.name,
          description: detail.description,
          discountType: detail.discountType,
          value: detail.value,
          currencyCode: detail.currencyCode,
          priority: detail.priority,
          isActive: detail.isActive,
          applicationScope: detail.applicationScope,
          stackingMode: detail.stackingMode,
          customerEligibility: detail.customerEligibility,
          specificCustomerId: detail.specificCustomerId,
          minimumCartSubtotal: detail.minimumCartSubtotal,
          minimumQuantity: detail.minimumQuantity,
          maximumDiscountAmount: detail.maximumDiscountAmount,
          storeId: detail.storeId,
          startsAtLocal: this.toLocalInput(detail.startsAtUtc),
          endsAtLocal: this.toLocalInput(detail.endsAtUtc)
        };
        this.targets = detail.targets.map(t => ({ ...t }));
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load discount.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      const startsAtUtc = this.fromLocalInput(this.form.startsAtLocal);
      const endsAtUtc = this.fromLocalInput(this.form.endsAtLocal);
      const targets = this.targets.filter(t => t.targetId > 0 || t.targetType === 'Cart');

      if (this.isEdit && this.id()) {
        const request: UpdateDiscountRequest = {
          name: this.form.name,
          description: this.form.description,
          discountType: this.form.discountType,
          value: this.form.value,
          currencyCode: this.form.currencyCode,
          priority: this.form.priority,
          startsAtUtc,
          endsAtUtc,
          storeId: this.form.storeId,
          stackingMode: this.form.stackingMode,
          maximumDiscountAmount: this.form.maximumDiscountAmount,
          minimumCartSubtotal: this.form.minimumCartSubtotal,
          minimumQuantity: this.form.minimumQuantity,
          customerEligibility: this.form.customerEligibility,
          specificCustomerId: this.form.specificCustomerId,
          applicationScope: this.form.applicationScope,
          targets
        };
        await firstValueFrom(this.discountsApi.updateDiscount(this.id()!, request));
      } else {
        const request: CreateDiscountRequest = {
          name: this.form.name,
          systemName: this.form.systemName,
          description: this.form.description,
          discountType: this.form.discountType,
          value: this.form.value,
          currencyCode: this.form.currencyCode,
          priority: this.form.priority,
          isActive: this.form.isActive,
          startsAtUtc,
          endsAtUtc,
          storeId: this.form.storeId,
          stackingMode: this.form.stackingMode,
          maximumDiscountAmount: this.form.maximumDiscountAmount,
          minimumCartSubtotal: this.form.minimumCartSubtotal,
          minimumQuantity: this.form.minimumQuantity,
          customerEligibility: this.form.customerEligibility,
          specificCustomerId: this.form.specificCustomerId,
          applicationScope: this.form.applicationScope,
          targets
        };
        await firstValueFrom(this.discountsApi.createDiscount(request));
      }
      await this.router.navigateByUrl('/pricing/discounts');
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
