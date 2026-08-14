import { NgTemplateOutlet } from '@angular/common';
import { Component, Input, TemplateRef, contentChild, output } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

export interface AdminTableColumn<T> {
  key: string;
  labelKey: string;
  sortable?: boolean;
  align?: 'start' | 'center' | 'end';
}

@Component({
  selector: 'cmr-admin-data-table',
  standalone: true,
  imports: [NgTemplateOutlet, TranslatePipe],
  template: `
    <div class="admin-table-wrap">
      <table class="admin-table">
        <thead>
          <tr>
            @if (selectable) {
              <th scope="col" class="admin-table__select">
                <input
                  type="checkbox"
                  [checked]="allSelected"
                  [indeterminate]="someSelected && !allSelected"
                  (change)="toggleAll.emit($any($event.target).checked)"
                  [attr.aria-label]="'admin.table.selectAll' | translate" />
              </th>
            }
            @for (column of columns; track column.key) {
              <th scope="col" [class]="'admin-table__align-' + (column.align ?? 'start')">
                @if (column.sortable) {
                  <button type="button" class="admin-table__sort" (click)="sortChange.emit(column.key)">
                    {{ column.labelKey | translate }}
                    @if (sortKey === column.key) {
                      <span aria-hidden="true">{{ sortDirection === 'asc' ? '↑' : '↓' }}</span>
                    }
                  </button>
                } @else {
                  {{ column.labelKey | translate }}
                }
              </th>
            }
            @if (actions()) {
              <th scope="col" class="admin-table__actions">{{ 'admin.table.actions' | translate }}</th>
            }
          </tr>
        </thead>
        <tbody>
          @for (row of rows; track trackBy(row)) {
            <tr>
              @if (selectable) {
                <td class="admin-table__select">
                  <input
                    type="checkbox"
                    [checked]="isSelected(row)"
                    (change)="toggleRow.emit({ row, selected: $any($event.target).checked })"
                    [attr.aria-label]="'admin.table.selectRow' | translate" />
                </td>
              }
              @for (column of columns; track column.key) {
                <td [class]="'admin-table__align-' + (column.align ?? 'start')">
                  @if (cell()) {
                    <ng-container [ngTemplateOutlet]="cell()!" [ngTemplateOutletContext]="{ $implicit: row, column: column }" />
                  }
                </td>
              }
              @if (actions()) {
                <td class="admin-table__actions">
                  <ng-container [ngTemplateOutlet]="actions()!" [ngTemplateOutletContext]="{ $implicit: row }" />
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .admin-table-wrap { overflow-x: auto; background: var(--surface-elevated, #fff); border-radius: 0.75rem; border: 1px solid #e5e7eb; }
    .admin-table { width: 100%; border-collapse: collapse; min-width: 40rem; }
    .admin-table th, .admin-table td { padding: 0.75rem 1rem; border-bottom: 1px solid #e5e7eb; text-align: start; vertical-align: middle; }
    .admin-table tr:last-child td { border-bottom: none; }
    .admin-table__sort { border: none; background: transparent; cursor: pointer; font: inherit; color: inherit; display: inline-flex; align-items: center; gap: 0.25rem; }
    .admin-table__select { width: 2.5rem; }
    .admin-table__actions { white-space: nowrap; }
    .admin-table__align-center { text-align: center; }
    .admin-table__align-end { text-align: end; }
  `]
})
export class AdminDataTableComponent<T> {
  @Input({ required: true }) columns: AdminTableColumn<T>[] = [];
  @Input({ required: true }) rows: T[] = [];
  @Input() sortKey = '';
  @Input() sortDirection: 'asc' | 'desc' = 'asc';
  @Input() selectable = false;
  @Input() allSelected = false;
  @Input() someSelected = false;
  @Input() trackBy: (row: T) => string | number = row => JSON.stringify(row);
  @Input() isSelected: (row: T) => boolean = () => false;

  readonly sortChange = output<string>();
  readonly toggleAll = output<boolean>();
  readonly toggleRow = output<{ row: T; selected: boolean }>();

  readonly cell = contentChild<TemplateRef<{ $implicit: T; column: AdminTableColumn<T> }>>('cell');
  readonly actions = contentChild<TemplateRef<{ $implicit: T }>>('actions');
}
