import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent } from '@commerce/shared';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, ErrorStateComponent],
  template: `
    <section class="auth-card">
      <h1>{{ 'auth.register.title' | translate }}</h1>
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>{{ 'auth.firstName' | translate }}<input formControlName="firstName" /></label>
        <label>{{ 'auth.lastName' | translate }}<input formControlName="lastName" /></label>
        <label>{{ 'auth.email' | translate }}<input type="email" formControlName="email" /></label>
        <label>{{ 'auth.password' | translate }}<input type="password" formControlName="password" /></label>
        <button type="submit" [disabled]="form.invalid || submitting">Register</button>
      </form>
    </section>
  `,
  styles: [`
    .auth-card { max-width: 24rem; margin: 0 auto; background: #fff; padding: 2rem; border-radius: 0.75rem; border: 1px solid #e5e7eb; }
    form { display: grid; gap: 1rem; margin-top: 1rem; }
    label { display: grid; gap: 0.375rem; }
    input { padding: 0.625rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    button { padding: 0.75rem; border: none; background: var(--primary, #0f766e); color: #fff; border-radius: 0.375rem; }
  `]
})
export class RegisterPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  submitting = false;
  errorMessage = '';
  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    this.submitting = true;
    try {
      await this.auth.register(this.form.getRawValue());
      await this.router.navigateByUrl('/account');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Registration failed.';
    } finally {
      this.submitting = false;
    }
  }
}
