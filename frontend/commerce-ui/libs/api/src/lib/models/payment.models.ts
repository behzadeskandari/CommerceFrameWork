export const PAYMENT_PROVIDER_MANUAL = 'Payment.Manual';

export type PaymentStatus =
  | 'Pending'
  | 'Initiated'
  | 'RedirectRequired'
  | 'Authorized'
  | 'Captured'
  | 'Failed'
  | 'Cancelled'
  | 'PartiallyRefunded'
  | 'Refunded';

export type PaymentTransactionStatus = 'Pending' | 'Succeeded' | 'Failed';

export type PaymentTransactionType =
  | 'Authorization'
  | 'Capture'
  | 'Sale'
  | 'Void'
  | 'Refund'
  | 'PartialRefund'
  | 'Verification';

export type RefundStatus = 'Pending' | 'Succeeded' | 'Failed' | 'Cancelled';

export type PaymentAttemptStatus = 'Pending' | 'Succeeded' | 'Failed';

export interface PaymentSummary {
  id: number;
  storeId: number;
  orderId: number;
  currency: string;
  amount: number;
  status: PaymentStatus;
  providerSystemName: string;
  createdAtUtc: string;
}

export interface PaymentDto {
  id: number;
  storeId: number;
  orderId: number;
  customerId: number | null;
  currency: string;
  amount: number;
  status: PaymentStatus;
  providerSystemName: string;
  providerPaymentId: string | null;
  refundedAmount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PaymentTransaction {
  id: number;
  transactionType: PaymentTransactionType;
  amount: number;
  currency: string;
  status: PaymentTransactionStatus;
  providerTransactionId: string | null;
  failureCode: string | null;
  failureMessage: string | null;
  createdAtUtc: string;
}

export interface PaymentAttempt {
  id: number;
  attemptNumber: number;
  status: PaymentAttemptStatus;
  failureMessage: string | null;
  createdAtUtc: string;
}

export interface RefundSummary {
  id: number;
  amount: number;
  currency: string;
  status: RefundStatus;
  reason: string | null;
  createdAtUtc: string;
}

export interface PaymentDetail {
  payment: PaymentDto;
  transactions: PaymentTransaction[];
  attempts: PaymentAttempt[];
  refunds: RefundSummary[];
}

export interface CreatePaymentResult {
  paymentId: number;
  status: PaymentStatus;
  redirectUrl: string | null;
  instructions: string | null;
}

export interface CreatePaymentRequest {
  orderId: number;
  paymentMethodId?: string | null;
  paymentMethodSystemName?: string | null;
  returnUrl?: string | null;
  cancelUrl?: string | null;
}

export interface PaymentListQuery {
  page?: number;
  pageSize?: number;
  storeId?: number | null;
  orderId?: number | null;
  status?: PaymentStatus | null;
}

export interface PagedPaymentSummaryResult {
  items: PaymentSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PaymentMethodSummary {
  id: number;
  storeId: number;
  name: string;
  systemName: string;
  providerSystemName: string;
  displayName: string;
  isActive: boolean;
  displayOrder: number;
  requiresRedirect: boolean;
  supportsGuest: boolean;
  supportsFreeOrders: boolean;
}

export interface PaymentMethodDetail extends PaymentMethodSummary {
  configurationJson: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreatePaymentMethodRequest {
  storeId: number;
  name: string;
  systemName: string;
  providerSystemName: string;
  displayName: string;
  isActive: boolean;
  displayOrder: number;
  requiresRedirect: boolean;
  supportsGuest: boolean;
  supportsFreeOrders: boolean;
  configurationJson?: string | null;
}

export interface UpdatePaymentMethodRequest {
  name: string;
  displayName: string;
  isActive: boolean;
  displayOrder: number;
  requiresRedirect: boolean;
  supportsGuest: boolean;
  supportsFreeOrders: boolean;
  configurationJson?: string | null;
}

export interface RefundPaymentRequest {
  reason?: string | null;
}

export interface PartialRefundPaymentRequest {
  amount: number;
  reason?: string | null;
}

export const PAYMENT_STATUSES: PaymentStatus[] = [
  'Pending',
  'Initiated',
  'RedirectRequired',
  'Authorized',
  'Captured',
  'Failed',
  'Cancelled',
  'PartiallyRefunded',
  'Refunded'
];
