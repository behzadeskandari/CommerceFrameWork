export interface CustomerPreference {
  id: number;
  customerId: number;
  storeId: number | null;
  preferenceKey: string;
  preferenceValue: string;
  updatedAtUtc: string;
}

export interface LoyaltyAccount {
  id: number;
  customerId: number;
  storeId: number;
  pointsBalance: number;
  updatedAtUtc: string;
}

export interface LoyaltyTransaction {
  id: number;
  type: string;
  pointsDelta: number;
  balanceAfter: number;
  reason: string | null;
  expiresAtUtc: string | null;
  isExpired: boolean;
  createdAtUtc: string;
}

export interface LoyaltyReward {
  id: number;
  storeId: number;
  name: string;
  description: string | null;
  pointsCost: number;
  isActive: boolean;
}

export interface StoreCreditAccount {
  id: number;
  customerId: number;
  storeId: number;
  currencyCode: string;
  balance: number;
  updatedAtUtc: string;
}

export interface StoreCreditTransaction {
  id: number;
  type: string;
  amountDelta: number;
  balanceAfter: number;
  currencyCode: string;
  reason: string | null;
  expiresAtUtc: string | null;
  isExpired: boolean;
  createdAtUtc: string;
}

export interface CustomerActivity {
  id: number;
  storeId: number | null;
  activityType: string;
  summary: string;
  detailsJson: string | null;
  createdAtUtc: string;
}

export interface CustomerAccountOverview {
  preferences: CustomerPreference[];
  loyalty: LoyaltyAccount | null;
  storeCredit: StoreCreditAccount | null;
  recentActivity: CustomerActivity[];
}

export interface CustomerSegmentSummary {
  id: number;
  storeId: number;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CustomerPurchaseHistoryItem {
  orderId: number;
  orderNumber: string;
  grandTotal: number;
  currencyCode: string;
  status: string;
  createdAtUtc: string;
}

export interface UpsertCustomerPreferenceRequest {
  preferenceKey: string;
  preferenceValue: string;
  storeId?: number | null;
}

export interface RedeemLoyaltyRewardRequest {
  rewardId: number;
}

export interface AssignCustomerGroupRequest {
  customerGroupId: number | null;
}

export interface CreateCustomerSegmentRequest {
  storeId: number;
  name: string;
  description?: string | null;
  rules: CreateCustomerSegmentRuleRequest[];
}

export interface CreateCustomerSegmentRuleRequest {
  ruleType: 'CustomerGroup' | 'MinOrderCount' | 'MinLifetimeSpend';
  customerGroupId?: number | null;
  minOrderCount?: number | null;
  minLifetimeSpend?: number | null;
}

export interface CreateLoyaltyRewardRequest {
  storeId: number;
  name: string;
  pointsCost: number;
  description?: string | null;
}

export interface CreditStoreCreditRequest {
  amount: number;
  reason?: string | null;
  expiresAtUtc?: string | null;
}
