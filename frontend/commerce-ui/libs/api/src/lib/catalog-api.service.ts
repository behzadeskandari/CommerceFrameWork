import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG } from '@commerce/core';
import { ApiResponse } from '@commerce/core';
import {
  AttributeDefinition,
  CategoryDetail,
  CategorySummary,
  CreateAttributeDefinitionRequest,
  CreateAttributeOptionRequest,
  CreateCategoryRequest,
  CreateOfferRequest,
  CreateProductRequest,
  CreateVariantRequest,
  GenerateVariantsRequest,
  OfferDetail,
  OfferSummary,
  ProductAttributeAssignment,
  ProductDetail,
  ProductSummary,
  ResolvedPrice,
  SetProductAttributeValueRequest,
  StorefrontProductDetail,
  UpdateAttributeDefinitionRequest,
  UpdateAttributeOptionRequest,
  UpdateCategoryRequest,
  UpdateOfferRequest,
  UpdateProductRequest,
  UpdateVariantRequest,
  VariantDetail,
  VariantSummary
} from './models/catalog.models';

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly base = `${this.config.apiBaseUrl}/api/catalog`;

  listProducts(): Observable<ProductSummary[]> {
    return this.http
      .get<ApiResponse<ProductSummary[]>>(`${this.base}/products`)
      .pipe(map(response => response.data ?? []));
  }

  getProduct(id: number): Observable<ProductDetail> {
    return this.http
      .get<ApiResponse<ProductDetail>>(`${this.base}/products/${id}`)
      .pipe(map(response => response.data!));
  }

  createProduct(request: CreateProductRequest): Observable<ProductDetail> {
    return this.http
      .post<ApiResponse<ProductDetail>>(`${this.base}/products`, request)
      .pipe(map(response => response.data!));
  }

  updateProduct(id: number, request: UpdateProductRequest): Observable<ProductDetail> {
    return this.http
      .put<ApiResponse<ProductDetail>>(`${this.base}/products/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteProduct(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.base}/products/${id}`)
      .pipe(map(() => undefined));
  }

  listCategories(): Observable<CategorySummary[]> {
    return this.http
      .get<ApiResponse<CategorySummary[]>>(`${this.base}/categories`)
      .pipe(map(response => response.data ?? []));
  }

  getCategory(id: number): Observable<CategoryDetail> {
    return this.http
      .get<ApiResponse<CategoryDetail>>(`${this.base}/categories/${id}`)
      .pipe(map(response => response.data!));
  }

  createCategory(request: CreateCategoryRequest): Observable<CategoryDetail> {
    return this.http
      .post<ApiResponse<CategoryDetail>>(`${this.base}/categories`, request)
      .pipe(map(response => response.data!));
  }

  updateCategory(id: number, request: UpdateCategoryRequest): Observable<CategoryDetail> {
    return this.http
      .put<ApiResponse<CategoryDetail>>(`${this.base}/categories/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteCategory(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.base}/categories/${id}`)
      .pipe(map(() => undefined));
  }

  listAttributes(includeInactive = false): Observable<AttributeDefinition[]> {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return this.http
      .get<ApiResponse<AttributeDefinition[]>>(`${this.base}/attributes`, { params })
      .pipe(map(response => response.data ?? []));
  }

  getAttribute(id: number): Observable<AttributeDefinition> {
    return this.http
      .get<ApiResponse<AttributeDefinition>>(`${this.base}/attributes/${id}`)
      .pipe(map(response => response.data!));
  }

  createAttribute(request: CreateAttributeDefinitionRequest): Observable<AttributeDefinition> {
    return this.http
      .post<ApiResponse<AttributeDefinition>>(`${this.base}/attributes`, request)
      .pipe(map(response => response.data!));
  }

  updateAttribute(id: number, request: UpdateAttributeDefinitionRequest): Observable<AttributeDefinition> {
    return this.http
      .put<ApiResponse<AttributeDefinition>>(`${this.base}/attributes/${id}`, request)
      .pipe(map(response => response.data!));
  }

  createAttributeOption(attributeId: number, request: CreateAttributeOptionRequest): Observable<AttributeDefinition> {
    return this.http
      .post<ApiResponse<AttributeDefinition>>(`${this.base}/attributes/${attributeId}/options`, request)
      .pipe(map(response => response.data!));
  }

  updateAttributeOption(optionId: number, request: UpdateAttributeOptionRequest): Observable<AttributeDefinition> {
    return this.http
      .put<ApiResponse<AttributeDefinition>>(`${this.base}/attributes/options/${optionId}`, request)
      .pipe(map(response => response.data!));
  }

  getProductAttributes(productId: number): Observable<ProductAttributeAssignment[]> {
    return this.http
      .get<ApiResponse<ProductAttributeAssignment[]>>(`${this.base}/attributes/products/${productId}`)
      .pipe(map(response => response.data ?? []));
  }

  assignAttributeToProduct(productId: number, attributeId: number): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.base}/attributes/products/${productId}/${attributeId}`, null)
      .pipe(map(() => undefined));
  }

  removeAttributeFromProduct(productId: number, attributeId: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.base}/attributes/products/${productId}/${attributeId}`)
      .pipe(map(() => undefined));
  }

  setProductAttributeValue(productId: number, request: SetProductAttributeValueRequest): Observable<void> {
    return this.http
      .put<ApiResponse<unknown>>(`${this.base}/attributes/products/${productId}/values`, request)
      .pipe(map(() => undefined));
  }

  getVariant(id: number): Observable<VariantDetail> {
    return this.http
      .get<ApiResponse<VariantDetail>>(`${this.base}/variants/${id}`)
      .pipe(map(response => response.data!));
  }

  updateVariant(id: number, request: UpdateVariantRequest): Observable<VariantDetail> {
    return this.http
      .put<ApiResponse<VariantDetail>>(`${this.base}/variants/${id}`, request)
      .pipe(map(response => response.data!));
  }

  deleteVariant(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.base}/variants/${id}`)
      .pipe(map(() => undefined));
  }

  listProductVariants(productId: number, includeInactive = false): Observable<VariantSummary[]> {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return this.http
      .get<ApiResponse<VariantSummary[]>>(`${this.base}/products/${productId}/variants`, { params })
      .pipe(map(response => response.data ?? []));
  }

  createVariant(productId: number, request: CreateVariantRequest): Observable<VariantDetail> {
    return this.http
      .post<ApiResponse<VariantDetail>>(`${this.base}/products/${productId}/variants`, request)
      .pipe(map(response => response.data!));
  }

  generateVariants(productId: number, request: GenerateVariantsRequest): Observable<VariantSummary[]> {
    return this.http
      .post<ApiResponse<VariantSummary[]>>(`${this.base}/products/${productId}/variants/generate`, request)
      .pipe(map(response => response.data ?? []));
  }

  getOffer(id: number): Observable<OfferDetail> {
    return this.http
      .get<ApiResponse<OfferDetail>>(`${this.base}/offers/${id}`)
      .pipe(map(response => response.data!));
  }

  createOffer(request: CreateOfferRequest): Observable<OfferDetail> {
    return this.http
      .post<ApiResponse<OfferDetail>>(`${this.base}/offers`, request)
      .pipe(map(response => response.data!));
  }

  updateOffer(id: number, request: UpdateOfferRequest): Observable<OfferDetail> {
    return this.http
      .put<ApiResponse<OfferDetail>>(`${this.base}/offers/${id}`, request)
      .pipe(map(response => response.data!));
  }

  listOffersForProduct(productId: number, storeId?: number): Observable<OfferSummary[]> {
    let params = new HttpParams();
    if (storeId !== undefined) {
      params = params.set('storeId', storeId);
    }
    return this.http
      .get<ApiResponse<OfferSummary[]>>(`${this.base}/offers/products/${productId}`, { params })
      .pipe(map(response => response.data ?? []));
  }

  listOffersForVariant(variantId: number, storeId?: number): Observable<OfferSummary[]> {
    let params = new HttpParams();
    if (storeId !== undefined) {
      params = params.set('storeId', storeId);
    }
    return this.http
      .get<ApiResponse<OfferSummary[]>>(`${this.base}/offers/variants/${variantId}`, { params })
      .pipe(map(response => response.data ?? []));
  }

  listStorefrontProducts(term?: string): Observable<ProductSummary[]> {
    let params = new HttpParams();
    if (term) {
      params = params.set('term', term);
    }
    return this.http
      .get<ApiResponse<ProductSummary[]>>(`${this.base}/storefront/products`, { params })
      .pipe(map(response => response.data ?? []));
  }

  getStorefrontProduct(id: number): Observable<StorefrontProductDetail> {
    return this.http
      .get<ApiResponse<StorefrontProductDetail>>(`${this.base}/storefront/products/${id}`)
      .pipe(map(response => response.data!));
  }

  getStorefrontProductBySlug(slug: string): Observable<StorefrontProductDetail> {
    return this.http
      .get<ApiResponse<StorefrontProductDetail>>(`${this.base}/storefront/products/by-slug/${encodeURIComponent(slug)}`)
      .pipe(map(response => response.data!));
  }

  getProductPrice(productId: number, currencyId?: number): Observable<ResolvedPrice> {
    let params = new HttpParams();
    if (currencyId !== undefined) {
      params = params.set('currencyId', currencyId);
    }
    return this.http
      .get<ApiResponse<ResolvedPrice>>(`${this.base}/pricing/products/${productId}`, { params })
      .pipe(map(response => response.data!));
  }

  getVariantPrice(variantId: number, currencyId?: number): Observable<ResolvedPrice> {
    let params = new HttpParams();
    if (currencyId !== undefined) {
      params = params.set('currencyId', currencyId);
    }
    return this.http
      .get<ApiResponse<ResolvedPrice>>(`${this.base}/pricing/variants/${variantId}`, { params })
      .pipe(map(response => response.data!));
  }

  getOfferPrice(offerId: number): Observable<ResolvedPrice> {
    return this.http
      .get<ApiResponse<ResolvedPrice>>(`${this.base}/pricing/offers/${offerId}`)
      .pipe(map(response => response.data!));
  }
}
