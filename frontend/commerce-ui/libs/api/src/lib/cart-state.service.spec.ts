import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CartApi, CartStateService } from '@commerce/api';
import { ApiClientError } from '@commerce/core';

describe('CartStateService', () => {
  let service: CartStateService;
  let cartApi: jasmine.SpyObj<CartApi>;

  const sampleCart = {
    id: 1,
    storeId: 1,
    currency: 'IRR',
    currencyId: 1,
    items: [],
    totals: {
      subtotal: 0,
      discountTotal: 0,
      shippingTotal: 0,
      taxTotal: 0,
      grandTotal: 0,
      currency: 'IRR'
    },
    itemCount: 0
  };

  beforeEach(() => {
    cartApi = jasmine.createSpyObj<CartApi>('CartApi', [
      'getCart',
      'addItem',
      'updateItem',
      'removeItem',
      'clearCart',
      'mergeGuestCart'
    ]);
    cartApi.getCart.and.returnValue(of(sampleCart));

    TestBed.configureTestingModule({
      providers: [
        CartStateService,
        { provide: CartApi, useValue: cartApi }
      ]
    });

    service = TestBed.inject(CartStateService);
  });

  it('loads cart on initialize', async () => {
    await service.initialize();
    expect(cartApi.getCart).toHaveBeenCalled();
    expect(service.itemCount()).toBe(0);
  });

  it('updates count after add item', async () => {
    cartApi.addItem.and.returnValue(of({ ...sampleCart, itemCount: 2 }));
    await service.addItem(10, 2);
    expect(service.itemCount()).toBe(2);
  });

  it('refreshes authoritative state when mutation fails', async () => {
    cartApi.updateItem.and.returnValue(throwError(() => new ApiClientError('Invalid quantity', 400)));
    cartApi.getCart.and.returnValue(of({ ...sampleCart, itemCount: 1 }));

    await expectAsync(service.updateQuantity(5, 99)).toBeRejected();
    expect(cartApi.getCart).toHaveBeenCalledTimes(1);
  });
});
