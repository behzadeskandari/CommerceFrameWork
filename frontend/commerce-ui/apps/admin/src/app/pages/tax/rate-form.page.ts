import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateTaxRateRequest,
  TaxApi,
  TaxCategorySummary,
  TaxRateType,
  TaxZoneSummary,
  UpdateTaxRateRequest
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
      { label: ('tax.rates.title' | translate), link: '/tax/rates' },
      { label: isEdit ? ('action.edit' | translate) : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? ('tax.rates.edit' | translate) : ('tax.rates.create' | translate) }}</h1>

          @if (!isEdit) {
            <label>{{ 'tax.storeId' | translate }}
              <input type="number" [(ngModel)]="form.storeId" name="storeId" required />
            </label>
            <label>{{ 'tax.rates.category' | translate }}
              <select [(ngModel)]="form.taxCategoryId" name="taxCategoryId" required>
                <option [ngValue]="null" disabled>{{ 'tax.rates.selectCategory' | translate }}</option>
                @for (category of categories; track category.id) {
                  <option [ngValue]="category.id">{{ category.name }}</option>
                }
              </select>
            </label>
            <label>{{ 'tax.rates.zone' | translate }}
              <select [(ngModel)]="form.taxZoneId" name="taxZoneId">
                <option [ngValue]="null">{{ 'tax.rates.anyZone' | translate }}</option>
                @for (zone of zones; track zone.id) {
                  <option [ngValue]="zone.id">{{ zone.name }}</option>
                }
              </select>
            </label>
            <label>{{ 'tax.rates.rateType' | translate }}
              <select [(ngModel)]="form.rateType" name="rateType" required>
                @for (type of rateTypes; track type) {
                  <option [value]="type">{{ rateTypeLabel(type) | translate }}</option>
                }
              </select>
            </label>
          }
          <label>{{ 'tax.rates.percentage' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.percentage" name="percentage" required />
          </label>
          <label>{{ 'tax.rates.priority' | translate }}
            <input type="number" [(ngModel)]="form.priority" name="priority" required />
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.taxShipping" name="taxShipping" />
            {{ 'tax.rates.taxShipping' | translate }}
          </label>
          @if (isEdit) {
            <label class="checkbox">
              <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
              {{ 'tax.active' | translate }}
            </label>
          }

          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/tax/rates">{{ 'action.cancel' | translate }}</a>
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
    .actions { display: flex; gap: 0.75rem; align-items: center; margin-top: 0.5rem; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer; }
    .btn--primary { background: #2563eb; color: #fff; }
  `]
})
export class RateFormPageComponent implements OnInit {
  readonly id = input<number | undefined>();

  private readonly taxApi = inject(TaxApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;
  categories: TaxCategorySummary[] = [];
  zones: TaxZoneSummary[] = [];
  readonly rateTypes: TaxRateType[] = ['Percentage'];

  form = {
    storeId: 1,
    taxCategoryId: null as number | null,
    taxZoneId: null as number | null,
    rateType: 'Percentage' as TaxRateType,
    percentage: 0,
    taxShipping: false,
    priority: 0,
    isActive: true
  };

  ngOnInit(): void {
    void this.load();
  }

  rateTypeLabel(type: TaxRateType): string {
    const map: Record<TaxRateType, string> = {
      Percentage: 'tax.rates.typePercentage',
      Fixed: 'tax.rates.typeFixed'
    };
    return map[type];
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const [categories, zones] = await Promise.all([
        firstValueFrom(this.taxApi.listCategories()),
        firstValueFrom(this.taxApi.listZones())
      ]);
      this.categories = categories;
      this.zones = zones;

      const rateId = this.id();
      if (rateId) {
        this.isEdit = true;
        const detail = await firstValueFrom(this.taxApi.getRate(rateId));
        this.form = {
          storeId: detail.storeId,
          taxCategoryId: detail.taxCategoryId,
          taxZoneId: detail.taxZoneId,
          rateType: detail.rateType,
          percentage: detail.percentage,
          taxShipping: detail.taxShipping,
          priority: detail.priority,
          isActive: detail.isActive
        };
      } else if (categories.length > 0) {
        this.form.taxCategoryId = categories[0].id;
      }

      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load tax rate.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      if (this.isEdit && this.id()) {
        const request: UpdateTaxRateRequest = {
          percentage: this.form.percentage,
          taxShipping: this.form.taxShipping,
          priority: this.form.priority,
          isActive: this.form.isActive
        };
        await firstValueFrom(this.taxApi.updateRate(this.id()!, request));
      } else {
        if (this.form.taxCategoryId == null) {
          this.errorMessage = 'Tax category is required.';
          this.state = 'error';
          return;
        }
        const request: CreateTaxRateRequest = {
          storeId: this.form.storeId,
          taxCategoryId: this.form.taxCategoryId,
          taxZoneId: this.form.taxZoneId,
          rateType: this.form.rateType,
          percentage: this.form.percentage,
          taxShipping: this.form.taxShipping,
          priority: this.form.priority
        };
        await firstValueFrom(this.taxApi.createRate(request));
      }
      await this.router.navigateByUrl('/tax/rates');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }
}
