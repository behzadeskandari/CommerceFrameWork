export type CheckoutStatus =
  | 'Active'
  | 'RequiresReview'
  | 'ReadyForOrder'
  | 'Expired'
  | 'Completed'
  | 'Cancelled';

export interface CheckoutAddress {
  sourceCustomerAddressId?: number | null;
  firstName: string;
  lastName: string;
  country: string;
  stateProvince?: string | null;
  city: string;
  address1: string;
  address2?: string | null;
  postalCode: string;
  phoneNumber?: string | null;
}

export interface CheckoutItemImage {
  url: string;
  thumbnailUrl?: string | null;
  altText?: string | null;
}

export interface CheckoutItem {
  cartItemId: number;
  offerId: number;
  productId: number;
  variantId?: number | null;
  productName: string;
  variantName?: string | null;
  sku: string;
  quantity: number;
  unitPrice: number;
  lineSubtotal: number;
  currency: string;
  priceChanged: boolean;
  primaryImage?: CheckoutItemImage | null;
}

export interface CheckoutTotals {
  subtotal: number;
  discountTotal: number;
  shippingTotal: number;
  taxTotal: number;
  grandTotal: number;
  currency: string;
}

export interface ShippingOption {
  id: string;
  name: string;
  providerSystemName: string;
  price: number;
  currency: string;
  estimatedDelivery?: string | null;
}

export interface PaymentMethod {
  id: string;
  name: string;
  systemName: string;
  displayName: string;
  requiresRedirect: boolean;
  supportsGuest: boolean;
  supportsCurrency: boolean;
}

export interface CheckoutCustomer {
  customerId?: number | null;
  email?: string | null;
  isGuest: boolean;
}

export interface CheckoutSession {
  id: number;
  cartId: number;
  storeId: number;
  status: CheckoutStatus;
  currency: string;
  currencyId: number;
  customer: CheckoutCustomer;
  billingAddress?: CheckoutAddress | null;
  shippingAddress?: CheckoutAddress | null;
  useShippingAsBilling: boolean;
  requiresShipping: boolean;
  priceChangeDetected: boolean;
  items: CheckoutItem[];
  totals: CheckoutTotals;
  shippingOptions: ShippingOption[];
  paymentMethods: PaymentMethod[];
  selectedShippingMethodId?: string | null;
  selectedPaymentMethodId?: string | null;
  validationErrors: string[];
  warnings: string[];
  expiresAtUtc: string;
  cartUpdatedAtUtc: string;
}

export interface CheckoutAddressRequest {
  firstName: string;
  lastName: string;
  country: string;
  city: string;
  address1: string;
  postalCode: string;
  stateProvince?: string | null;
  address2?: string | null;
  phoneNumber?: string | null;
}

export interface SetBillingAddressRequest {
  address?: CheckoutAddressRequest | null;
  customerAddressId?: number | null;
  useShippingAsBilling?: boolean;
}

export interface SetShippingAddressRequest {
  address?: CheckoutAddressRequest | null;
  customerAddressId?: number | null;
}

export interface CheckoutValidationResult {
  checkout: CheckoutSession;
  isValid: boolean;
  isReadyForOrder: boolean;
  errors: string[];
  warnings: string[];
}

export type CheckoutStep = 'contact' | 'billing' | 'shipping' | 'shippingMethod' | 'payment' | 'review';
