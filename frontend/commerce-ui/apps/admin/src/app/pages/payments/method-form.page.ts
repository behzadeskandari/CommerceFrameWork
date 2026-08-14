import { Component, OnInit, inject, input } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { Router, RouterLink } from '@angular/router';

import {

  CreatePaymentMethodRequest,

  PAYMENT_PROVIDER_MANUAL,

  PaymentsApi,

  UpdatePaymentMethodRequest

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

      { label: ('payments.methods.title' | translate), link: '/payments/methods' },

      { label: isEdit ? form.displayName : ('action.create' | translate) }

    ]" />

    @switch (state) {

      @case ('loading') { <cmr-loading-state /> }

      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }

      @default {

        <form class="form" (ngSubmit)="save()">

          <h1>{{ isEdit ? form.displayName : ('payments.methods.create' | translate) }}</h1>



          @if (!isEdit) {

            <label>{{ 'payments.methods.systemName' | translate }}

              <input [(ngModel)]="form.systemName" name="systemName" required />

            </label>

            <label>{{ 'payments.storeId' | translate }}

              <input type="number" [(ngModel)]="form.storeId" name="storeId" required />

            </label>

            <label>{{ 'payments.methods.provider' | translate }}

              <input [(ngModel)]="form.providerSystemName" name="providerSystemName" required />

            </label>

          }

          <label>{{ 'payments.methods.name' | translate }}

            <input [(ngModel)]="form.name" name="name" required />

          </label>

          <label>{{ 'payments.methods.displayName' | translate }}

            <input [(ngModel)]="form.displayName" name="displayName" required />

          </label>

          <label>{{ 'payments.displayOrder' | translate }}

            <input type="number" [(ngModel)]="form.displayOrder" name="displayOrder" required />

          </label>

          <label class="checkbox">

            <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />

            {{ 'payments.active' | translate }}

          </label>

          <label class="checkbox">

            <input type="checkbox" [(ngModel)]="form.requiresRedirect" name="requiresRedirect" />

            {{ 'payments.methods.requiresRedirect' | translate }}

          </label>

          <label class="checkbox">

            <input type="checkbox" [(ngModel)]="form.supportsGuest" name="supportsGuest" />

            {{ 'payments.methods.supportsGuest' | translate }}

          </label>

          <label class="checkbox">

            <input type="checkbox" [(ngModel)]="form.supportsFreeOrders" name="supportsFreeOrders" />

            {{ 'payments.methods.supportsFreeOrders' | translate }}

          </label>

          <label>{{ 'payments.methods.configurationJson' | translate }}

            <textarea [(ngModel)]="form.configurationJson" name="configurationJson" rows="4"></textarea>

          </label>



          <div class="actions">

            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>

            <a routerLink="/payments/methods">{{ 'action.cancel' | translate }}</a>

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

export class PaymentMethodFormPageComponent implements OnInit {

  readonly id = input<number | undefined>();



  private readonly paymentsApi = inject(PaymentsApi);

  private readonly router = inject(Router);



  state: PageState = 'loading';

  errorMessage = '';

  isEdit = false;



  form = {

    storeId: 1,

    systemName: '',

    name: '',

    displayName: '',

    providerSystemName: PAYMENT_PROVIDER_MANUAL,

    isActive: true,

    displayOrder: 0,

    requiresRedirect: false,

    supportsGuest: true,

    supportsFreeOrders: false,

    configurationJson: '' as string | null

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

        const detail = await firstValueFrom(this.paymentsApi.getMethod(methodId));

        this.form = {

          storeId: detail.storeId,

          systemName: detail.systemName,

          name: detail.name,

          displayName: detail.displayName,

          providerSystemName: detail.providerSystemName,

          isActive: detail.isActive,

          displayOrder: detail.displayOrder,

          requiresRedirect: detail.requiresRedirect,

          supportsGuest: detail.supportsGuest,

          supportsFreeOrders: detail.supportsFreeOrders,

          configurationJson: detail.configurationJson

        };

      }

      this.state = 'success';

    } catch (error) {

      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load payment method.';

      this.state = 'error';

    }

  }



  async save(): Promise<void> {

    try {

      if (this.isEdit && this.id()) {

        const request: UpdatePaymentMethodRequest = {

          name: this.form.name,

          displayName: this.form.displayName,

          isActive: this.form.isActive,

          displayOrder: this.form.displayOrder,

          requiresRedirect: this.form.requiresRedirect,

          supportsGuest: this.form.supportsGuest,

          supportsFreeOrders: this.form.supportsFreeOrders,

          configurationJson: this.form.configurationJson || null

        };

        await firstValueFrom(this.paymentsApi.updateMethod(this.id()!, request));

      } else {

        const request: CreatePaymentMethodRequest = {

          storeId: this.form.storeId,

          name: this.form.name,

          systemName: this.form.systemName,

          providerSystemName: this.form.providerSystemName,

          displayName: this.form.displayName,

          isActive: this.form.isActive,

          displayOrder: this.form.displayOrder,

          requiresRedirect: this.form.requiresRedirect,

          supportsGuest: this.form.supportsGuest,

          supportsFreeOrders: this.form.supportsFreeOrders,

          configurationJson: this.form.configurationJson || null

        };

        await firstValueFrom(this.paymentsApi.createMethod(request));

      }

      await this.router.navigateByUrl('/payments/methods');

    } catch (error) {

      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';

      this.state = 'error';

    }

  }

}


