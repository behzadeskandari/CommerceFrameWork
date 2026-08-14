import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateShippingRateRequest,
  ShippingApi,
  ShippingMethodSummary,
  ShippingRateType,
  ShippingZoneSummary,
  UpdateShippingRateRequest
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
      { label: ('shipping.rates.title' | translate), link: '/shipping/rates' },
      { label: isEdit ? ('action.edit' | translate) : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? ('shipping.rates.edit' | translate) : ('shipping.rates.create' | translate) }}</h1>

          @if (!isEdit) {
            <label>{{ 'shipping.storeId' | translate }}
              <input type="number" [(ngModel)]="form.storeId" name="storeId" required />
            </label>
            <label>{{ 'shipping.rates.method' | translate }}
              <select [(ngModel)]="form.shippingMethodId" name="shippingMethodId" required>
                <option [ngValue]="null" disabled>{{ 'shipping.rates.selectMethod' | translate }}</option>
                @for (method of methods; track method.id) {
                  <option [ngValue]="method.id">{{ method.name }}</option>
                }
              </select>
            </label>
            <label>{{ 'shipping.rates.zone' | translate }}
              <select [(ngModel)]="form.shippingZoneId" name="shippingZoneId">
                <option [ngValue]="null">{{ 'shipping.rates.anyZone' | translate }}</option>
                @for (zone of zones; track zone.id) {
                  <option [ngValue]="zone.id">{{ zone.name }}</option>
                }
              </select>
            </label>
            <label>{{ 'shipping.rates.currency' | translate }}
              <input [(ngModel)]="form.currencyCode" name="currencyCode" required />
            </label>
            <label>{{ 'shipping.rates.rateType' | translate }}
              <select [(ngModel)]="form.rateType" name="rateType" required>
                @for (type of rateTypes; track type) {
                  <option [value]="type">{{ rateTypeLabel(type) | translate }}</option>
                }
              </select>
            </label>
          }
          <label>{{ 'shipping.rates.basePrice' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.basePrice" name="basePrice" required />
          </label>
          <label>{{ 'shipping.rates.pricePerWeightUnit' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.pricePerWeightUnit" name="pricePerWeightUnit" />
          </label>
          <label>{{ 'shipping.rates.freeShippingThreshold' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.freeShippingThreshold" name="freeShippingThreshold" />
          </label>
          <label>{{ 'shipping.rates.minOrderSubtotal' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.minOrderSubtotal" name="minOrderSubtotal" />
          </label>
          <label>{{ 'shipping.rates.maxOrderSubtotal' | translate }}
            <input type="number" step="0.01" [(ngModel)]="form.maxOrderSubtotal" name="maxOrderSubtotal" />
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
            {{ 'shipping.active' | translate }}
          </label>

          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/shipping/rates">{{ 'action.cancel' | translate }}</a>
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

  private readonly shippingApi = inject(ShippingApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;
  methods: ShippingMethodSummary[] = [];
  zones: ShippingZoneSummary[] = [];
  readonly rateTypes: ShippingRateType[] = ['Flat', 'WeightBased', 'OrderSubtotalBased', 'QuantityBased'];

  form = {
    storeId: 1,
    shippingMethodId: null as number | null,
    shippingZoneId: null as number | null,
    currencyCode: 'USD',
    rateType: 'Flat' as ShippingRateType,
    basePrice: 0,
    pricePerWeightUnit: null as number | null,
    freeShippingThreshold: null as number | null,
    minOrderSubtotal: null as number | null,
    maxOrderSubtotal: null as number | null,
    isActive: true
  };

  ngOnInit(): void {
    void this.load();
  }

  rateTypeLabel(type: ShippingRateType): string {
    const map: Record<ShippingRateType, string> = {
      Flat: 'shipping.rates.typeFlat',
      WeightBased: 'shipping.rates.typeWeightBased',
      OrderSubtotalBased: 'shipping.rates.typeOrderSubtotalBased',
      QuantityBased: 'shipping.rates.typeQuantityBased'
    };
    return map[type];
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const [methods, zones] = await Promise.all([
        firstValueFrom(this.shippingApi.listMethods()),
        firstValueFrom(this.shippingApi.listZones())
      ]);
      this.methods = methods;
      this.zones = zones;

      const rateId = this.id();
      if (rateId) {
        this.isEdit = true;
        const detail = await firstValueFrom(this.shippingApi.getRate(rateId));
        this.form = {
          storeId: detail.storeId,
          shippingMethodId: detail.shippingMethodId,
          shippingZoneId: detail.shippingZoneId,
          currencyCode: detail.currencyCode,
          rateType: detail.rateType,
          basePrice: detail.basePrice,
          pricePerWeightUnit: detail.pricePerWeightUnit,
          freeShippingThreshold: detail.freeShippingThreshold,
          minOrderSubtotal: detail.minOrderSubtotal,
          maxOrderSubtotal: detail.maxOrderSubtotal,
          isActive: detail.isActive
        };
      } else if (methods.length > 0) {
        this.form.shippingMethodId = methods[0].id;
      }

      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load shipping rate.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      if (this.isEdit && this.id()) {
        const request: UpdateShippingRateRequest = {
          basePrice: this.form.basePrice,
          pricePerWeightUnit: this.form.pricePerWeightUnit,
          freeShippingThreshold: this.form.freeShippingThreshold,
          minOrderSubtotal: this.form.minOrderSubtotal,
          maxOrderSubtotal: this.form.maxOrderSubtotal,
          isActive: this.form.isActive
        };
        await firstValueFrom(this.shippingApi.updateRate(this.id()!, request));
      } else {
        if (this.form.shippingMethodId == null) {
          this.errorMessage = 'Shipping method is required.';
          this.state = 'error';
          return;
        }
        const request: CreateShippingRateRequest = {
          storeId: this.form.storeId,
          shippingMethodId: this.form.shippingMethodId,
          shippingZoneId: this.form.shippingZoneId,
          currencyCode: this.form.currencyCode,
          rateType: this.form.rateType,
          basePrice: this.form.basePrice,
          pricePerWeightUnit: this.form.pricePerWeightUnit,
          freeShippingThreshold: this.form.freeShippingThreshold,
          minOrderSubtotal: this.form.minOrderSubtotal,
          maxOrderSubtotal: this.form.maxOrderSubtotal
        };
        await firstValueFrom(this.shippingApi.createRate(request));
      }
      await this.router.navigateByUrl('/shipping/rates');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }
}
