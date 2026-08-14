export interface CartItemImage {
  url: string;
  thumbnailUrl?: string | null;
  altText?: string | null;
}

export interface CartItem {
  id: number;
  offerId: number;
  productId: number;
  variantId: number | null;
  productName: string;
  variantName: string | null;
  sku: string;
  quantity: number;
  unitPrice: number;
  lineSubtotal: number;
  currency: string;
  isValid: boolean;
  validationMessages: string[];
  primaryImage?: CartItemImage | null;
}

export interface CartTotals {
  subtotal: number;
  discountTotal: number;
  shippingTotal: number;
  taxTotal: number;
  grandTotal: number;
  currency: string;
}

export interface Cart {
  id: number;
  storeId: number;
  currency: string;
  currencyId: number;
  items: CartItem[];
  totals: CartTotals;
  itemCount: number;
  appliedCouponCode?: string | null;
}

export interface ApplyCartCouponRequest {
  code: string;
}

export interface AddCartItemRequest {
  offerId: number;
  quantity: number;
}

export interface UpdateCartItemQuantityRequest {
  quantity: number;
}

export interface CartMergeResult {
  cart: Cart;
  mergedItemCount: number;
  conflicts: CartMergeConflict[];
}

export interface CartMergeConflict {
  offerId: number;
  requestedQuantity: number;
  appliedQuantity: number;
  reason: string;
}
