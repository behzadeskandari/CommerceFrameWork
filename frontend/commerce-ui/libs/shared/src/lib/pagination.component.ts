import { Component, Input, output } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  selector: 'cmr-pagination',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <nav class="pagination" [attr.aria-label]="'admin.pagination.page' | translate">
      <label class="pagination__size">
        {{ 'admin.pagination.pageSize' | translate }}
        <select [value]="pageSize" (change)="pageSizeChange.emit(+$any($event.target).value)">
          @for (size of pageSizes; track size) {
            <option [value]="size">{{ size }}</option>
          }
        </select>
      </label>
      <div class="pagination__controls">
        <button type="button" [disabled]="page <= 1" (click)="pageChange.emit(page - 1)">
          {{ 'admin.pagination.previous' | translate }}
        </button>
        <span>{{ 'admin.pagination.page' | translate }} {{ page }} {{ 'admin.pagination.of' | translate }} {{ totalPages }}</span>
        <button type="button" [disabled]="page >= totalPages" (click)="pageChange.emit(page + 1)">
          {{ 'admin.pagination.next' | translate }}
        </button>
      </div>
      @if (totalItems !== null) {
        <span class="pagination__total">{{ totalItems }}</span>
      }
    </nav>
  `,
  styles: [`
    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
      margin-top: 1rem;
    }
    .pagination__controls { display: flex; align-items: center; gap: 0.75rem; }
    .pagination__size, .pagination__size select {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
    }
    button {
      padding: 0.375rem 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 0.375rem;
      background: #fff;
      cursor: pointer;
    }
    button:disabled { opacity: 0.5; cursor: not-allowed; }
    select { padding: 0.375rem 0.5rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .pagination__total { color: var(--text-muted, #6b7280); font-size: 0.875rem; }
  `]
})
export class PaginationComponent {
  @Input({ required: true }) page = 1;
  @Input({ required: true }) totalPages = 1;
  @Input() pageSize = 10;
  @Input() pageSizes = [10, 20, 50];
  @Input() totalItems: number | null = null;

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
}
