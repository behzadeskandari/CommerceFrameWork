import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ShippingApi } from './shipping-api.service';

describe('ShippingApi', () => {
  let api: ShippingApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ShippingApi, provideHttpClient(), provideHttpClientTesting()]
    });
    api = TestBed.inject(ShippingApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lists shipping methods', () => {
    api.listMethods().subscribe(items => {
      expect(items.length).toBe(1);
      expect(items[0].name).toBe('Flat Rate');
    });

    const req = httpMock.expectOne('/api/admin/shipping/methods');
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: [{
        id: 1,
        storeId: 1,
        name: 'Flat Rate',
        systemName: 'flat-rate',
        providerSystemName: 'Shipping.FlatRate',
        isActive: true,
        displayOrder: 0
      }]
    });
  });

  it('creates a shipping zone', () => {
    api.createZone({
      storeId: 1,
      name: 'Default Zone',
      systemName: 'default',
      isDefault: true,
      isActive: true,
      displayOrder: 0,
      countries: [{ countryCode: 'US' }],
      states: [],
      postalRules: []
    }).subscribe(zone => {
      expect(zone.name).toBe('Default Zone');
    });

    const req = httpMock.expectOne('/api/admin/shipping/zones');
    expect(req.request.method).toBe('POST');
    req.flush({
      success: true,
      data: {
        id: 1,
        storeId: 1,
        name: 'Default Zone',
        systemName: 'default',
        isDefault: true,
        isActive: true,
        displayOrder: 0,
        countries: [{ countryCode: 'US' }],
        states: [],
        postalRules: [],
        createdAtUtc: '2026-01-01T00:00:00Z',
        updatedAtUtc: '2026-01-01T00:00:00Z'
      }
    });
  });

  it('deletes a shipping rate', () => {
    api.deleteRate(4).subscribe();

    const req = httpMock.expectOne('/api/admin/shipping/rates/4');
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true });
  });
});
