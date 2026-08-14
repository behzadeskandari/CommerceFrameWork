export type DiscountType = 'Percentage' | 'FixedAmount';

export type DiscountTargetType = 'Product' | 'Variant' | 'Offer' | 'Category' | 'Cart';

export type DiscountApplicationScope = 'Line' | 'Cart';

export type CustomerEligibility = 'All' | 'Authenticated' | 'Guest' | 'SpecificCustomer';

export type StackingMode = 'NonStackable' | 'Stackable';

export interface DiscountTarget {
  targetType: DiscountTargetType;
  targetId: number;
}

export interface DiscountSummary {
  id: number;
  name: string;
  systemName: string;
  discountType: DiscountType;
  value: number;
  currencyCode: string | null;
  priority: number;
  isActive: boolean;
  startsAtUtc: string | null;
  endsAtUtc: string | null;
  storeId: number | null;
  applicationScope: DiscountApplicationScope;
}

export interface DiscountDetail extends DiscountSummary {
  description: string | null;
  stackingMode: StackingMode;
  maximumDiscountAmount: number | null;
  minimumCartSubtotal: number | null;
  minimumQuantity: number | null;
  customerEligibility: CustomerEligibility;
  specificCustomerId: number | null;
  targets: DiscountTarget[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateDiscountRequest {
  name: string;
  systemName: string;
  description?: string | null;
  discountType: DiscountType;
  value: number;
  currencyCode?: string | null;
  priority: number;
  isActive: boolean;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  storeId?: number | null;
  stackingMode: StackingMode;
  maximumDiscountAmount?: number | null;
  minimumCartSubtotal?: number | null;
  minimumQuantity?: number | null;
  customerEligibility: CustomerEligibility;
  specificCustomerId?: number | null;
  applicationScope: DiscountApplicationScope;
  targets: DiscountTarget[];
}

export interface UpdateDiscountRequest {
  name: string;
  description?: string | null;
  discountType: DiscountType;
  value: number;
  currencyCode?: string | null;
  priority: number;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  storeId?: number | null;
  stackingMode: StackingMode;
  maximumDiscountAmount?: number | null;
  minimumCartSubtotal?: number | null;
  minimumQuantity?: number | null;
  customerEligibility: CustomerEligibility;
  specificCustomerId?: number | null;
  applicationScope: DiscountApplicationScope;
  targets: DiscountTarget[];
}

export interface CouponSummary {
  id: number;
  code: string;
  discountId: number;
  discountName: string;
  isActive: boolean;
  usageCount: number;
  globalUsageLimit: number | null;
  perCustomerUsageLimit: number | null;
  startsAtUtc: string | null;
  endsAtUtc: string | null;
  storeId: number | null;
}

export interface CouponDetail extends CouponSummary {
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateCouponRequest {
  code: string;
  discountId: number;
  isActive: boolean;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  storeId?: number | null;
  globalUsageLimit?: number | null;
  perCustomerUsageLimit?: number | null;
}

export interface UpdateCouponRequest {
  isActive: boolean;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  storeId?: number | null;
  globalUsageLimit?: number | null;
  perCustomerUsageLimit?: number | null;
}
