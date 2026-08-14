import { Component, Input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@commerce/localization';

export interface AdminFilterOption {
  value: string;
  labelKey: string;
}

@Component({
  selector: 'cmr-filter-bar',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  template: `
    <div class="filter-bar" role="search">
      @if (showSearch) {
        <label class="filter-bar__field filter-bar__field--grow">
          <span>{{ 'action.search' | translate }}</span>
          <input
            type="search"
            [ngModel]="search"
            (ngModelChange)="searchChange.emit($event)"
            [placeholder]="searchPlaceholderKey | translate"
            [attr.aria-label]="'action.search' | translate" />
        </label>
      }
      <ng-content />
      @if (showReset) {
        <button type="button" class="btn btn--secondary" (click)="reset.emit()">
          {{ 'admin.filters.reset' | translate }}
        </button>
      }
    </div>
  `,
  styles: [`
    .filter-bar {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      align-items: end;
      padding: 1rem;
      background: var(--surface-elevated, #fff);
      border: 1px solid #e5e7eb;
      border-radius: 0.75rem;
    }
    .filter-bar__field {
      display: grid;
      gap: 0.35rem;
      min-width: 10rem;
    }
    .filter-bar__field--grow { flex: 1 1 16rem; }
    input, select {
      padding: 0.5rem 0.75rem;
      border: 1px solid #d1d5db;
      border-radius: 0.375rem;
      font: inherit;
    }
    .btn {
      padding: 0.5rem 0.875rem;
      border-radius: 0.375rem;
      border: 1px solid #d1d5db;
      background: #fff;
      cursor: pointer;
    }
  `]
})
export class FilterBarComponent {
  @Input() search = '';
  @Input() showSearch = true;
  @Input() showReset = true;
  @Input() searchPlaceholderKey = 'admin.filters.searchPlaceholder';

  readonly searchChange = output<string>();
  readonly reset = output<void>();
}
