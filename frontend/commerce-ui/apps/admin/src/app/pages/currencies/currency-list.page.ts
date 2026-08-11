import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CurrencySummary, StoreApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: ('nav.currencies' | translate) }]" />
    <header class="page-header"><h1>{{ 'nav.currencies' | translate }}</h1></header>
    @if (permissions.hasPermission('Currencies.Create')) {
      <form class="inline-form" (ngSubmit)="create()">
        <input [(ngModel)]="newCurrency.code" name="code" placeholder="Code (USD)" required />
        <input [(ngModel)]="newCurrency.name" name="name" placeholder="Name" required />
        <input [(ngModel)]="newCurrency.symbol" name="symbol" placeholder="Symbol" />
        <input type="number" step="0.000001" [(ngModel)]="newCurrency.rate" name="rate" placeholder="Rate" required />
        <button type="submit">{{ 'action.create' | translate }}</button>
      </form>
    }
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state messageKey="state.empty" /> }
      @default {
        <table>
          <thead><tr><th>Code</th><th>Name</th><th>Symbol</th><th>Rate</th><th>Decimals</th><th>Active</th><th></th></tr></thead>
          <tbody>
            @for (currency of currencies; track currency.id) {
              <tr>
                <td>{{ currency.code }}</td>
                <td>{{ currency.displayName || currency.name }}</td>
                <td>{{ currency.symbol }}</td>
                <td>{{ currency.rate }}</td>
                <td>{{ currency.decimalPlaces }}</td>
                <td>{{ currency.isActive ? 'Yes' : 'No' }}</td>
                <td>
                  @if (permissions.hasPermission('Currencies.Update') && editingId !== currency.id) {
                    <button type="button" (click)="startEdit(currency)">{{ 'action.edit' | translate }}</button>
                  }
                </td>
              </tr>
              @if (editingId === currency.id) {
                <tr>
                  <td colspan="7">
                    <form class="inline-form" (ngSubmit)="saveEdit(currency.id)">
                      <input [(ngModel)]="editForm.name" name="editName" />
                      <input [(ngModel)]="editForm.symbol" name="editSymbol" />
                      <input type="number" step="0.000001" [(ngModel)]="editForm.rate" name="editRate" />
                      <input type="number" [(ngModel)]="editForm.decimalPlaces" name="editDecimals" />
                      <label><input type="checkbox" [(ngModel)]="editForm.isActive" name="editActive" /> Active</label>
                      <button type="submit">{{ 'action.save' | translate }}</button>
                      <button type="button" (click)="editingId = null">{{ 'action.cancel' | translate }}</button>
                    </form>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .page-header { margin-block-end: 1rem; }
    .inline-form { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-block-end: 1rem; align-items: center; }
    input, button { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 0.5rem; overflow: hidden; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class CurrencyListPageComponent implements OnInit {
  private readonly storeApi = inject(StoreApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  currencies: CurrencySummary[] = [];
  editingId: number | null = null;
  newCurrency = { code: '', name: '', symbol: '', rate: 1, decimalPlaces: 2 };
  editForm = { name: '', symbol: '', displayName: '', rate: 1, decimalPlaces: 2, displayOrder: 0, isActive: true };

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.currencies = await firstValueFrom(this.storeApi.listCurrencies());
      this.state = this.currencies.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load currencies.';
      this.state = 'error';
    }
  }

  async create(): Promise<void> {
    await firstValueFrom(this.storeApi.createCurrency(this.newCurrency));
    this.newCurrency = { code: '', name: '', symbol: '', rate: 1, decimalPlaces: 2 };
    await this.load();
  }

  startEdit(currency: CurrencySummary): void {
    this.editingId = currency.id;
    this.editForm = {
      name: currency.name,
      symbol: currency.symbol,
      displayName: currency.displayName,
      rate: currency.rate,
      decimalPlaces: currency.decimalPlaces,
      displayOrder: currency.displayOrder,
      isActive: currency.isActive
    };
  }

  async saveEdit(id: number): Promise<void> {
    await firstValueFrom(this.storeApi.updateCurrency(id, this.editForm));
    this.editingId = null;
    await this.load();
  }
}
