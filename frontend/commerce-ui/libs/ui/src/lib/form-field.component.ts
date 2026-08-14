import { Component, Input } from '@angular/core';
import { TranslatePipe } from '@commerce/localization';

@Component({
  selector: 'cmr-form-field',
  standalone: true,
  template: `
    <label class="form-field" [class.form-field--invalid]="!!error">
      <span class="form-field__label">
        {{ label }}
        @if (required) {
          <span class="form-field__required" aria-hidden="true">*</span>
        }
      </span>
      <div class="form-field__control">
        <ng-content />
      </div>
      @if (hint) {
        <span class="form-field__hint">{{ hint }}</span>
      }
      @if (error) {
        <span class="form-field__error" role="alert">{{ error }}</span>
      }
    </label>
  `,
  styles: [`
    .form-field { display: grid; gap: 0.35rem; }
    .form-field__label { font-weight: 600; }
    .form-field__required { color: #dc2626; margin-inline-start: 0.15rem; }
    .form-field__hint { color: var(--text-muted, #6b7280); font-size: 0.875rem; }
    .form-field__error { color: #dc2626; font-size: 0.875rem; }
    .form-field--invalid .form-field__control :where(input, select, textarea) {
      border-color: #dc2626;
    }
  `]
})
export class FormFieldComponent {
  @Input({ required: true }) label = '';
  @Input() hint = '';
  @Input() error = '';
  @Input() required = false;
}
