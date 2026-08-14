import { Component, Input } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  selector: 'cmr-bulk-action-bar',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    @if (selectedCount > 0) {
      <div class="bulk-bar" role="status">
        <span>{{ selectedCount }} {{ 'admin.bulk.itemsSelected' | translate }}</span>
        <div class="bulk-bar__actions">
          <ng-content />
        </div>
      </div>
    }
  `,
  styles: [`
    .bulk-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      flex-wrap: wrap;
      margin-block: 0.75rem;
      padding: 0.75rem 1rem;
      background: #eff6ff;
      border: 1px solid #bfdbfe;
      border-radius: 0.5rem;
    }
    .bulk-bar__actions { display: flex; flex-wrap: wrap; gap: 0.5rem; }
  `]
})
export class BulkActionBarComponent {
  @Input() selectedCount = 0;
}
