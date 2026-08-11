export type ProductType =
  | 'Simple'
  | 'Grouped'
  | 'Digital'
  | 'Downloadable'
  | 'Virtual'
  | 'Variant'
  | 'Bundle';

export type AttributeType = 'Text' | 'Option' | 'Boolean' | 'Number';

export interface ProductSummary {
  id: number;
  name: string;
  sku: string;
  productType: ProductType;
  published: boolean;
  isVisible: boolean;
  isAvailable: boolean;
  deleted: boolean;
  displayOrder: number;
  slug: string | null;
}

export interface ProductDetail extends ProductSummary {
  shortDescription: string | null;
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  categoryIds: number[];
  attributes: ProductAttributeValue[];
}

export interface ProductAttributeValue {
  attributeDefinitionId: number;
  attributeCode: string;
  attributeName: string;
  value: string;
}

export interface CreateProductRequest {
  name: string;
  sku: string;
  productType: ProductType;
  shortDescription?: string | null;
  description?: string | null;
  slug?: string | null;
  published?: boolean;
  isVisible?: boolean;
  isAvailable?: boolean;
  displayOrder?: number;
  categoryIds?: number[] | null;
}

export interface UpdateProductRequest {
  name: string;
  productType: ProductType;
  shortDescription?: string | null;
  description?: string | null;
  slug?: string | null;
  published?: boolean;
  isVisible?: boolean;
  isAvailable?: boolean;
  displayOrder?: number;
  categoryIds?: number[] | null;
}

export interface CategorySummary {
  id: number;
  name: string;
  parentCategoryId: number | null;
  published: boolean;
  displayOrder: number;
  slug: string | null;
}

export interface CategoryDetail extends CategorySummary {
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  childCategoryIds: number[];
  productIds: number[];
}

export interface CreateCategoryRequest {
  name: string;
  parentCategoryId?: number | null;
  description?: string | null;
  slug?: string | null;
  published?: boolean;
  displayOrder?: number;
}

export interface UpdateCategoryRequest {
  name: string;
  parentCategoryId?: number | null;
  description?: string | null;
  slug?: string | null;
  published?: boolean;
  displayOrder?: number;
}

export interface CategoryTreeNode extends CategorySummary {
  children: CategoryTreeNode[];
}

export interface AttributeOption {
  id: number;
  attributeDefinitionId: number;
  value: string;
  isActive: boolean;
  displayOrder: number;
}

export interface AttributeDefinition {
  id: number;
  name: string;
  code: string;
  attributeType: AttributeType;
  isActive: boolean;
  displayOrder: number;
  options: AttributeOption[];
}

export interface ProductAttributeAssignment {
  attributeDefinitionId: number;
  attributeCode: string;
  attributeName: string;
  attributeType: AttributeType;
  options: AttributeOption[];
}

export interface CreateAttributeDefinitionRequest {
  name: string;
  code: string;
  attributeType: AttributeType;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateAttributeDefinitionRequest {
  name: string;
  attributeType: AttributeType;
  displayOrder?: number;
  isActive?: boolean;
}

export interface CreateAttributeOptionRequest {
  value: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateAttributeOptionRequest {
  value: string;
  displayOrder?: number;
  isActive?: boolean;
}

export interface SetProductAttributeValueRequest {
  attributeDefinitionId: number;
  value: string;
}

export interface VariantSummary {
  id: number;
  productId: number;
  sku: string;
  name: string;
  isActive: boolean;
  isDefault: boolean;
  displayOrder: number;
  attributeCombinationKey: string;
}

export interface VariantAttribute {
  attributeOptionId: number;
  attributeDefinitionId: number;
  attributeCode: string;
  attributeName: string;
  optionValue: string;
}

export interface VariantDetail extends VariantSummary {
  attributes: VariantAttribute[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateVariantRequest {
  sku: string;
  name: string;
  attributeOptionIds: number[];
  isActive?: boolean;
  isDefault?: boolean;
  displayOrder?: number;
}

export interface UpdateVariantRequest {
  name: string;
  attributeOptionIds: number[];
  isActive?: boolean;
  isDefault?: boolean;
  displayOrder?: number;
}

export interface GenerateVariantsRequest {
  skuPrefix: string;
  skipExisting?: boolean;
}

export interface OfferSummary {
  id: number;
  productId: number;
  variantId: number | null;
  storeId: number;
  currencyId: number;
  currencyCode: string;
  price: number;
  compareAtPrice: number | null;
  isActive: boolean;
  validFromUtc: string | null;
  validToUtc: string | null;
}

export interface OfferDetail extends OfferSummary {
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateOfferRequest {
  productId: number;
  variantId?: number | null;
  storeId: number;
  currencyId: number;
  currencyCode: string;
  price: number;
  compareAtPrice?: number | null;
  isActive?: boolean;
  validFromUtc?: string | null;
  validToUtc?: string | null;
}

export interface UpdateOfferRequest {
  price: number;
  compareAtPrice?: number | null;
  isActive?: boolean;
  validFromUtc?: string | null;
  validToUtc?: string | null;
}

export interface StorefrontAttributeOption {
  id: number;
  value: string;
}

export interface StorefrontVariant {
  id: number;
  sku: string;
  name: string;
  isDefault: boolean;
  options: StorefrontAttributeOption[];
  image?: StorefrontMedia | null;
}

export interface ProductAttributeAssignmentSummary {
  attributeDefinitionId: number;
  code: string;
  name: string;
  options: StorefrontAttributeOption[];
}

export interface ResolvedPrice {
  offerId: number;
  productId?: number;
  variantId?: number | null;
  storeId?: number;
  currencyCode: string;
  unitPrice: number;
  compareAtPrice: number | null;
  resolvedAtUtc?: string;
  availability?: StorefrontAvailability | null;
}

export interface StorefrontAvailability {
  status: string;
  canPurchase: boolean;
  isBackorder: boolean;
}

export interface StorefrontProductDetail {
  id: number;
  name: string;
  shortDescription: string | null;
  description: string | null;
  sku: string;
  productType: ProductType;
  slug: string | null;
  categoryIds: number[];
  configurableAttributes: ProductAttributeAssignmentSummary[];
  variants: StorefrontVariant[];
  defaultVariantId: number | null;
  price: ResolvedPrice | null;
  primaryImage?: StorefrontMedia | null;
  gallery?: StorefrontMedia[] | null;
}

export interface StorefrontMedia {
  mediaAssetId: number;
  url: string;
  thumbnailUrl?: string | null;
  altText?: string | null;
  role: string;
}
