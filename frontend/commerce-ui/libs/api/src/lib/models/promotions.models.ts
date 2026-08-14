export type PromotionConditionType =
  | 'MinimumCartSubtotal'
  | 'MinimumQuantity'
  | 'CustomerGroup'
  | 'ProductInCart'
  | 'CategoryInCart'
  | 'ProductRestriction'
  | 'CategoryRestriction'
  | 'StoreRestriction'
  | 'UsageLimitRemaining'
  | 'PerCustomerUsageRemaining';

export type PromotionActionType =
  | 'PercentageDiscount'
  | 'FixedAmountDiscount'
  | 'BuyXGetY'
  | 'ApplyLinkedDiscount';

export type PromotionCombinationRule = 'Exclusive' | 'Stackable' | 'SameGroupExclusive';
export type PromotionTargetScope = 'Line' | 'Cart';

export interface PromotionConditionDto {
  id: number;
  conditionType: PromotionConditionType;
  parametersJson: string;
}

export interface PromotionActionDto {
  id: number;
  actionType: PromotionActionType;
  targetScope: PromotionTargetScope;
  parametersJson: string;
}

export interface PromotionSummary {
  id: number;
  name: string;
  systemName: string;
  isActive: boolean;
  startsAtUtc: string | null;
  endsAtUtc: string | null;
  storeId: number | null;
  priority: number;
  combinationRule: PromotionCombinationRule;
  usageCount: number;
  globalUsageLimit: number | null;
}

export interface PromotionDetail extends PromotionSummary {
  description: string | null;
  combinationGroup: string | null;
  perCustomerUsageLimit: number | null;
  requiresCouponCode: boolean;
  couponCode: string | null;
  conditions: PromotionConditionDto[];
  actions: PromotionActionDto[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface PromotionConditionRequest {
  conditionType: PromotionConditionType;
  parametersJson: string;
}

export interface PromotionActionRequest {
  actionType: PromotionActionType;
  targetScope: PromotionTargetScope;
  parametersJson: string;
}

export interface CreatePromotionRequest {
  name: string;
  systemName: string;
  description?: string | null;
  isActive: boolean;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  storeId?: number | null;
  priority: number;
  combinationRule: PromotionCombinationRule;
  combinationGroup?: string | null;
  globalUsageLimit?: number | null;
  perCustomerUsageLimit?: number | null;
  requiresCouponCode: boolean;
  couponCode?: string | null;
  conditions: PromotionConditionRequest[];
  actions: PromotionActionRequest[];
}

export type UpdatePromotionRequest = Omit<CreatePromotionRequest, 'systemName'>;
