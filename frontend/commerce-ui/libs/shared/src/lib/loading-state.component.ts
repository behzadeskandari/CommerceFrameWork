import { Component, Input } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  selector: 'cmr-loading-state',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="state state--loading" role="status" aria-live="polite">
      <span class="spinner" aria-hidden="true"></span>
      <span>{{ messageKey | translate }}</span>
    </div>
  `,
  styles: [`
    .state { display: flex; align-items: center; gap: 0.75rem; padding: 2rem; color: var(--text-muted); }
    .spinner {
      width: 1rem; height: 1rem; border: 2px solid currentColor; border-right-color: transparent;
      border-radius: 50%; animation: spin 0.8s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class LoadingStateComponent {
  @Input() messageKey = 'state.loading';
}
