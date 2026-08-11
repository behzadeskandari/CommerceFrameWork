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
    <section class="login-card">
      <h1>{{ 'auth.login.title' | translate }}</h1>
      @if (errorMessage) {
        <cmr-error-state [message]="errorMessage" [retryLabel]="''" />
      }
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          {{ 'auth.email' | translate }}
          <input type="email" formControlName="email" autocomplete="username" required />
        </label>
        <label>
          {{ 'auth.password' | translate }}
          <input type="password" formControlName="password" autocomplete="current-password" required />
        </label>
        <button type="submit" [disabled]="form.invalid || submitting">Sign in</button>
      </form>
    </section>
  `,
  styles: [`
    :host { display: grid; min-height: 100vh; place-items: center; padding: 1rem; }
    .login-card { width: min(100%, 24rem); background: #fff; padding: 2rem; border-radius: 0.75rem; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }
    form { display: grid; gap: 1rem; margin-top: 1rem; }
    label { display: grid; gap: 0.375rem; font-size: 0.875rem; }
    input { padding: 0.625rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    button { padding: 0.75rem; border: none; border-radius: 0.375rem; background: #2563eb; color: #fff; cursor: pointer; }
    button:disabled { opacity: 0.6; cursor: not-allowed; }
  `]
})
export class AdminLoginPageComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  submitting = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    this.submitting = true;
    this.errorMessage = '';
    try {
      await this.auth.login({ ...this.form.getRawValue(), rememberMe: true });
      if (!this.auth.isAdministrator()) {
        await this.auth.logout();
        this.errorMessage = 'This account does not have administrator access.';
        return;
      }
      await this.router.navigateByUrl('/dashboard');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Login failed.';
    } finally {
      this.submitting = false;
    }
  }
}
