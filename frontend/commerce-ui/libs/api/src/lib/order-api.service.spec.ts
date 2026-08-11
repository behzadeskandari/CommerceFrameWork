import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { APP_CONFIG } from '@commerce/core';
import { OrdersApi } from './order-api.service';

describe('OrdersApi', () => {
  let api: OrdersApi;
  let httpMock: HttpTestingController;

  const config = { apiBaseUrl: 'http://localhost:5000' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        OrdersApi,
        { provide: APP_CONFIG, useValue: config },
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    api = TestBed.inject(OrdersApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('creates order with idempotency key header', () => {
    api.create({ checkoutId: 42 }, 'key-123').subscribe(result => {
      expect(result.orderNumber).toBe('ORD-001');
      expect(result.guestAccessToken).toBe('guest-token');
    });

    const req = httpMock.expectOne(`${config.apiBaseUrl}/api/orders`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('Idempotency-Key')).toBe('key-123');
    expect(req.request.body).toEqual({ checkoutId: 42 });
    req.flush({ success: true, data: { id: 1, orderNumber: 'ORD-001', guestAccessToken: 'guest-token' } });
  });

  it('lists customer orders with query params', () => {
    api.list({ page: 2, pageSize: 10, status: 'Pending' }).subscribe(result => {
      expect(result.items.length).toBe(1);
      expect(result.totalCount).toBe(1);
    });

    const req = httpMock.expectOne(
      request => request.url === `${config.apiBaseUrl}/api/orders` &&
        request.params.get('page') === '2' &&
        request.params.get('pageSize') === '10' &&
        request.params.get('status') === 'Pending'
    );
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: {
        items: [{
          id: 1,
          orderNumber: 'ORD-001',
          storeId: 1,
          status: 'Pending',
          paymentStatus: 'Pending',
          fulfillmentStatus: 'Unfulfilled',
          grandTotal: 100,
          currencyCode: 'IRR',
          createdAtUtc: '2026-01-01T00:00:00Z'
        }],
        page: 2,
        pageSize: 10,
        totalCount: 1
      }
    });
  });

  it('gets order by number with access token', () => {
    api.getByNumber('ORD-001', 'secret').subscribe(order => {
      expect(order.orderNumber).toBe('ORD-001');
    });

    const req = httpMock.expectOne(
      request => request.url === `${config.apiBaseUrl}/api/orders/by-number/ORD-001` &&
        request.params.get('accessToken') === 'secret'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { id: 1, orderNumber: 'ORD-001' } });
  });

  it('cancels order', () => {
    api.cancel(5, { reason: 'Changed mind' }).subscribe(order => {
      expect(order.id).toBe(5);
    });

    const req = httpMock.expectOne(`${config.apiBaseUrl}/api/orders/5/cancel`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reason: 'Changed mind' });
    req.flush({ success: true, data: { id: 5, orderNumber: 'ORD-005' } });
  });
});
