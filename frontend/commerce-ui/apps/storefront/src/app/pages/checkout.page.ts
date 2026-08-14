import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import {
  CheckoutSession,
  CheckoutStateService,
  CustomerAddress,
  CustomersApi,
  CheckoutStep,
  OrdersApi,
  PaymentsApi
} from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    CurrencyFormatPipe,
    LoadingStateComponent,
    ErrorStateComponent
  ],
  template: `
    <section class="checkout-page" aria-labelledby="checkout-title">
      <header>
        <h1 id="checkout-title">{{ 'checkout.title' | translate }}</h1>
      </header>

      @if (state === 'loading') { <cmr-loading-state /> }
      @else if (checkout.error()) { <cmr-error-state [message]="checkout.error()!" (retry)="init()" /> }
      @else if (session) {
        <nav class="steps" [attr.aria-label]="'checkout.steps' | translate">
          @for (step of steps; track step; let index = $index) {
            <button
              type="button"
              class="step"
              [class.active]="currentStep === step"
              [class.done]="stepIndex(step) < stepIndex(currentStep)"
              (click)="goTo(step)"
              [attr.aria-current]="currentStep === step ? 'step' : null">
              {{ ('checkout.step.' + step) | translate }}
            </button>
          }
        </nav>

        @if (session.priceChangeDetected || session.warnings.length) {
          <div class="banner warning" role="status">
            @for (warning of session.warnings; track warning) { <p>{{ warning }}</p> }
          </div>
        }

        @if (session.validationErrors.length) {
          <div class="banner error" role="alert">
            @for (err of session.validationErrors; track err) { <p>{{ err }}</p> }
          </div>
        }

        @switch (currentStep) {
          @case ('contact') {
            <form [formGroup]="contactForm" (ngSubmit)="saveContact()" class="panel">
              <label>{{ 'auth.email' | translate }}<input type="email" formControlName="email" autocomplete="email" /></label>
              <button type="submit" [disabled]="contactForm.invalid || checkout.loading()">{{ 'action.continue' | translate }}</button>
            </form>
          }
          @case ('billing') {
            <div class="panel">
              @if (savedAddresses.length) {
                <fieldset>
                  <legend>{{ 'checkout.savedAddresses' | translate }}</legend>
                  @for (address of savedAddresses; track address.id) {
                    <label class="address-option">
                      <input type="radio" name="billingAddress" [value]="address.id" (change)="selectBillingAddress(address.id)" />
                      {{ address.firstName }} {{ address.lastName }} — {{ address.address1 }}, {{ address.city }}
                    </label>
                  }
                </fieldset>
              }
              <form [formGroup]="addressForm" (ngSubmit)="saveBillingAddress()">
                <label>{{ 'auth.firstName' | translate }}<input formControlName="firstName" /></label>
                <label>{{ 'auth.lastName' | translate }}<input formControlName="lastName" /></label>
                <label>{{ 'checkout.country' | translate }}<input formControlName="country" /></label>
                <label>{{ 'checkout.city' | translate }}<input formControlName="city" /></label>
                <label>{{ 'checkout.address' | translate }}<input formControlName="address1" /></label>
                <label>{{ 'checkout.postalCode' | translate }}<input formControlName="postalCode" /></label>
                <label class="checkbox">
                  <input type="checkbox" formControlName="useShippingAsBilling" />
                  {{ 'checkout.useShippingAsBilling' | translate }}
                </label>
                <button type="submit" [disabled]="addressForm.invalid || checkout.loading()">{{ 'action.continue' | translate }}</button>
              </form>
            </div>
          }
          @case ('shipping') {
            <form [formGroup]="shippingForm" (ngSubmit)="saveShippingAddress()" class="panel">
              <label>{{ 'auth.firstName' | translate }}<input formControlName="firstName" /></label>
              <label>{{ 'auth.lastName' | translate }}<input formControlName="lastName" /></label>
              <label>{{ 'checkout.country' | translate }}<input formControlName="country" /></label>
              <label>{{ 'checkout.city' | translate }}<input formControlName="city" /></label>
              <label>{{ 'checkout.address' | translate }}<input formControlName="address1" /></label>
              <label>{{ 'checkout.postalCode' | translate }}<input formControlName="postalCode" /></label>
              <button type="submit" [disabled]="shippingForm.invalid || checkout.loading()">{{ 'action.continue' | translate }}</button>
            </form>
          }
          @case ('shippingMethod') {
            <div class="panel">
              @if (session.shippingOptions.length === 0) {
                <p role="status">{{ 'checkout.noShipping' | translate }}</p>
              } @else {
                @for (option of session.shippingOptions; track option.id) {
                  <label class="option-row">
                    <input type="radio" name="shipping" [checked]="session.selectedShippingMethodId === option.id" (change)="selectShipping(option.id, option.providerSystemName)" />
                    <span>{{ option.name }}</span>
                    <strong>{{ option.price | currencyFormat: option.currency }}</strong>
                  </label>
                }
              }
              <button type="button" class="secondary" (click)="nextFromShippingMethod()">{{ 'action.continue' | translate }}</button>
            </div>
          }
          @case ('payment') {
            <div class="panel">
              @if (session.paymentMethods.length === 0) {
                <p role="status">{{ 'checkout.noPayment' | translate }}</p>
              } @else {
                @for (method of session.paymentMethods; track method.id) {
                  <label class="option-row">
                    <input type="radio" name="payment" [checked]="session.selectedPaymentMethodId === method.id" (change)="selectPayment(method.id, method.systemName)" />
                    <span>{{ method.displayName }}</span>
                  </label>
                }
              }
              <button type="button" class="secondary" (click)="goTo('review')">{{ 'action.continue' | translate }}</button>
            </div>
          }
          @case ('review') {
            <div class="panel review">
              <ul class="items" role="list">
                @for (item of session.items; track item.cartItemId) {
                  <li>
                    <span>{{ item.productName }} × {{ item.quantity }}</span>
                    <strong>{{ item.lineSubtotal | currencyFormat: item.currency }}</strong>
                  </li>
                }
              </ul>
              <dl class="totals">
                <div><dt>{{ 'cart.subtotal' | translate }}</dt><dd>{{ session.totals.subtotal | currencyFormat: session.currency }}</dd></div>
                @if (session.totals.discountTotal > 0) {
                  <div class="discount"><dt>{{ 'cart.discount' | translate }}</dt><dd>−{{ session.totals.discountTotal | currencyFormat: session.currency }}</dd></div>
                }
                <div><dt>{{ 'checkout.shipping' | translate }}</dt><dd>{{ session.totals.shippingTotal | currencyFormat: session.currency }}</dd></div>
                @if (session.totals.productTaxTotal > 0) {
                  <div><dt>{{ 'checkout.productTax' | translate }}</dt><dd>{{ session.totals.productTaxTotal | currencyFormat: session.currency }}</dd></div>
                }
                @if (session.totals.shippingTaxTotal > 0) {
                  <div><dt>{{ 'checkout.shippingTax' | translate }}</dt><dd>{{ session.totals.shippingTaxTotal | currencyFormat: session.currency }}</dd></div>
                }
                <div><dt>{{ 'checkout.tax' | translate }}</dt><dd>{{ session.totals.taxTotal | currencyFormat: session.currency }}</dd></div>
                @if (session.totals.pricesIncludeTax) {
                  <div class="note"><dt>{{ 'checkout.pricesIncludeTax' | translate }}</dt><dd>{{ 'tax.yes' | translate }}</dd></div>
                }
                @if (session.totals.taxLines.length) {
                  <div class="tax-lines">
                    @for (line of session.totals.taxLines; track line.name) {
                      <div class="tax-line">
                        <dt>{{ line.name }}@if (line.ratePercentage != null) { ({{ line.ratePercentage }}%) }</dt>
                        <dd>{{ line.amount | currencyFormat: session.currency }}</dd>
                      </div>
                    }
                  </div>
                }
                <div class="grand"><dt>{{ 'checkout.grandTotal' | translate }}</dt><dd>{{ session.totals.grandTotal | currencyFormat: session.currency }}</dd></div>
              </dl>
              <button type="button" class="primary" (click)="finalize()" [disabled]="checkout.loading() || placingOrder">
                @if (checkout.isReadyForOrder()) {
                  {{ 'checkout.placeOrder' | translate }}
                } @else {
                  {{ 'checkout.readyForOrder' | translate }}
                }
              </button>
              @if (checkout.isReadyForOrder()) {
                <p class="success" role="status">{{ 'checkout.readyMessage' | translate }}</p>
              }
              @if (orderError) {
                <p class="order-error" role="alert">{{ orderError }}</p>
              }
            </div>
          }
        }
      }
    </section>
  `,
  styles: [`
    .checkout-page { display: grid; gap: 1.25rem; }
    .steps { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .step {
      border: 1px solid #d1d5db; background: #fff; border-radius: 999px; padding: 0.375rem 0.875rem; cursor: pointer;
    }
    .step.active { background: var(--primary, #0f766e); color: #fff; border-color: transparent; }
    .step.done { border-color: #86efac; }
    .panel { display: grid; gap: 0.75rem; max-width: 32rem; }
    label { display: grid; gap: 0.25rem; }
    input[type="text"], input[type="email"] { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .banner { padding: 0.75rem 1rem; border-radius: 0.5rem; }
    .banner.warning { background: #fffbeb; border: 1px solid #fcd34d; }
    .banner.error { background: #fef2f2; border: 1px solid #fca5a5; }
    .option-row, .address-option { display: flex; gap: 0.75rem; align-items: center; padding: 0.5rem 0; }
    .items { list-style: none; padding: 0; margin: 0; display: grid; gap: 0.5rem; }
    .items li { display: flex; justify-content: space-between; gap: 1rem; }
    .totals { display: grid; gap: 0.375rem; margin: 1rem 0; }
    .totals div { display: flex; justify-content: space-between; gap: 1rem; }
    .totals .discount { color: #047857; }
    .totals .note { font-size: 0.875rem; color: #6b7280; }
    .tax-lines { margin-top: 0.25rem; padding-inline-start: 0.75rem; border-inline-start: 2px solid #e5e7eb; }
    .tax-line { display: flex; justify-content: space-between; gap: 1rem; font-size: 0.875rem; color: #4b5563; }
    .grand { font-weight: 700; border-top: 1px solid #e5e7eb; padding-top: 0.5rem; }
    button.primary, button.secondary, form button[type="submit"] {
      width: fit-content; padding: 0.625rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer;
    }
    button.primary, form button[type="submit"] { background: var(--primary, #0f766e); color: #fff; }
    button.secondary { background: #fff; border: 1px solid #d1d5db; }
    .success { color: #047857; font-weight: 600; }
    .order-error { color: #b91c1c; }
    .checkbox { display: flex; align-items: center; gap: 0.5rem; }
    @media (max-width: 640px) { .steps { flex-direction: column; } }
  `]
})
export class CheckoutPageComponent implements OnInit {
  readonly checkout = inject(CheckoutStateService);
  private readonly customersApi = inject(CustomersApi);
  private readonly ordersApi = inject(OrdersApi);
  private readonly paymentsApi = inject(PaymentsApi);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  state: PageState = 'loading';
  session: CheckoutSession | null = null;
  steps: CheckoutStep[] = [];
  currentStep: CheckoutStep = 'contact';
  savedAddresses: CustomerAddress[] = [];
  placingOrder = false;
  orderError = '';

  readonly contactForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  readonly addressForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    country: ['', Validators.required],
    city: ['', Validators.required],
    address1: ['', Validators.required],
    postalCode: ['', Validators.required],
    useShippingAsBilling: [false]
  });

  readonly shippingForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    country: ['', Validators.required],
    city: ['', Validators.required],
    address1: ['', Validators.required],
    postalCode: ['', Validators.required]
  });

  ngOnInit(): void {
    void this.init();
  }

  async init(): Promise<void> {
    this.state = 'loading';
    try {
      const session = await this.checkout.start();
      this.bindSession(session);
      if (!session.customer.isGuest) {
        this.savedAddresses = await firstValueFrom(this.customersApi.listAddresses());
      }
      this.state = 'success';
    } catch {
      this.state = 'error';
    }
  }

  stepIndex(step: CheckoutStep): number {
    return this.steps.indexOf(step);
  }

  goTo(step: CheckoutStep): void {
    this.currentStep = step;
    this.checkout.setStep(step);
  }

  async saveContact(): Promise<void> {
    if (this.contactForm.invalid) return;
    const session = await this.checkout.setGuestContact(this.contactForm.controls.email.value);
    this.bindSession(session);
    this.goTo(this.nextStep('contact'));
  }

  async selectBillingAddress(addressId: number): Promise<void> {
    const session = await this.checkout.setBillingAddress({ customerAddressId: addressId });
    this.bindSession(session);
    this.goTo(this.nextStep('billing'));
  }

  async saveBillingAddress(): Promise<void> {
    if (this.addressForm.invalid) return;
    const value = this.addressForm.getRawValue();
    const session = await this.checkout.setBillingAddress({
      address: {
        firstName: value.firstName,
        lastName: value.lastName,
        country: value.country,
        city: value.city,
        address1: value.address1,
        postalCode: value.postalCode
      },
      useShippingAsBilling: value.useShippingAsBilling
    });
    this.bindSession(session);
    this.goTo(this.nextStep('billing'));
  }

  async saveShippingAddress(): Promise<void> {
    if (this.shippingForm.invalid) return;
    const value = this.shippingForm.getRawValue();
    const session = await this.checkout.setShippingAddress({ address: value });
    this.bindSession(session);
    this.goTo(this.nextStep('shipping'));
  }

  async selectShipping(methodId: string, providerSystemName: string): Promise<void> {
    const session = await this.checkout.selectShippingMethod(methodId, providerSystemName);
    this.bindSession(session);
  }

  nextFromShippingMethod(): void {
    this.goTo(this.nextStep('shippingMethod'));
  }

  async selectPayment(methodId: string, systemName: string): Promise<void> {
    const session = await this.checkout.selectPaymentMethod(methodId, systemName);
    this.bindSession(session);
  }

  async finalize(): Promise<void> {
    this.orderError = '';
    if (!this.checkout.isReadyForOrder()) {
      await this.checkout.validate();
      this.session = this.checkout.session();
      return;
    }

    const checkoutId = this.session?.id ?? this.checkout.session()?.id;
    if (!checkoutId || !this.session) return;

    const grandTotal = this.session.totals.grandTotal;
    const paymentMethodId = this.session.selectedPaymentMethodId;

    this.placingOrder = true;
    try {
      const result = await firstValueFrom(
        this.ordersApi.create({ checkoutId }, crypto.randomUUID())
      );
      this.checkout.reset();

      const guestQuery = result.guestAccessToken
        ? { accessToken: result.guestAccessToken }
        : {};

      if (grandTotal === 0) {
        await this.router.navigate(['/order-confirmation', result.orderNumber], {
          queryParams: Object.keys(guestQuery).length ? guestQuery : undefined
        });
        return;
      }

      try {
        const payment = await firstValueFrom(
          this.paymentsApi.createPayment(
            {
              orderId: result.id,
              paymentMethodId: paymentMethodId ?? undefined
            },
            crypto.randomUUID()
          )
        );

        const baseQuery = { orderNumber: result.orderNumber, ...guestQuery };

        if (payment.status === 'Captured') {
          await this.router.navigate(['/payment/success'], { queryParams: baseQuery });
          return;
        }

        if (
          payment.status === 'Initiated' ||
          payment.status === 'Authorized' ||
          payment.status === 'RedirectRequired'
        ) {
          await this.router.navigate(['/payment/processing'], {
            queryParams: {
              ...baseQuery,
              instructions: payment.instructions ?? '',
              paymentId: payment.paymentId,
              ...(payment.redirectUrl ? { redirectUrl: payment.redirectUrl } : {})
            }
          });
          return;
        }

        await this.router.navigate(['/payment/failed'], {
          queryParams: { orderNumber: result.orderNumber }
        });
      } catch {
        await this.router.navigate(['/payment/failed'], {
          queryParams: { orderNumber: result.orderNumber }
        });
      }
    } catch (error) {
      this.orderError = error instanceof ApiClientError ? error.message : 'Failed to place order.';
    } finally {
      this.placingOrder = false;
    }
  }

  private bindSession(session: CheckoutSession): void {
    this.session = session;
    this.steps = this.checkout.visibleSteps(session);
    this.currentStep = this.steps[0];
    this.checkout.setStep(this.currentStep);
    if (session.customer.email) {
      this.contactForm.patchValue({ email: session.customer.email });
    }
  }

  private nextStep(current: CheckoutStep): CheckoutStep {
    const index = this.steps.indexOf(current);
    return this.steps[Math.min(index + 1, this.steps.length - 1)];
  }
}
