import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthApi, CustomersApi, CartStateService } from '@commerce/api';
import { LoginRequest, RegisterCustomerRequest, SessionResponse } from '@commerce/api';
import { firstValueFrom } from 'rxjs';

const ADMINISTRATOR_ROLE = 'Administrator';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(AuthApi);
  private readonly customersApi = inject(CustomersApi);
  private readonly cartState = inject(CartStateService);
  private readonly router = inject(Router);

  private readonly sessionSignal = signal<SessionResponse | null>(null);
  private readonly loadingSignal = signal(false);

  readonly session = this.sessionSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.sessionSignal()?.isAuthenticated === true);
  readonly isAdministrator = computed(() =>
    this.sessionSignal()?.roles.includes(ADMINISTRATOR_ROLE) === true
  );
  readonly isCustomer = computed(() => (this.sessionSignal()?.customerId ?? 0) > 0);

  async initialize(): Promise<void> {
    await this.refreshSession();
  }

  async refreshSession(): Promise<SessionResponse> {
    this.loadingSignal.set(true);
    try {
      const session = await firstValueFrom(this.authApi.getSession());
      this.sessionSignal.set(session);
      return session;
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async login(request: LoginRequest): Promise<void> {
    await firstValueFrom(this.customersApi.login(request));
    await this.refreshSession();
    await this.cartState.mergeAfterLogin();
  }

  async register(request: RegisterCustomerRequest): Promise<void> {
    await firstValueFrom(this.customersApi.register(request));
    await this.refreshSession();
    await this.cartState.mergeAfterLogin();
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.customersApi.logout());
    this.sessionSignal.set({
      isAuthenticated: false,
      identityUserId: null,
      email: null,
      customerId: null,
      roles: [],
      permissions: []
    });
  }

  async logoutAndRedirect(loginUrl: string): Promise<void> {
    await this.logout();
    await this.router.navigateByUrl(loginUrl);
  }
}
