export type TaxRateType = 'Percentage' | 'Fixed';

export type TaxPostalRuleType = 'Exact' | 'Prefix' | 'Range';

export interface TaxZoneCountry {
  countryCode: string;
}

export interface TaxZoneState {
  countryCode: string;
  stateProvince: string;
}

export interface TaxZonePostalRule {
  countryCode: string;
  ruleType: TaxPostalRuleType;
  postalFrom: string;
  postalTo?: string | null;
}

export interface TaxCategorySummary {
  id: number;
  storeId: number;
  name: string;
  systemName: string;
  isExempt: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface TaxCategoryDetail extends TaxCategorySummary {
  description: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateTaxCategoryRequest {
  storeId: number;
  name: string;
  systemName: string;
  description?: string | null;
  isExempt: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface UpdateTaxCategoryRequest {
  name: string;
  description?: string | null;
  isExempt: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface TaxZoneSummary {
  id: number;
  storeId: number;
  name: string;
  systemName: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface TaxZoneDetail extends TaxZoneSummary {
  countries: TaxZoneCountry[];
  states: TaxZoneState[];
  postalRules: TaxZonePostalRule[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateTaxZoneRequest {
  storeId: number;
  name: string;
  systemName: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
  countries: TaxZoneCountry[];
  states: TaxZoneState[];
  postalRules: TaxZonePostalRule[];
}

export interface UpdateTaxZoneRequest {
  name: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
  countries: TaxZoneCountry[];
  states: TaxZoneState[];
  postalRules: TaxZonePostalRule[];
}

export interface TaxRateSummary {
  id: number;
  storeId: number;
  taxCategoryId: number;
  taxZoneId: number | null;
  rateType: TaxRateType;
  percentage: number;
  taxShipping: boolean;
  priority: number;
  isActive: boolean;
}

export interface TaxRateDetail extends TaxRateSummary {
  fixedAmount: number | null;
  effectiveFromUtc: string | null;
  effectiveToUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateTaxRateRequest {
  storeId: number;
  taxCategoryId: number;
  taxZoneId?: number | null;
  rateType: TaxRateType;
  percentage: number;
  taxShipping: boolean;
  priority: number;
  effectiveFromUtc?: string | null;
  effectiveToUtc?: string | null;
}

export interface UpdateTaxRateRequest {
  percentage: number;
  taxShipping: boolean;
  priority: number;
  effectiveFromUtc?: string | null;
  effectiveToUtc?: string | null;
  isActive: boolean;
}
