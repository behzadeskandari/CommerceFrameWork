import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DiscountsApi } from './discounts-api.service';

describe('DiscountsApi', () => {
  let api: DiscountsApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DiscountsApi, provideHttpClient(), provideHttpClientTesting()]
    });
    api = TestBed.inject(DiscountsApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lists discounts', () => {
    api.listDiscounts().subscribe(items => {
      expect(items.length).toBe(1);
      expect(items[0].name).toBe('Summer sale');
    });

    const req = httpMock.expectOne('/api/admin/discounts');
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: [{
        id: 1,
        name: 'Summer sale',
        systemName: 'summer-sale',
        discountType: 'Percentage',
        value: 10,
        currencyCode: null,
        priority: 0,
        isActive: true,
        startsAtUtc: null,
        endsAtUtc: null,
        storeId: null,
        applicationScope: 'Cart'
      }]
    });
  });

  it('creates a coupon', () => {
    api.createCoupon({
      code: 'SAVE10',
      discountId: 1,
      isActive: true
    }).subscribe(coupon => {
      expect(coupon.code).toBe('SAVE10');
    });

    const req = httpMock.expectOne('/api/admin/coupons');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ code: 'SAVE10', discountId: 1, isActive: true });
    req.flush({
      success: true,
      data: {
        id: 1,
        code: 'SAVE10',
        discountId: 1,
        discountName: 'Summer sale',
        isActive: true,
        usageCount: 0,
        globalUsageLimit: null,
        perCustomerUsageLimit: null,
        startsAtUtc: null,
        endsAtUtc: null,
        storeId: null,
        createdAtUtc: '2026-01-01T00:00:00Z',
        updatedAtUtc: '2026-01-01T00:00:00Z'
      }
    });
  });

  it('activates a discount', () => {
    api.activateDiscount(5).subscribe();

    const req = httpMock.expectOne('/api/admin/discounts/5/activate');
    expect(req.request.method).toBe('POST');
    req.flush({ success: true });
  });

  it('deletes a coupon', () => {
    api.deleteCoupon(3).subscribe();

    const req = httpMock.expectOne('/api/admin/coupons/3');
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true });
  });
});
