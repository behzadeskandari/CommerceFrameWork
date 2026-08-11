export type OrderStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Processing'
  | 'Completed'
  | 'Cancelled';

export type PaymentStatus =
  | 'Pending'
  | 'Authorized'
  | 'Paid'
  | 'Failed'
  | 'Refunded'
  | 'PartiallyRefunded';

export type FulfillmentStatus =
  | 'Unfulfilled'
  | 'PartiallyFulfilled'
  | 'Fulfilled'
  | 'Cancelled';

export type OrderStatusHistoryType = 'Order' | 'Payment' | 'Fulfillment';

export interface OrderAddress {
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

export interface OrderItem {
  id: number;
  offerId: number;
  productId: number;
  variantId?: number | null;
  productName: string;
  variantName?: string | null;
  sku: string;
  quantity: number;
  unitPrice: number;
  lineSubtotal: number;
  discountTotal: number;
  taxTotal: number;
  lineTotal: number;
  currencyCode: string;
  primaryImageUrl?: string | null;
  primaryImageThumbnailUrl?: string | null;
}

export interface OrderTotals {
  subtotal: number;
  discountTotal: number;
  shippingTotal: number;
  taxTotal: number;
  grandTotal: number;
  currencyCode: string;
}

export interface OrderCustomer {
  customerId?: number | null;
  email?: string | null;
  displayName?: string | null;
  isGuest: boolean;
}

export interface OrderStatusHistory {
  id: number;
  historyType: OrderStatusHistoryType;
  fromStatus?: string | null;
  toStatus: string;
  reason: string;
  actor?: string | null;
  createdAtUtc: string;
}

export interface OrderSummary {
  id: number;
  orderNumber: string;
  storeId: number;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  fulfillmentStatus: FulfillmentStatus;
  grandTotal: number;
  currencyCode: string;
  customerEmail?: string | null;
  customerDisplayName?: string | null;
  customerId?: number | null;
  createdAtUtc: string;
}

export interface OrderDetail {
  id: number;
  orderNumber: string;
  storeId: number;
  checkoutId: number;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  fulfillmentStatus: FulfillmentStatus;
  customer: OrderCustomer;
  totals: OrderTotals;
  requiresShipping: boolean;
  billingAddress?: OrderAddress | null;
  shippingAddress?: OrderAddress | null;
  selectedShippingMethodId?: string | null;
  selectedShippingProviderSystemName?: string | null;
  selectedPaymentMethodId?: string | null;
  selectedPaymentMethodSystemName?: string | null;
  items: OrderItem[];
  statusHistory: OrderStatusHistory[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateOrderRequest {
  checkoutId: number;
}

export interface CreateOrderResult {
  id: number;
  orderNumber: string;
  guestAccessToken?: string | null;
}

export interface CancelOrderRequest {
  reason?: string | null;
}

export interface OrderListQuery {
  page?: number;
  pageSize?: number;
  orderNumber?: string | null;
  email?: string | null;
  customerId?: number | null;
  storeId?: number | null;
  status?: OrderStatus | null;
  createdFromUtc?: string | null;
  createdToUtc?: string | null;
}

export interface PagedOrderSummaryResult {
  items: OrderSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export const ORDER_STATUSES: OrderStatus[] = [
  'Pending',
  'Confirmed',
  'Processing',
  'Completed',
  'Cancelled'
];
