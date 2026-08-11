import { TestBed } from '@angular/core/testing';
import { CatalogApi } from '@commerce/api';
import { of } from 'rxjs';
import { StorefrontCatalogFacade } from './storefront-catalog.facade';

describe('StorefrontCatalogFacade', () => {
  it('loads storefront products from the storefront API', async () => {
    const catalogApi = {
      listStorefrontProducts: jasmine.createSpy('listStorefrontProducts').and.returnValue(of([
        {
          id: 1,
          name: 'T-Shirt',
          sku: 'TS-001',
          productType: 'Variant',
          published: true,
          isVisible: true,
          isAvailable: true,
          deleted: false,
          displayOrder: 0,
          slug: 't-shirt'
        }
      ])),
      getStorefrontProductBySlug: jasmine.createSpy('getStorefrontProductBySlug'),
      getStorefrontProduct: jasmine.createSpy('getStorefrontProduct'),
      listCategories: jasmine.createSpy('listCategories').and.returnValue(of([])),
      getCategory: jasmine.createSpy('getCategory'),
      getProductPrice: jasmine.createSpy('getProductPrice'),
      getVariantPrice: jasmine.createSpy('getVariantPrice')
    };

    TestBed.configureTestingModule({
      providers: [
        StorefrontCatalogFacade,
        { provide: CatalogApi, useValue: catalogApi }
      ]
    });

    const facade = TestBed.inject(StorefrontCatalogFacade);
    const products = await facade.listPublishedProducts();

    expect(catalogApi.listStorefrontProducts).toHaveBeenCalled();
    expect(products.length).toBe(1);
    expect(products[0].slug).toBe('t-shirt');
  });

  it('resolves product detail by slug', async () => {
    const catalogApi = {
      listStorefrontProducts: jasmine.createSpy('listStorefrontProducts'),
      getStorefrontProductBySlug: jasmine.createSpy('getStorefrontProductBySlug').and.returnValue(of({
        id: 2,
        name: 'Hoodie',
        shortDescription: null,
        description: null,
        sku: 'HD-001',
        productType: 'Simple',
        slug: 'hoodie',
        categoryIds: [],
        configurableAttributes: [],
        variants: [],
        defaultVariantId: null,
        price: null
      })),
      getStorefrontProduct: jasmine.createSpy('getStorefrontProduct'),
      listCategories: jasmine.createSpy('listCategories').and.returnValue(of([])),
      getCategory: jasmine.createSpy('getCategory'),
      getProductPrice: jasmine.createSpy('getProductPrice'),
      getVariantPrice: jasmine.createSpy('getVariantPrice')
    };

    TestBed.configureTestingModule({
      providers: [
        StorefrontCatalogFacade,
        { provide: CatalogApi, useValue: catalogApi }
      ]
    });

    const facade = TestBed.inject(StorefrontCatalogFacade);
    const product = await facade.findProductBySlug('hoodie');

    expect(catalogApi.getStorefrontProductBySlug).toHaveBeenCalledWith('hoodie');
    expect(product?.name).toBe('Hoodie');
  });
});
