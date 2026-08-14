import { Component, OnInit, inject } from '@angular/core';

import { RouterLink } from '@angular/router';

import { PaymentMethodSummary, PaymentsApi } from '@commerce/api';

import { PermissionService } from '@commerce/auth';

import { ApiClientError } from '@commerce/core';

import { BreadcrumbsComponent } from '@commerce/layout';

import { TranslatePipe } from '@commerce/localization';

import {

  ConfirmDialogComponent,

  EmptyStateComponent,

  ErrorStateComponent,

  LoadingStateComponent,

  PageState

} from '@commerce/shared';

import { firstValueFrom } from 'rxjs';



@Component({

  standalone: true,

  imports: [

    RouterLink,

    BreadcrumbsComponent,

    TranslatePipe,

    LoadingStateComponent,

    EmptyStateComponent,

    ErrorStateComponent,

    ConfirmDialogComponent

  ],

  template: `

    <cmr-breadcrumbs [items]="[

      { label: 'Dashboard', link: '/dashboard' },

      { label: ('payments.methods.title' | translate) }

    ]" />

    <header class="page-header">

      <h1>{{ 'payments.methods.title' | translate }}</h1>

      @if (permissions.hasPermission('Payments.Configure')) {

        <a routerLink="/payments/methods/new" class="btn">{{ 'action.create' | translate }}</a>

      }

    </header>



    @switch (state) {

      @case ('loading') { <cmr-loading-state /> }

      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }

      @case ('empty') { <cmr-empty-state /> }

      @default {

        <div class="table-wrap">

          <table>

            <thead>

              <tr>

                <th>{{ 'payments.methods.displayName' | translate }}</th>

                <th>{{ 'payments.methods.name' | translate }}</th>

                <th>{{ 'payments.methods.systemName' | translate }}</th>

                <th>{{ 'payments.methods.provider' | translate }}</th>

                <th>{{ 'payments.displayOrder' | translate }}</th>

                <th>{{ 'payments.active' | translate }}</th>

                <th></th>

              </tr>

            </thead>

            <tbody>

              @for (item of items; track item.id) {

                <tr>

                  <td>{{ item.displayName }}</td>

                  <td>{{ item.name }}</td>

                  <td><code>{{ item.systemName }}</code></td>

                  <td>{{ item.providerSystemName }}</td>

                  <td>{{ item.displayOrder }}</td>

                  <td>{{ item.isActive ? ('payments.active' | translate) : ('payments.inactive' | translate) }}</td>

                  <td class="actions">

                    @if (permissions.hasPermission('Payments.Configure')) {

                      <a [routerLink]="['/payments/methods', item.id]">{{ 'action.edit' | translate }}</a>

                      <button type="button" class="danger" (click)="confirmDelete(item)">{{ 'action.delete' | translate }}</button>

                    }

                  </td>

                </tr>

              }

            </tbody>

          </table>

        </div>

      }

    }



    <cmr-confirm-dialog

      [open]="deleteTarget !== null"

      [title]="'payments.methods.deleteTitle' | translate"

      [message]="('payments.methods.deleteMessage' | translate) + ' ' + (deleteTarget?.displayName ?? '')"

      (confirm)="deleteConfirmed()"

      (cancel)="deleteTarget = null" />

  `,

  styles: [`

    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }

    .btn { padding: 0.5rem 1rem; background: #2563eb; color: #fff; text-decoration: none; border-radius: 0.375rem; }

    .table-wrap { overflow-x: auto; }

    table { width: 100%; border-collapse: collapse; background: #fff; min-width: 640px; }

    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }

    .actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }

    .actions button, .actions a { font-size: 0.875rem; }

    .actions button { background: none; border: none; color: #2563eb; cursor: pointer; text-decoration: underline; }

    .actions button.danger { color: #dc2626; }

  `]

})

export class PaymentMethodListPageComponent implements OnInit {

  private readonly paymentsApi = inject(PaymentsApi);

  readonly permissions = inject(PermissionService);



  state: PageState = 'loading';

  errorMessage = '';

  items: PaymentMethodSummary[] = [];

  deleteTarget: PaymentMethodSummary | null = null;



  ngOnInit(): void {

    void this.load();

  }



  async load(): Promise<void> {

    this.state = 'loading';

    this.errorMessage = '';

    try {

      this.items = await firstValueFrom(this.paymentsApi.listMethods());

      this.state = this.items.length === 0 ? 'empty' : 'success';

    } catch (error) {

      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load payment methods.';

      this.state = 'error';

    }

  }



  confirmDelete(item: PaymentMethodSummary): void {

    this.deleteTarget = item;

  }



  async deleteConfirmed(): Promise<void> {

    if (!this.deleteTarget) return;

    try {

      await firstValueFrom(this.paymentsApi.deleteMethod(this.deleteTarget.id));

      this.deleteTarget = null;

      await this.load();

    } catch (error) {

      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';

      this.state = 'error';

    }

  }

}


