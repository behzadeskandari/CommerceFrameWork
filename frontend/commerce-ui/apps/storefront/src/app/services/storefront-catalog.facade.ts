import { Injectable, inject } from '@angular/core';
import { CatalogApi, CategorySummary, ProductSummary, ResolvedPrice, StorefrontProductDetail } from '@commerce/api';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class StorefrontCatalogFacade {
  private readonly catalogApi = inject(CatalogApi);

  listPublishedProducts(term?: string): Promise<ProductSummary[]> {
    return firstValueFrom(this.catalogApi.listStorefrontProducts(term));
  }

  listPublishedCategories(): Promise<CategorySummary[]> {
    return firstValueFrom(this.catalogApi.listCategories()).then(categories =>
      categories.filter(category => category.published)
    );
  }

  async findProductBySlug(slug: string): Promise<StorefrontProductDetail | null> {
    if (/^\d+$/.test(slug)) {
      try {
        return await firstValueFrom(this.catalogApi.getStorefrontProduct(Number(slug)));
      } catch {
        return null;
      }
    }

    try {
      return await firstValueFrom(this.catalogApi.getStorefrontProductBySlug(slug));
    } catch {
      return null;
    }
  }

  resolveProductPrice(productId: number, currencyId?: number): Promise<ResolvedPrice> {
    return firstValueFrom(this.catalogApi.getProductPrice(productId, currencyId));
  }

  resolveVariantPrice(variantId: number, currencyId?: number): Promise<ResolvedPrice> {
    return firstValueFrom(this.catalogApi.getVariantPrice(variantId, currencyId));
  }

  async getCategoryDetail(id: number) {
    return firstValueFrom(this.catalogApi.getCategory(id));
  }

  async findCategoryBySlug(slug: string): Promise<CategorySummary | null> {
    const categories = await this.listPublishedCategories();
    return categories.find(category => category.slug === slug) ?? null;
  }
}
