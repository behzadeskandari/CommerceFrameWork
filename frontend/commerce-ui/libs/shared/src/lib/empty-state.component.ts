import { Component, Input } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  selector: 'cmr-empty-state',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="state state--empty" role="status">
      <p>{{ messageKey | translate }}</p>
      <ng-content />
    </div>
  `,
  styles: [`
    .state { padding: 2rem; text-align: center; color: var(--text-muted); border: 1px dashed #d1d5db; border-radius: 0.5rem; }
  `]
})
export class EmptyStateComponent {
  @Input() messageKey = 'state.empty';
}
