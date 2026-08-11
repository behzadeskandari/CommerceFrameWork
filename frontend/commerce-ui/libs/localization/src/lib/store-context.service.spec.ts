import { TestBed } from '@angular/core/testing';
import { StoreContextService } from './store-context.service';
import { LocalizationService } from './localization.service';
import { StoreApi } from '@commerce/api';
import { of } from 'rxjs';

describe('StoreContextService', () => {
  it('formats currency using backend configuration', async () => {
    const storeApi = {
      getContext: () => of({
        storeId: 1,
        storeSystemName: 'primary-store',
        storeName: 'Primary Store',
        languageId: 1,
        languageCode: 'en',
        cultureCode: 'en-US',
        isRtl: false,
        currencyId: 1,
        currencyCode: 'IRR'
      }),
      listCurrencies: () => of([
        {
          id: 1,
          code: 'IRR',
          name: 'Iranian Rial',
          symbol: 'ریال',
          displayName: 'Iranian Rial',
          decimalPlaces: 0,
          rate: 1,
          isActive: true,
          displayOrder: 0,
          createdAtUtc: '',
          updatedAtUtc: ''
        }
      ]),
      selectLanguage: () => of(undefined)
    };

    TestBed.configureTestingModule({
      providers: [
        StoreContextService,
        LocalizationService,
        { provide: StoreApi, useValue: storeApi }
      ]
    });

    const service = TestBed.inject(StoreContextService);
    await service.initialize();

    expect(service.formatAmount(100000)).toContain('100');
    expect(service.direction()).toBe('ltr');
  });
});
