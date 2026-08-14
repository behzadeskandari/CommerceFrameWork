export type GiftCardTransactionType = 'Issue' | 'Redeem' | 'Refund' | 'Adjust' | 'Expire';

export interface GiftCardSummary {
  id: number;
  code: string;
  storeId: number;
  currencyCode: string;
  initialAmount: number;
  balance: number;
  isActive: boolean;
  expiresAtUtc?: string | null;
  createdAtUtc: string;
}

export interface GiftCardDetail extends GiftCardSummary {
  startsAtUtc?: string | null;
  recipientEmail?: string | null;
  purchasedByCustomerId?: number | null;
  recipientCustomerId?: number | null;
  updatedAtUtc: string;
}

export interface GiftCardTransaction {
  id: number;
  type: GiftCardTransactionType;
  amountDelta: number;
  balanceAfter: number;
  currencyCode: string;
  reason?: string | null;
  createdAtUtc: string;
}

export interface CreateGiftCardRequest {
  code: string;
  storeId: number;
  currencyCode: string;
  initialAmount: number;
  isActive: boolean;
  startsAtUtc?: string | null;
  expiresAtUtc?: string | null;
  recipientEmail?: string | null;
  purchasedByCustomerId?: number | null;
  recipientCustomerId?: number | null;
}

export interface UpdateGiftCardRequest {
  isActive: boolean;
  startsAtUtc?: string | null;
  expiresAtUtc?: string | null;
  recipientEmail?: string | null;
  recipientCustomerId?: number | null;
}
