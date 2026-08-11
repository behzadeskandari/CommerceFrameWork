import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomerSummary, CustomersApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
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
    FormsModule,
    RouterLink,
    TranslatePipe,
    BreadcrumbsComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('customers.title' | translate) }
    ]" />
    <h1>{{ 'customers.title' | translate }}</h1>
    <label class="search">Search<input type="search" [(ngModel)]="search" (ngModelChange)="applyFilter()" /></label>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <table>
          <thead><tr><th>Name</th><th>Email</th><th>Status</th><th></th></tr></thead>
          <tbody>
            @for (customer of pageItems; track customer.id) {
              <tr>
                <td>{{ customer.firstName }} {{ customer.lastName }}</td>
                <td>{{ customer.email }}</td>
                <td>{{ customer.active ? 'Active' : 'Inactive' }}</td>
                <td><a [routerLink]="['/customers', customer.id]">View</a></td>
              </tr>
            }
          </tbody>
        </table>
        <cmr-pagination [page]="page" [totalPages]="totalPages" (pageChange)="setPage($event)" />
      }
    }
  `,
  styles: [`
    table { width: 100%; border-collapse: collapse; background: #fff; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .search { display: grid; gap: 0.375rem; margin: 1rem 0; max-width: 20rem; }
  `]
})
export class CustomerListPageComponent implements OnInit {
  private readonly customersApi = inject(CustomersApi);

  state: PageState = 'loading';
  errorMessage = '';
  allCustomers: CustomerSummary[] = [];
  filtered: CustomerSummary[] = [];
  pageItems: CustomerSummary[] = [];
  search = '';
  page = 1;
  pageSize = 10;
  totalPages = 1;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.allCustomers = await firstValueFrom(this.customersApi.listCustomersAdmin());
      this.applyFilter();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load customers.';
      this.state = 'error';
    }
  }

  applyFilter(): void {
    const term = this.search.trim().toLowerCase();
    this.filtered = this.allCustomers.filter(customer =>
      !term ||
      customer.email.toLowerCase().includes(term) ||
      `${customer.firstName} ${customer.lastName}`.toLowerCase().includes(term)
    );
    this.setPage(1);
  }

  setPage(page: number): void {
    this.page = page;
    this.totalPages = Math.max(1, Math.ceil(this.filtered.length / this.pageSize));
    const start = (this.page - 1) * this.pageSize;
    this.pageItems = this.filtered.slice(start, start + this.pageSize);
    this.state = this.filtered.length ? 'success' : 'empty';
  }
}
