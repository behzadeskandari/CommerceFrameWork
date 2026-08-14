import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TaxApi } from './tax-api.service';

describe('TaxApi', () => {
  let api: TaxApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TaxApi, provideHttpClient(), provideHttpClientTesting()]
    });
    api = TestBed.inject(TaxApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lists tax categories', () => {
    api.listCategories().subscribe(items => {
      expect(items.length).toBe(1);
      expect(items[0].name).toBe('Standard');
    });

    const req = httpMock.expectOne('/api/admin/tax/categories');
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: [{
        id: 1,
        storeId: 1,
        name: 'Standard',
        systemName: 'standard',
        isExempt: false,
        isActive: true,
        displayOrder: 0
      }]
    });
  });

  it('creates a tax zone', () => {
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

    const req = httpMock.expectOne('/api/admin/tax/zones');
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

  it('deletes a tax rate', () => {
    api.deleteRate(3).subscribe();

    const req = httpMock.expectOne('/api/admin/tax/rates/3');
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true });
  });
});
