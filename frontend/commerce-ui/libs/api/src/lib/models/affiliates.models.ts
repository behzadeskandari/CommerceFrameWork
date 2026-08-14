export interface AffiliateSummary {
  id: number;
  customerId: number;
  storeId: number;
  referralCode: string;
  commissionRatePercent: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface AffiliateDetail extends AffiliateSummary {
  commissionBalance: number;
  currencyCode: string;
  updatedAtUtc: string;
}

export interface AffiliateCommissionTransaction {
  id: number;
  type: string;
  amountDelta: number;
  balanceAfter: number;
  currencyCode: string;
  reason?: string | null;
  createdAtUtc: string;
}

export interface AffiliateReferral {
  id: number;
  affiliateId: number;
  referredCustomerId: number;
  storeId: number;
  referredAtUtc: string;
}

export interface CreateAffiliateRequest {
  customerId: number;
  storeId: number;
  referralCode: string;
  commissionRatePercent: number;
  isActive: boolean;
}

export interface UpdateAffiliateRequest {
  commissionRatePercent: number;
  isActive: boolean;
}
