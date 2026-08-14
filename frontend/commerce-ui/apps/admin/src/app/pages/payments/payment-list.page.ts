import { DatePipe } from '@angular/common';

import { Component, OnInit, inject } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { RouterLink } from '@angular/router';

import { PAYMENT_STATUSES, PaymentStatus, PaymentSummary, PaymentsApi } from '@commerce/api';

import { ApiClientError } from '@commerce/core';

import { BreadcrumbsComponent } from '@commerce/layout';

import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';

import {

  EmptyStateComponent,

  ErrorStateComponent,

  LoadingStateComponent,

  PageState,

  PaginationComponent

} from '@commerce/shared';

import { firstValueFrom } from 'rxjs';



@Component({

  standalone: true,

  imports: [

    DatePipe,

    FormsModule,

    RouterLink,

    BreadcrumbsComponent,

    TranslatePipe,

    CurrencyFormatPipe,

    LoadingStateComponent,

    EmptyStateComponent,

    ErrorStateComponent,

    PaginationComponent

  ],

  template: `

    <cmr-breadcrumbs [items]="[

      { label: 'Dashboard', link: '/dashboard' },

      { label: ('payments.title' | translate) }

    ]" />

    <h1>{{ 'payments.title' | translate }}</h1>



    <div class="filters">

      <label>

        {{ 'payments.status' | translate }}

        <select [(ngModel)]="statusFilter" (ngModelChange)="applyFilters()">

          <option value="">{{ 'payments.allStatuses' | translate }}</option>

          @for (status of statuses; track status) {

            <option [value]="status">{{ status }}</option>

          }

        </select>

      </label>

    </div>



    @switch (state) {

      @case ('loading') { <cmr-loading-state /> }

      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }

      @case ('empty') { <cmr-empty-state /> }

      @default {

        <div class="table-wrap">

          <table>

            <thead>

              <tr>

                <th>{{ 'payments.id' | translate }}</th>

                <th>{{ 'payments.orderId' | translate }}</th>

                <th>{{ 'payments.amount' | translate }}</th>

                <th>{{ 'payments.status' | translate }}</th>

                <th>{{ 'payments.provider' | translate }}</th>

                <th>{{ 'payments.date' | translate }}</th>

                <th></th>

              </tr>

            </thead>

            <tbody>

              @for (payment of items; track payment.id) {

                <tr>

                  <td>{{ payment.id }}</td>

                  <td>{{ payment.orderId }}</td>

                  <td>{{ payment.amount | currencyFormat: payment.currency }}</td>

                  <td>{{ payment.status }}</td>

                  <td><code>{{ payment.providerSystemName }}</code></td>

                  <td>{{ payment.createdAtUtc | date: 'medium' }}</td>

                  <td><a [routerLink]="['/payments', payment.id]">{{ 'action.view' | translate }}</a></td>

                </tr>

              }

            </tbody>

          </table>

        </div>

        <cmr-pagination [page]="page" [totalPages]="totalPages" (pageChange)="setPage($event)" />

      }

    }

  `,

  styles: [`

    .filters { display: flex; flex-wrap: wrap; gap: 1rem; margin: 1rem 0; }

    .filters label { display: grid; gap: 0.375rem; min-width: 10rem; }

    input, select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }

    .table-wrap { overflow-x: auto; }

    table { width: 100%; border-collapse: collapse; background: #fff; min-width: 640px; }

    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }

  `]

})

export class PaymentListPageComponent implements OnInit {

  private readonly paymentsApi = inject(PaymentsApi);



  readonly statuses = PAYMENT_STATUSES;

  state: PageState = 'loading';

  errorMessage = '';

  items: PaymentSummary[] = [];

  statusFilter: PaymentStatus | '' = '';

  page = 1;

  pageSize = 20;

  totalPages = 1;



  ngOnInit(): void {

    void this.load();

  }



  applyFilters(): void {

    void this.setPage(1);

  }



  async load(): Promise<void> {

    this.state = 'loading';

    this.errorMessage = '';

    try {

      const result = await firstValueFrom(this.paymentsApi.listPayments({

        page: this.page,

        pageSize: this.pageSize,

        status: this.statusFilter || undefined

      }));

      this.items = result.items;

      this.totalPages = Math.max(1, Math.ceil(result.totalCount / this.pageSize));

      this.state = this.items.length ? 'success' : 'empty';

    } catch (error) {

      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load payments.';

      this.state = 'error';

    }

  }



  setPage(page: number): void {

    this.page = page;

    void this.load();

  }

}


