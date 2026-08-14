import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateShippingMethodRequest,
  SHIPPING_PROVIDER_FLAT_RATE,
  ShippingApi,
  UpdateShippingMethodRequest
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
      { label: ('shipping.methods.title' | translate), link: '/shipping/methods' },
      { label: isEdit ? form.name : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.name : ('shipping.methods.create' | translate) }}</h1>

          @if (!isEdit) {
            <label>{{ 'shipping.methods.systemName' | translate }}
              <input [(ngModel)]="form.systemName" name="systemName" required />
            </label>
            <label>{{ 'shipping.storeId' | translate }}
              <input type="number" [(ngModel)]="form.storeId" name="storeId" required />
            </label>
            <label>{{ 'shipping.methods.provider' | translate }}
              <input [(ngModel)]="form.providerSystemName" name="providerSystemName" required />
            </label>
          }
          <label>{{ 'shipping.methods.name' | translate }}
            <input [(ngModel)]="form.name" name="name" required />
          </label>
          <label>{{ 'shipping.methods.description' | translate }}
            <textarea [(ngModel)]="form.description" name="description" rows="3"></textarea>
          </label>
          <label>{{ 'shipping.displayOrder' | translate }}
            <input type="number" [(ngModel)]="form.displayOrder" name="displayOrder" required />
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
            {{ 'shipping.active' | translate }}
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.requiresAddress" name="requiresAddress" />
            {{ 'shipping.methods.requiresAddress' | translate }}
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.supportsTracking" name="supportsTracking" />
            {{ 'shipping.methods.supportsTracking' | translate }}
          </label>
          <label>{{ 'shipping.methods.estimatedDeliveryMin' | translate }}
            <input type="number" [(ngModel)]="form.estimatedDeliveryDaysMin" name="estimatedDeliveryDaysMin" />
          </label>
          <label>{{ 'shipping.methods.estimatedDeliveryMax' | translate }}
            <input type="number" [(ngModel)]="form.estimatedDeliveryDaysMax" name="estimatedDeliveryDaysMax" />
          </label>

          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/shipping/methods">{{ 'action.cancel' | translate }}</a>
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
export class MethodFormPageComponent implements OnInit {
  readonly id = input<number | undefined>();

  private readonly shippingApi = inject(ShippingApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;

  form = {
    storeId: 1,
    systemName: '',
    name: '',
    description: '' as string | null,
    providerSystemName: SHIPPING_PROVIDER_FLAT_RATE,
    isActive: true,
    displayOrder: 0,
    requiresAddress: true,
    supportsTracking: false,
    estimatedDeliveryDaysMin: null as number | null,
    estimatedDeliveryDaysMax: null as number | null
  };

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const methodId = this.id();
      if (methodId) {
        this.isEdit = true;
        const detail = await firstValueFrom(this.shippingApi.getMethod(methodId));
        this.form = {
          storeId: detail.storeId,
          systemName: detail.systemName,
          name: detail.name,
          description: detail.description,
          providerSystemName: detail.providerSystemName,
          isActive: detail.isActive,
          displayOrder: detail.displayOrder,
          requiresAddress: detail.requiresAddress,
          supportsTracking: detail.supportsTracking,
          estimatedDeliveryDaysMin: detail.estimatedDeliveryDaysMin,
          estimatedDeliveryDaysMax: detail.estimatedDeliveryDaysMax
        };
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load shipping method.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      if (this.isEdit && this.id()) {
        const request: UpdateShippingMethodRequest = {
          name: this.form.name,
          description: this.form.description,
          isActive: this.form.isActive,
          displayOrder: this.form.displayOrder,
          requiresAddress: this.form.requiresAddress,
          supportsTracking: this.form.supportsTracking,
          estimatedDeliveryDaysMin: this.form.estimatedDeliveryDaysMin,
          estimatedDeliveryDaysMax: this.form.estimatedDeliveryDaysMax
        };
        await firstValueFrom(this.shippingApi.updateMethod(this.id()!, request));
      } else {
        const request: CreateShippingMethodRequest = {
          storeId: this.form.storeId,
          name: this.form.name,
          systemName: this.form.systemName,
          description: this.form.description,
          providerSystemName: this.form.providerSystemName,
          isActive: this.form.isActive,
          displayOrder: this.form.displayOrder,
          requiresAddress: this.form.requiresAddress,
          supportsTracking: this.form.supportsTracking,
          estimatedDeliveryDaysMin: this.form.estimatedDeliveryDaysMin,
          estimatedDeliveryDaysMax: this.form.estimatedDeliveryDaysMax
        };
        await firstValueFrom(this.shippingApi.createMethod(request));
      }
      await this.router.navigateByUrl('/shipping/methods');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }
}
