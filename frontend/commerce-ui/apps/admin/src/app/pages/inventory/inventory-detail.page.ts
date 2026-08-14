import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AdjustInventoryStockRequest,
  InventoryApi,
  InventoryItemDetail,
  InventoryMovement,
  InventoryMovementType,
  InventoryReservation
} from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    BreadcrumbsComponent,
    TranslatePipe,
    LoadingStateComponent,
    ErrorStateComponent
  ],
  template: `
    @if (state === 'loading') { <cmr-loading-state /> }
    @else if (state === 'error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
    @else if (item) {
      <cmr-breadcrumbs [items]="[
        { label: 'Dashboard', link: '/dashboard' },
        { label: ('nav.inventory' | translate), link: '/inventory' },
        { label: '#' + item.id }
      ]" />
      <h1>{{ 'inventory.detailTitle' | translate }} #{{ item.id }}</h1>

      <section class="card">
        <h2>{{ 'inventory.stockSummary' | translate }}</h2>
        <dl>
          <div><dt>{{ 'inventory.offerId' | translate }}</dt><dd>{{ item.offerId }}</dd></div>
          <div><dt>{{ 'inventory.onHand' | translate }}</dt><dd>{{ item.onHand }}</dd></div>
          <div><dt>{{ 'inventory.reserved' | translate }}</dt><dd>{{ item.reserved }}</dd></div>
          <div><dt>{{ 'inventory.available' | translate }}</dt><dd>{{ item.available }}</dd></div>
          <div><dt>Incoming</dt><dd>{{ item.incoming }}</dd></div>
          @if (item.warehouseId != null) {
            <div><dt>Warehouse</dt><dd>#{{ item.warehouseId }}</dd></div>
          }
          <div><dt>Low stock</dt><dd>{{ item.isLowStock ? 'Yes' : 'No' }}</dd></div>
          <div><dt>{{ 'inventory.status' | translate }}</dt><dd>{{ item.availabilityStatus }}</dd></div>
          <div><dt>{{ 'inventory.backorder' | translate }}</dt><dd>{{ item.allowBackorder ? 'Yes' : 'No' }}</dd></div>
        </dl>
      </section>

      <section class="card">
        <h2>Receive incoming</h2>
        <form class="adjust-form" (ngSubmit)="submitReceiveIncoming()">
          <label>
            Quantity
            <input type="number" [(ngModel)]="receiveIncoming.quantity" name="receiveQty" min="1" required />
          </label>
          <label>
            Reason
            <input type="text" [(ngModel)]="receiveIncoming.reason" name="receiveReason" required />
          </label>
          <button type="submit" [disabled]="receiving">Receive</button>
        </form>
        @if (receiveMessage) { <p role="status">{{ receiveMessage }}</p> }
      </section>

      <section class="card">
        <h2>Low stock threshold</h2>
        <form class="adjust-form" (ngSubmit)="submitLowStockThreshold()">
          <label>
            Threshold
            <input type="number" [(ngModel)]="lowStockThreshold" name="lowStockThreshold" min="0" />
          </label>
          <button type="submit" [disabled]="settingThreshold">Save threshold</button>
        </form>
        @if (thresholdMessage) { <p role="status">{{ thresholdMessage }}</p> }
      </section>

      <section class="card">
        <h2>{{ 'inventory.adjustment' | translate }}</h2>
        <form class="adjust-form" (ngSubmit)="submitAdjustment()">
          <label>
            {{ 'inventory.quantityDelta' | translate }}
            <input type="number" [(ngModel)]="adjustment.quantityDelta" name="quantityDelta" required />
          </label>
          <label>
            {{ 'inventory.movementType' | translate }}
            <select [(ngModel)]="adjustment.movementType" name="movementType" required>
              @for (type of movementTypes; track type) {
                <option [value]="type">{{ type }}</option>
              }
            </select>
          </label>
          <label>
            {{ 'inventory.reason' | translate }}
            <input type="text" [(ngModel)]="adjustment.reason" name="reason" required />
          </label>
          <button type="submit" [disabled]="adjusting">{{ 'inventory.applyAdjustment' | translate }}</button>
        </form>
        @if (adjustMessage) { <p role="status">{{ adjustMessage }}</p> }
      </section>

      <section class="card">
        <h2>{{ 'inventory.movementHistory' | translate }}</h2>
        <table>
          <thead>
            <tr>
              <th>{{ 'inventory.quantityDelta' | translate }}</th>
              <th>{{ 'inventory.movementType' | translate }}</th>
              <th>{{ 'inventory.reason' | translate }}</th>
              <th>Date</th>
            </tr>
          </thead>
          <tbody>
            @for (movement of movements; track movement.id) {
              <tr>
                <td>{{ movement.quantityDelta }}</td>
                <td>{{ movement.movementType }}</td>
                <td>{{ movement.reason }}</td>
                <td>{{ movement.createdAtUtc | date: 'medium' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </section>

      <section class="card">
        <h2>{{ 'inventory.reservationHistory' | translate }}</h2>
        <table>
          <thead>
            <tr>
              <th>Qty</th>
              <th>Reference</th>
              <th>Status</th>
              <th>Expires</th>
            </tr>
          </thead>
          <tbody>
            @for (reservation of reservations; track reservation.id) {
              <tr>
                <td>{{ reservation.quantity }}</td>
                <td>{{ reservation.referenceType }} #{{ reservation.referenceId }}</td>
                <td>{{ reservation.status }}</td>
                <td>{{ reservation.expiresAtUtc | date: 'medium' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </section>
    }
  `,
  styles: [`
    .card { background: #fff; border: 1px solid #e5e7eb; border-radius: 0.5rem; padding: 1rem; margin-bottom: 1rem; }
    dl { display: grid; grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr)); gap: 0.75rem; }
    dt { color: #6b7280; font-size: 0.875rem; }
    dd { margin: 0; font-weight: 600; }
    .adjust-form { display: grid; gap: 0.75rem; max-width: 24rem; }
    input, select, button { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    button { background: #111827; color: #fff; border: none; cursor: pointer; width: fit-content; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class InventoryDetailPageComponent implements OnInit {
  readonly id = input.required<number>();
  private readonly inventoryApi = inject(InventoryApi);

  state: PageState = 'loading';
  errorMessage = '';
  item: InventoryItemDetail | null = null;
  movements: InventoryMovement[] = [];
  reservations: InventoryReservation[] = [];
  adjusting = false;
  adjustMessage = '';
  receiving = false;
  receiveMessage = '';
  settingThreshold = false;
  thresholdMessage = '';
  lowStockThreshold: number | null = null;
  receiveIncoming = { quantity: 1, reason: '' };
  readonly movementTypes: InventoryMovementType[] = [
    'InitialStock',
    'PurchaseReceipt',
    'ManualAdjustment',
    'Return',
    'Correction',
    'Damage',
    'Loss'
  ];
  adjustment: AdjustInventoryStockRequest = {
    quantityDelta: 0,
    movementType: 'ManualAdjustment',
    reason: ''
  };

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      const inventoryId = this.id();
      const [item, movements, reservations] = await Promise.all([
        firstValueFrom(this.inventoryApi.getById(inventoryId)),
        firstValueFrom(this.inventoryApi.listMovements(inventoryId)),
        firstValueFrom(this.inventoryApi.listReservations(inventoryId))
      ]);
      this.item = item;
      this.lowStockThreshold = item.lowStockThreshold;
      this.movements = movements;
      this.reservations = reservations;
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load inventory item.';
      this.state = 'error';
    }
  }

  async submitAdjustment(): Promise<void> {
    if (!this.item) return;
    this.adjusting = true;
    this.adjustMessage = '';
    try {
      this.item = await firstValueFrom(this.inventoryApi.adjust(this.item.id, this.adjustment));
      this.movements = await firstValueFrom(this.inventoryApi.listMovements(this.item.id));
      this.adjustMessage = 'Adjustment applied.';
      this.adjustment = { quantityDelta: 0, movementType: 'ManualAdjustment', reason: '' };
    } catch (error) {
      this.adjustMessage = error instanceof ApiClientError ? error.message : 'Adjustment failed.';
    } finally {
      this.adjusting = false;
    }
  }

  async submitReceiveIncoming(): Promise<void> {
    if (!this.item) return;
    this.receiving = true;
    this.receiveMessage = '';
    try {
      this.item = await firstValueFrom(this.inventoryApi.receiveIncoming(this.item.id, this.receiveIncoming));
      this.movements = await firstValueFrom(this.inventoryApi.listMovements(this.item.id));
      this.receiveMessage = 'Incoming stock received.';
      this.receiveIncoming = { quantity: 1, reason: '' };
    } catch (error) {
      this.receiveMessage = error instanceof ApiClientError ? error.message : 'Receive failed.';
    } finally {
      this.receiving = false;
    }
  }

  async submitLowStockThreshold(): Promise<void> {
    if (!this.item) return;
    this.settingThreshold = true;
    this.thresholdMessage = '';
    try {
      this.item = await firstValueFrom(
        this.inventoryApi.setLowStockThreshold(this.item.id, { threshold: this.lowStockThreshold })
      );
      this.thresholdMessage = 'Threshold updated.';
    } catch (error) {
      this.thresholdMessage = error instanceof ApiClientError ? error.message : 'Update failed.';
    } finally {
      this.settingThreshold = false;
    }
  }
}
