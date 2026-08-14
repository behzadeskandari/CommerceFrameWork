export type ShippingRateType = 'Flat' | 'WeightBased' | 'OrderSubtotalBased' | 'QuantityBased';

export type PostalRuleType = 'Exact' | 'Prefix' | 'Range';

export const SHIPPING_PROVIDER_FLAT_RATE = 'Shipping.FlatRate';
export const SHIPPING_PROVIDER_PICKUP = 'Shipping.Pickup';

export interface ShipmentSummary {
  id: number;
  orderId: number;
  storeId: number;
  status: string;
  trackingNumber: string | null;
  carrierName: string | null;
  shippedAtUtc: string | null;
  createdAtUtc: string;
}

export interface CreateShipmentItemRequest {
  orderItemId: number;
  offerId: number;
  productId: number;
  quantity: number;
}

export interface CreateShipmentRequest {
  orderId: number;
  shippingMethodId?: number | null;
  providerSystemName?: string | null;
  notes?: string | null;
  items: CreateShipmentItemRequest[];
}

export interface ShippingSettings {
  enabled: boolean;
  defaultEstimatedDeliveryDays: number;
  allowFreeShipping: boolean;
  requireShippingAddress: boolean;
}

export interface ShippingZoneCountry {
  countryCode: string;
}

export interface ShippingZoneState {
  countryCode: string;
  stateProvince: string;
}

export interface ShippingZonePostalRule {
  countryCode: string;
  ruleType: PostalRuleType;
  postalFrom: string;
  postalTo?: string | null;
}

export interface ShippingMethodSummary {
  id: number;
  storeId: number;
  name: string;
  systemName: string;
  providerSystemName: string;
  isActive: boolean;
  displayOrder: number;
}

export interface ShippingMethodDetail extends ShippingMethodSummary {
  description: string | null;
  requiresAddress: boolean;
  supportsTracking: boolean;
  estimatedDeliveryDaysMin: number | null;
  estimatedDeliveryDaysMax: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateShippingMethodRequest {
  storeId: number;
  name: string;
  systemName: string;
  description?: string | null;
  providerSystemName: string;
  isActive: boolean;
  displayOrder: number;
  requiresAddress: boolean;
  supportsTracking: boolean;
  estimatedDeliveryDaysMin?: number | null;
  estimatedDeliveryDaysMax?: number | null;
}

export interface UpdateShippingMethodRequest {
  name: string;
  description?: string | null;
  isActive: boolean;
  displayOrder: number;
  requiresAddress: boolean;
  supportsTracking: boolean;
  estimatedDeliveryDaysMin?: number | null;
  estimatedDeliveryDaysMax?: number | null;
}

export interface ShippingZoneSummary {
  id: number;
  storeId: number;
  name: string;
  systemName: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface ShippingZoneDetail extends ShippingZoneSummary {
  countries: ShippingZoneCountry[];
  states: ShippingZoneState[];
  postalRules: ShippingZonePostalRule[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateShippingZoneRequest {
  storeId: number;
  name: string;
  systemName: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
  countries: ShippingZoneCountry[];
  states: ShippingZoneState[];
  postalRules: ShippingZonePostalRule[];
}

export interface UpdateShippingZoneRequest {
  name: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
  countries: ShippingZoneCountry[];
  states: ShippingZoneState[];
  postalRules: ShippingZonePostalRule[];
}

export interface ShippingRateSummary {
  id: number;
  storeId: number;
  shippingMethodId: number;
  shippingZoneId: number | null;
  currencyCode: string;
  rateType: ShippingRateType;
  basePrice: number;
  isActive: boolean;
}

export interface ShippingRateDetail extends ShippingRateSummary {
  pricePerWeightUnit: number | null;
  pricePerQuantityUnit: number | null;
  orderSubtotalPercentage: number | null;
  freeShippingThreshold: number | null;
  minOrderSubtotal: number | null;
  maxOrderSubtotal: number | null;
  minWeightGrams: number | null;
  maxWeightGrams: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateShippingRateRequest {
  storeId: number;
  shippingMethodId: number;
  shippingZoneId?: number | null;
  currencyCode: string;
  rateType: ShippingRateType;
  basePrice: number;
  pricePerWeightUnit?: number | null;
  freeShippingThreshold?: number | null;
  minOrderSubtotal?: number | null;
  maxOrderSubtotal?: number | null;
}

export interface UpdateShippingRateRequest {
  basePrice: number;
  pricePerWeightUnit?: number | null;
  freeShippingThreshold?: number | null;
  minOrderSubtotal?: number | null;
  maxOrderSubtotal?: number | null;
  isActive: boolean;
}
