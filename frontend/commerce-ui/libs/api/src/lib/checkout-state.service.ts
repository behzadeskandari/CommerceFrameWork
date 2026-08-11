import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiClientError } from '@commerce/core';
import { firstValueFrom } from 'rxjs';
import { CheckoutApi } from './checkout-api.service';
import {
  CheckoutSession,
  CheckoutStep,
  CheckoutValidationResult,
  SetBillingAddressRequest,
  SetShippingAddressRequest
} from './models/checkout.models';

@Injectable({ providedIn: 'root' })
export class CheckoutStateService {
  private readonly checkoutApi = inject(CheckoutApi);

  private readonly sessionSignal = signal<CheckoutSession | null>(null);
  private readonly stepSignal = signal<CheckoutStep>('contact');
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);
  private readonly validationSignal = signal<CheckoutValidationResult | null>(null);

  readonly session = this.sessionSignal.asReadonly();
  readonly step = this.stepSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly validation = this.validationSignal.asReadonly();

  readonly isReadyForOrder = computed(() => this.validationSignal()?.isReadyForOrder ?? false);
  readonly warnings = computed(() => this.sessionSignal()?.warnings ?? []);
  readonly validationErrors = computed(() => this.sessionSignal()?.validationErrors ?? []);

  async start(): Promise<CheckoutSession> {
    return this.run(() => this.checkoutApi.start());
  }

  async load(checkoutId: number): Promise<CheckoutSession> {
    return this.run(() => this.checkoutApi.get(checkoutId));
  }

  async setGuestContact(email: string): Promise<CheckoutSession> {
    const id = this.requireSessionId();
    return this.run(() => this.checkoutApi.setGuestContact(id, email));
  }

  async setBillingAddress(request: SetBillingAddressRequest): Promise<CheckoutSession> {
    const id = this.requireSessionId();
    return this.run(() => this.checkoutApi.setBillingAddress(id, request));
  }

  async setShippingAddress(request: SetShippingAddressRequest): Promise<CheckoutSession> {
    const id = this.requireSessionId();
    return this.run(() => this.checkoutApi.setShippingAddress(id, request));
  }

  async selectShippingMethod(methodId: string, providerSystemName: string): Promise<CheckoutSession> {
    const id = this.requireSessionId();
    return this.run(() => this.checkoutApi.selectShippingMethod(id, methodId, providerSystemName));
  }

  async selectPaymentMethod(methodId: string, systemName: string): Promise<CheckoutSession> {
    const id = this.requireSessionId();
    return this.run(() => this.checkoutApi.selectPaymentMethod(id, methodId, systemName));
  }

  async validate(): Promise<CheckoutValidationResult> {
    const id = this.requireSessionId();
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const result = await firstValueFrom(this.checkoutApi.validate(id));
      this.sessionSignal.set(result.checkout);
      this.validationSignal.set(result);
      return result;
    } catch (error) {
      this.errorSignal.set(error instanceof ApiClientError ? error.message : 'Checkout validation failed.');
      throw error;
    } finally {
      this.loadingSignal.set(false);
    }
  }

  async refresh(): Promise<CheckoutSession> {
    const id = this.requireSessionId();
    return this.run(() => this.checkoutApi.refresh(id));
  }

  setStep(step: CheckoutStep): void {
    this.stepSignal.set(step);
  }

  reset(): void {
    this.sessionSignal.set(null);
    this.stepSignal.set('contact');
    this.validationSignal.set(null);
    this.errorSignal.set(null);
  }

  visibleSteps(session: CheckoutSession): CheckoutStep[] {
    const steps: CheckoutStep[] = [];
    if (session.customer.isGuest) {
      steps.push('contact');
    }
    steps.push('billing');
    if (session.requiresShipping) {
      steps.push('shipping', 'shippingMethod');
    }
    steps.push('payment', 'review');
    return steps;
  }

  private requireSessionId(): number {
    const id = this.sessionSignal()?.id;
    if (!id) {
      throw new Error('Checkout session is not initialized.');
    }
    return id;
  }

  private async run(request: () => import('rxjs').Observable<CheckoutSession>): Promise<CheckoutSession> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);
    try {
      const session = await firstValueFrom(request());
      this.sessionSignal.set(session);
      return session;
    } catch (error) {
      this.errorSignal.set(error instanceof ApiClientError ? error.message : 'Checkout request failed.');
      throw error;
    } finally {
      this.loadingSignal.set(false);
    }
  }
}
