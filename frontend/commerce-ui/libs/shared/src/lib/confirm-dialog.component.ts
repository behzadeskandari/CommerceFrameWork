import { Component, Input, output } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  selector: 'cmr-confirm-dialog',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    @if (open) {
      <div class="overlay" role="presentation" (click)="cancel.emit()"></div>
      <dialog class="dialog" open [attr.aria-label]="title">
        <h2>{{ title }}</h2>
        <p>{{ message }}</p>
        <div class="actions">
          <button type="button" class="btn btn--secondary" (click)="cancel.emit()">{{ cancelLabelKey | translate }}</button>
          <button type="button" class="btn btn--danger" (click)="confirm.emit()">{{ confirmLabelKey | translate }}</button>
        </div>
      </dialog>
    }
  `,
  styles: [`
    .overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.4); z-index: 90; }
    .dialog {
      position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%);
      border: none; border-radius: 0.5rem; padding: 1.5rem; min-width: min(90vw, 24rem); background: #fff;
      box-shadow: 0 10px 30px rgba(0,0,0,0.15); z-index: 91; margin: 0;
    }
    .actions { display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 1rem; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; border: 1px solid transparent; cursor: pointer; }
    .btn--secondary { background: #f3f4f6; border-color: #d1d5db; }
    .btn--danger { background: #dc2626; color: #fff; }
  `]
})
export class ConfirmDialogComponent {
  @Input() open = false;
  @Input() title = 'Confirm';
  @Input() message = 'Are you sure?';
  @Input() cancelLabelKey = 'action.cancel';
  @Input() confirmLabelKey = 'action.delete';
  readonly confirm = output<void>();
  readonly cancel = output<void>();
}
