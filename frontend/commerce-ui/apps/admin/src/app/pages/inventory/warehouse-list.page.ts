import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WarehouseApi, WarehouseSummary } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    BreadcrumbsComponent,
    TranslatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('nav.inventory' | translate), link: '/inventory' },
      { label: ('inventory.warehouses.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'inventory.warehouses.title' | translate }}</h1>
      @if (permissions.hasPermission('Inventory.Manage')) {
        <button type="button" class="btn" (click)="showCreate = true">{{ 'action.create' | translate }}</button>
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
                <th>Name</th>
                <th>System name</th>
                <th>Default</th>
                <th>Active</th>
                <th>Order</th>
              </tr>
            </thead>
            <tbody>
              @for (item of items; track item.id) {
                <tr>
                  <td>{{ item.name }}</td>
                  <td><code>{{ item.systemName }}</code></td>
                  <td>{{ item.isDefault ? 'Yes' : 'No' }}</td>
                  <td>{{ item.isActive ? 'Active' : 'Inactive' }}</td>
                  <td>{{ item.displayOrder }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }

    @if (showCreate) {
      <section class="modal">
        <h2>{{ 'inventory.warehouses.createTitle' | translate }}</h2>
        <form class="create-form" (ngSubmit)="createWarehouse()">
          <label>
            Name
            <input type="text" [(ngModel)]="createForm.name" name="name" required />
          </label>
          <label>
            System name
            <input type="text" [(ngModel)]="createForm.systemName" name="systemName" required />
          </label>
          <label>
            <input type="checkbox" [(ngModel)]="createForm.isDefault" name="isDefault" />
            Default warehouse
          </label>
          <div class="actions">
            <button type="submit" [disabled]="creating">Create</button>
            <button type="button" class="secondary" (click)="showCreate = false">Cancel</button>
          </div>
        </form>
        @if (createMessage) { <p>{{ createMessage }}</p> }
      </section>
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
    .btn, button { padding: 0.5rem 1rem; background: #2563eb; color: #fff; border: none; border-radius: 0.375rem; cursor: pointer; }
    button.secondary { background: #6b7280; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; background: #fff; min-width: 640px; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .modal { margin-top: 1rem; background: #fff; border: 1px solid #e5e7eb; border-radius: 0.5rem; padding: 1rem; max-width: 24rem; }
    .create-form { display: grid; gap: 0.75rem; }
    input[type='text'] { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .actions { display: flex; gap: 0.5rem; }
  `]
})
export class WarehouseListPageComponent implements OnInit {
  private readonly warehouseApi = inject(WarehouseApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  items: WarehouseSummary[] = [];
  showCreate = false;
  creating = false;
  createMessage = '';
  createForm = { name: '', systemName: '', isDefault: false };

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      this.items = await firstValueFrom(this.warehouseApi.list());
      this.state = this.items.length === 0 ? 'empty' : 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load warehouses.';
      this.state = 'error';
    }
  }

  async createWarehouse(): Promise<void> {
    this.creating = true;
    this.createMessage = '';
    try {
      await firstValueFrom(this.warehouseApi.create(this.createForm));
      this.showCreate = false;
      this.createForm = { name: '', systemName: '', isDefault: false };
      await this.load();
    } catch (error) {
      this.createMessage = error instanceof ApiClientError ? error.message : 'Create failed.';
    } finally {
      this.creating = false;
    }
  }
}
