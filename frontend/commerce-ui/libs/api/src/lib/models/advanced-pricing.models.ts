export interface CustomerGroup {
  id: number;
  storeId: number;
  name: string;
  code: string;
  isActive: boolean;
  displayOrder: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CustomerGroupPrice {
  id: number;
  customerGroupId: number;
  storeId: number;
  productId: number;
  variantId?: number | null;
  currencyId: number;
  currencyCode: string;
  price: number;
  isActive: boolean;
}

export interface OfferTierPrice {
  id: number;
  offerId: number;
  minQuantity: number;
  price: number;
  isActive: boolean;
}

export interface TaxSettings {
  enabled: boolean;
  pricesIncludeTax: boolean;
  defaultCategoryId?: number | null;
  shippingTaxableByDefault: boolean;
}

export interface PricePreviewResult {
  baseUnitPrice: number;
  adjustedUnitPrice: number;
  compareAtPrice?: number | null;
  finalUnitPrice?: number | null;
  discountAmount?: number | null;
  currencyCode: string;
  tierPriceApplied: boolean;
  customerGroupPriceApplied: boolean;
  currencyConverted: boolean;
}
