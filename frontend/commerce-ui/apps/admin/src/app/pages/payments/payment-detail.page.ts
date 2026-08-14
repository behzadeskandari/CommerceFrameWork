import { DatePipe } from '@angular/common';

import { Component, OnInit, inject, input } from '@angular/core';

import { RouterLink } from '@angular/router';

import { PaymentDetail, PaymentsApi } from '@commerce/api';

import { PermissionService } from '@commerce/auth';

import { ApiClientError } from '@commerce/core';

import { BreadcrumbsComponent } from '@commerce/layout';

import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';

import {

  ConfirmDialogComponent,

  ErrorStateComponent,

  LoadingStateComponent,

  PageState

} from '@commerce/shared';

import { firstValueFrom } from 'rxjs';



@Component({

  standalone: true,

  imports: [

    DatePipe,

    RouterLink,

    BreadcrumbsComponent,

    TranslatePipe,

    CurrencyFormatPipe,

    LoadingStateComponent,

    ErrorStateComponent,

    ConfirmDialogComponent

  ],

  template: `

    @if (state === 'loading') { <cmr-loading-state /> }

    @else if (detail) {

      <cmr-breadcrumbs [items]="[

        { label: 'Dashboard', link: '/dashboard' },

        { label: ('payments.title' | translate), link: '/payments' },

        { label: ('payments.detail' | translate) + ' #' + detail.payment.id }

      ]" />



      <header class="header">

        <div>

          <h1>{{ 'payments.detail' | translate }} #{{ detail.payment.id }}</h1>

          <p>{{ detail.payment.createdAtUtc | date: 'medium' }}</p>

        </div>

        <div class="actions">

          @if (canCapture) {

            <button type="button" (click)="confirmAction = 'capture'" [disabled]="acting">{{ 'payments.capture' | translate }}</button>

          }

          @if (canVoid) {

            <button type="button" class="secondary" (click)="confirmAction = 'void'" [disabled]="acting">{{ 'payments.void' | translate }}</button>

          }

          @if (canRefund) {

            <button type="button" class="danger" (click)="confirmAction = 'refund'" [disabled]="acting">{{ 'payments.refund' | translate }}</button>

          }

        </div>

      </header>



      <section class="info-grid">

        <div><strong>{{ 'payments.orderId' | translate }}</strong><a [routerLink]="['/orders', detail.payment.orderId]">{{ detail.payment.orderId }}</a></div>

        <div><strong>{{ 'payments.status' | translate }}</strong><span>{{ detail.payment.status }}</span></div>

        <div><strong>{{ 'payments.amount' | translate }}</strong><span>{{ detail.payment.amount | currencyFormat: detail.payment.currency }}</span></div>

        <div><strong>{{ 'payments.refundedAmount' | translate }}</strong><span>{{ detail.payment.refundedAmount | currencyFormat: detail.payment.currency }}</span></div>

        <div><strong>{{ 'payments.provider' | translate }}</strong><span><code>{{ detail.payment.providerSystemName }}</code></span></div>

        @if (detail.payment.providerPaymentId) {

          <div><strong>{{ 'payments.providerPaymentId' | translate }}</strong><span><code>{{ detail.payment.providerPaymentId }}</code></span></div>

        }

      </section>



      <section>

        <h2>{{ 'payments.transactions' | translate }}</h2>

        @if (detail.transactions.length === 0) {

          <p>{{ 'payments.noTransactions' | translate }}</p>

        } @else {

          <div class="table-wrap">

            <table>

              <thead>

                <tr>

                  <th>{{ 'payments.transactionType' | translate }}</th>

                  <th>{{ 'payments.amount' | translate }}</th>

                  <th>{{ 'payments.status' | translate }}</th>

                  <th>{{ 'payments.date' | translate }}</th>

                  <th>{{ 'payments.failure' | translate }}</th>

                </tr>

              </thead>

              <tbody>

                @for (tx of detail.transactions; track tx.id) {

                  <tr>

                    <td>{{ tx.transactionType }}</td>

                    <td>{{ tx.amount | currencyFormat: tx.currency }}</td>

                    <td>{{ tx.status }}</td>

                    <td>{{ tx.createdAtUtc | date: 'medium' }}</td>

                    <td>{{ tx.failureMessage || '—' }}</td>

                  </tr>

                }

              </tbody>

            </table>

          </div>

        }

      </section>



      @if (detail.refunds.length) {

        <section>

          <h2>{{ 'payments.refunds' | translate }}</h2>

          <div class="table-wrap">

            <table>

              <thead>

                <tr>

                  <th>{{ 'payments.amount' | translate }}</th>

                  <th>{{ 'payments.status' | translate }}</th>

                  <th>{{ 'payments.reason' | translate }}</th>

                  <th>{{ 'payments.date' | translate }}</th>

                </tr>

              </thead>

              <tbody>

                @for (refund of detail.refunds; track refund.id) {

                  <tr>

                    <td>{{ refund.amount | currencyFormat: refund.currency }}</td>

                    <td>{{ refund.status }}</td>

                    <td>{{ refund.reason || '—' }}</td>

                    <td>{{ refund.createdAtUtc | date: 'medium' }}</td>

                  </tr>

                }

              </tbody>

            </table>

          </div>

        </section>

      }



      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }

    } @else if (state === 'error') {

      <cmr-error-state [message]="errorMessage" (retry)="load()" />

    }



    <cmr-confirm-dialog

      [open]="confirmAction !== null"

      [title]="confirmTitleKey | translate"

      [message]="confirmMessageKey | translate"

      (confirm)="executeAction()"

      (cancel)="confirmAction = null" />

  `,

  styles: [`

    .header { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap; }

    .actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }

    .actions button { padding: 0.625rem 1rem; border: none; border-radius: 0.375rem; cursor: pointer; background: #2563eb; color: #fff; }

    .actions button.secondary { background: #6b7280; }

    .actions button.danger { background: #dc2626; }

    .actions button:disabled { opacity: 0.6; cursor: not-allowed; }

    .info-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr)); gap: 1rem; margin-bottom: 1.5rem; }

    .info-grid div { display: grid; gap: 0.25rem; padding: 0.75rem; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.375rem; }

    section { margin-bottom: 1.5rem; }

    .table-wrap { overflow-x: auto; }

    table { width: 100%; border-collapse: collapse; background: #fff; min-width: 640px; }

    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }

  `]

})

export class PaymentDetailPageComponent implements OnInit {

  readonly id = input.required<string>();



  private readonly paymentsApi = inject(PaymentsApi);

  readonly permissions = inject(PermissionService);



  state: PageState = 'loading';

  errorMessage = '';

  detail: PaymentDetail | null = null;

  acting = false;

  confirmAction: 'capture' | 'void' | 'refund' | null = null;



  get canManage(): boolean {

    return this.permissions.hasPermission('Payments.Manage');

  }



  get canRefundPermission(): boolean {

    return this.permissions.hasPermission('Payments.Refund');

  }



  get canCapture(): boolean {

    return this.canManage && this.detail?.payment.status === 'Authorized';

  }



  get canVoid(): boolean {

    return this.canManage && (this.detail?.payment.status === 'Authorized' || this.detail?.payment.status === 'Initiated');

  }



  get canRefund(): boolean {

    return this.canRefundPermission &&

      (this.detail?.payment.status === 'Captured' || this.detail?.payment.status === 'PartiallyRefunded');

  }



  get confirmTitleKey(): string {

    switch (this.confirmAction) {

      case 'capture': return 'payments.captureConfirmTitle';

      case 'void': return 'payments.voidConfirmTitle';

      case 'refund': return 'payments.refundConfirmTitle';

      default: return '';

    }

  }



  get confirmMessageKey(): string {

    switch (this.confirmAction) {

      case 'capture': return 'payments.captureConfirmMessage';

      case 'void': return 'payments.voidConfirmMessage';

      case 'refund': return 'payments.refundConfirmMessage';

      default: return '';

    }

  }



  ngOnInit(): void {

    void this.load();

  }



  async load(): Promise<void> {

    this.state = 'loading';

    this.errorMessage = '';

    try {

      this.detail = await firstValueFrom(this.paymentsApi.getAdminPayment(Number(this.id())));

      this.state = 'success';

    } catch (error) {

      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load payment.';

      this.state = 'error';

    }

  }



  async executeAction(): Promise<void> {

    if (!this.detail || !this.confirmAction) return;

    const paymentId = this.detail.payment.id;

    const action = this.confirmAction;

    this.confirmAction = null;

    this.acting = true;

    this.errorMessage = '';

    try {

      switch (action) {

        case 'capture':

          this.detail = await firstValueFrom(this.paymentsApi.capturePayment(paymentId));

          break;

        case 'void':

          this.detail = await firstValueFrom(this.paymentsApi.voidPayment(paymentId));

          break;

        case 'refund':

          this.detail = await firstValueFrom(this.paymentsApi.refundPayment(paymentId, { reason: 'Refunded by admin' }));

          break;

      }

    } catch (error) {

      this.errorMessage = error instanceof ApiClientError ? error.message : 'Action failed.';

    } finally {

      this.acting = false;

    }

  }

}


