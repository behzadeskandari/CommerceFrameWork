import { Component, Input, output } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  selector: 'cmr-error-state',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="state state--error" role="alert">
      <p>{{ message || ('state.error' | translate) }}</p>
      @if (retryLabel) {
        <button type="button" class="btn btn--secondary" (click)="retry.emit()">{{ retryLabel }}</button>
      }
    </div>
  `,
  styles: [`
    .state { padding: 1.5rem; border: 1px solid #fecaca; background: #fef2f2; color: #991b1b; border-radius: 0.5rem; }
    .btn { margin-top: 0.75rem; padding: 0.5rem 1rem; border: 1px solid #d1d5db; border-radius: 0.375rem; background: #fff; cursor: pointer; }
  `]
})
export class ErrorStateComponent {
  @Input() message = '';
  @Input() retryLabel = 'Retry';
  readonly retry = output<void>();
}
