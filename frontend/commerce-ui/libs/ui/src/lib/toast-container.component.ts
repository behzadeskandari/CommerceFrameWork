import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';
import { ToastService } from './toast.service';

@Component({
  selector: 'cmr-toast-container',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="toast-stack" aria-live="polite" aria-atomic="true">
      @for (toast of toastService.messages(); track toast.id) {
        <div class="toast toast--{{ toast.kind }}" role="status">
          <span>{{ toast.message }}</span>
          <button type="button" class="toast__close" (click)="toastService.dismiss(toast.id)" [attr.aria-label]="'action.close' | translate">×</button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-stack {
      position: fixed;
      inset-block-start: 1rem;
      inset-inline-end: 1rem;
      display: grid;
      gap: 0.5rem;
      z-index: 1000;
      max-width: min(24rem, calc(100vw - 2rem));
    }
    .toast {
      display: flex;
      justify-content: space-between;
      gap: 0.75rem;
      align-items: start;
      padding: 0.75rem 1rem;
      border-radius: 0.5rem;
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
      background: #fff;
      border-inline-start: 4px solid #2563eb;
    }
    .toast--success { border-inline-start-color: #059669; }
    .toast--error { border-inline-start-color: #dc2626; }
    .toast__close {
      border: none;
      background: transparent;
      font-size: 1.25rem;
      line-height: 1;
      cursor: pointer;
      color: inherit;
    }
  `]
})
export class ToastContainerComponent {
  readonly toastService = inject(ToastService);
}
