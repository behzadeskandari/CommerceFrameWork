import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CheckoutApi, CheckoutStateService } from '@commerce/api';
import { ApiClientError } from '@commerce/core';

describe('CheckoutStateService', () => {
  let service: CheckoutStateService;
  let checkoutApi: jasmine.SpyObj<CheckoutApi>;

  const sampleSession = {
    id: 1,
    cartId: 1,
    storeId: 1,
    status: 'Active' as const,
    currency: 'IRR',
    currencyId: 1,
    customer: { customerId: null, email: null, isGuest: true },
    useShippingAsBilling: false,
    requiresShipping: true,
    priceChangeDetected: false,
    items: [],
    totals: {
      subtotal: 100,
      discountTotal: 0,
      shippingTotal: 0,
      taxTotal: 0,
      productTaxTotal: 0,
      shippingTaxTotal: 0,
      grandTotal: 100,
      giftCardApplied: 0,
      storeCreditApplied: 0,
      walletAdjustmentTotal: 0,
      currency: 'IRR',
      pricesIncludeTax: false,
      taxLines: [],
      taxLineItems: []
    },
    shippingOptions: [],
    paymentMethods: [],
    validationErrors: [],
    warnings: [],
    expiresAtUtc: new Date().toISOString(),
    cartUpdatedAtUtc: new Date().toISOString()
  };

  beforeEach(() => {
    checkoutApi = jasmine.createSpyObj<CheckoutApi>('CheckoutApi', [
      'start',
      'get',
      'setGuestContact',
      'setBillingAddress',
      'setShippingAddress',
      'validate',
      'refresh'
    ]);
    checkoutApi.start.and.returnValue(of(sampleSession));

    TestBed.configureTestingModule({
      providers: [
        CheckoutStateService,
        { provide: CheckoutApi, useValue: checkoutApi }
      ]
    });

    service = TestBed.inject(CheckoutStateService);
  });

  it('starts checkout and stores session', async () => {
    const session = await service.start();
    expect(checkoutApi.start).toHaveBeenCalled();
    expect(session.id).toBe(1);
    expect(service.session()?.id).toBe(1);
  });

  it('computes visible steps for guest physical cart', () => {
    const steps = service.visibleSteps(sampleSession);
    expect(steps).toEqual(['contact', 'billing', 'shipping', 'shippingMethod', 'payment', 'review']);
  });

  it('hides shipping steps for digital-only cart', () => {
    const steps = service.visibleSteps({ ...sampleSession, requiresShipping: false, customer: { customerId: 5, email: 'a@b.com', isGuest: false } });
    expect(steps).toEqual(['billing', 'payment', 'review']);
  });

  it('stores validation result', async () => {
    await service.start();
    checkoutApi.validate.and.returnValue(of({
      checkout: { ...sampleSession, status: 'ReadyForOrder' },
      isValid: true,
      isReadyForOrder: true,
      errors: [],
      warnings: []
    }));

    const result = await service.validate();
    expect(result.isReadyForOrder).toBeTrue();
    expect(service.isReadyForOrder()).toBeTrue();
  });

  it('surfaces api errors', async () => {
    checkoutApi.start.and.returnValue(throwError(() => new ApiClientError('Empty cart', 400)));
    await expectAsync(service.start()).toBeRejected();
    expect(service.error()).toBe('Empty cart');
  });
});
