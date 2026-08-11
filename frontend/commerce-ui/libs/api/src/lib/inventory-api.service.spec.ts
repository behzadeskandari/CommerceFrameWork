import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InventoryApi } from './inventory-api.service';

describe('InventoryApi', () => {
  let api: InventoryApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [InventoryApi, provideHttpClient(), provideHttpClientTesting()]
    });
    api = TestBed.inject(InventoryApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lists inventory items', () => {
    api.list({ page: 1, pageSize: 20 }).subscribe(result => {
      expect(result.items.length).toBe(1);
      expect(result.items[0].offerId).toBe(42);
    });

    const req = httpMock.expectOne(request =>
      request.url === '/api/admin/inventory' && request.params.get('page') === '1');
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: {
        items: [{
          id: 1,
          storeId: 1,
          offerId: 42,
          productId: 10,
          variantId: null,
          trackInventory: true,
          allowBackorder: false,
          onHand: 5,
          reserved: 1,
          available: 4,
          availabilityStatus: 'InStock',
          updatedAtUtc: '2026-01-01T00:00:00Z'
        }],
        page: 1,
        pageSize: 20,
        totalCount: 1
      }
    });
  });
});
