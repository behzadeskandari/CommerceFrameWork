import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnalyticsApi, DashboardSummary, ReportFilterQuery } from '@commerce/api';
import { TranslatePipe } from '@commerce/localization';
import { firstValueFrom } from 'rxjs';

type PageState = 'loading' | 'error' | 'success';

@Component({
  standalone: true,
  imports: [FormsModule, TranslatePipe, DecimalPipe],
  template: `
    <section class="dashboard">
      <header class="dashboard__header">
        <h1>{{ 'nav.dashboard' | translate }}</h1>
        <div class="dashboard__filters">
          <label>
            Store ID
            <input type="number" [(ngModel)]="filters.storeId" min="1" />
          </label>
          <label>
            From
            <input type="date" [(ngModel)]="fromDate" />
          </label>
          <label>
            To
            <input type="date" [(ngModel)]="toDate" />
          </label>
          <button type="button" (click)="loadSummary()">Apply</button>
          <button type="button" (click)="exportRevenue()" [disabled]="state() !== 'success'">Export Revenue</button>
        </div>
      </header>

      @if (state() === 'loading') {
        <p>Loading dashboard metrics…</p>
      } @else if (state() === 'error') {
        <p class="dashboard__error">{{ errorMessage() }}</p>
      } @else if (summary()) {
        <div class="dashboard__kpis">
          <article class="kpi-card">
            <h2>Revenue</h2>
            <p class="kpi-value">{{ summary()!.totalRevenue | number: '1.2-2' }}</p>
            <small>{{ summary()!.orderCount }} paid orders</small>
          </article>
          <article class="kpi-card">
            <h2>Average Order</h2>
            <p class="kpi-value">{{ summary()!.averageOrderValue | number: '1.2-2' }}</p>
          </article>
          <article class="kpi-card">
            <h2>New Customers</h2>
            <p class="kpi-value">{{ summary()!.newCustomers }}</p>
          </article>
          <article class="kpi-card">
            <h2>Refunds</h2>
            <p class="kpi-value">{{ summary()!.totalRefunded | number: '1.2-2' }}</p>
          </article>
          <article class="kpi-card">
            <h2>Inventory Alerts</h2>
            <p class="kpi-value">{{ summary()!.lowStockItems }} low / {{ summary()!.outOfStockItems }} out</p>
          </article>
          <article class="kpi-card">
            <h2>Conversion</h2>
            <p class="kpi-value">{{ summary()!.cartToOrderConversionRate | number: '1.1-1' }}%</p>
            <small>Cart → order</small>
          </article>
        </div>

        @if (summary()!.topProducts.length) {
          <section class="dashboard__section">
            <h2>Top Products</h2>
            <table>
              <thead>
                <tr>
                  <th>Product</th>
                  <th>Qty</th>
                  <th>Revenue</th>
                </tr>
              </thead>
              <tbody>
                @for (row of summary()!.topProducts; track row.productId) {
                  <tr>
                    <td>{{ row.productName }}</td>
                    <td>{{ row.quantitySold }}</td>
                    <td>{{ row.revenue | number: '1.2-2' }} {{ row.currencyCode }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        }
      }
    </section>
  `,
  styles: [
    `
      .dashboard__header {
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1.5rem;
      }

      .dashboard__filters {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: end;
      }

      .dashboard__filters label {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        font-size: 0.875rem;
      }

      .dashboard__kpis {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1rem;
        margin-bottom: 2rem;
      }

      .kpi-card {
        border: 1px solid #e5e7eb;
        border-radius: 0.5rem;
        padding: 1rem;
        background: #fff;
      }

      .kpi-value {
        font-size: 1.5rem;
        font-weight: 600;
        margin: 0.25rem 0;
      }

      .dashboard__section table {
        width: 100%;
        border-collapse: collapse;
      }

      .dashboard__section th,
      .dashboard__section td {
        border-bottom: 1px solid #e5e7eb;
        padding: 0.5rem;
        text-align: left;
      }

      .dashboard__error {
        color: #b91c1c;
      }
    `
  ]
})
export class DashboardPageComponent implements OnInit {
  private readonly analyticsApi = inject(AnalyticsApi);

  readonly state = signal<PageState>('loading');
  readonly summary = signal<DashboardSummary | null>(null);
  readonly errorMessage = signal('');

  filters: ReportFilterQuery = { storeId: 1, granularity: 'Day' };
  fromDate = this.toInputDate(new Date(Date.now() - 30 * 86400000));
  toDate = this.toInputDate(new Date());

  ngOnInit(): void {
    void this.loadSummary();
  }

  async loadSummary(): Promise<void> {
    this.state.set('loading');
    this.errorMessage.set('');

    try {
      const query = this.buildQuery();
      const data = await firstValueFrom(this.analyticsApi.getDashboardSummary(query));
      this.summary.set(data);
      this.state.set('success');
    } catch {
      this.errorMessage.set('Unable to load dashboard metrics.');
      this.state.set('error');
    }
  }

  async exportRevenue(): Promise<void> {
    try {
      const blob = await firstValueFrom(this.analyticsApi.exportReport('Revenue', this.buildQuery()));
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'revenue-report.csv';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      this.errorMessage.set('Export failed.');
      this.state.set('error');
    }
  }

  private buildQuery(): ReportFilterQuery {
    return {
      ...this.filters,
      fromUtc: this.fromDate ? new Date(`${this.fromDate}T00:00:00Z`).toISOString() : undefined,
      toUtc: this.toDate ? new Date(`${this.toDate}T23:59:59Z`).toISOString() : undefined
    };
  }

  private toInputDate(value: Date): string {
    return value.toISOString().slice(0, 10);
  }
}
