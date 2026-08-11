export interface StoreSummary {
  id: number;
  systemName: string;
  name: string;
  url: string;
  isActive: boolean;
  displayOrder: number;
  defaultLanguageId: number;
  defaultCurrencyId: number;
  createdAtUtc: string;
}

export interface StoreDomain {
  id: number;
  storeId: number;
  host: string;
  scheme: string;
  port: number | null;
  isPrimary: boolean;
  isSslRequired: boolean;
}

export interface StoreDetail extends StoreSummary {
  updatedAtUtc: string;
  domains: StoreDomain[];
}

export interface StoreContext {
  storeId: number | null;
  storeSystemName: string | null;
  storeName: string | null;
  languageId: number | null;
  languageCode: string | null;
  cultureCode: string | null;
  isRtl: boolean;
  currencyId: number | null;
  currencyCode: string | null;
}

export interface LanguageSummary {
  id: number;
  name: string;
  languageCode: string;
  cultureCode: string;
  nativeName: string;
  isActive: boolean;
  isRtl: boolean;
  displayOrder: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CurrencySummary {
  id: number;
  code: string;
  name: string;
  symbol: string;
  displayName: string;
  decimalPlaces: number;
  rate: number;
  isActive: boolean;
  displayOrder: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface SettingEntry {
  key: string;
  value: string;
  valueType: string;
  description: string;
  storeId: number;
  moduleSystemName: string;
}

export interface CreateStoreRequest {
  systemName: string;
  name: string;
  url: string;
  defaultLanguageId: number;
  defaultCurrencyId: number;
  displayOrder?: number;
  isActive?: boolean;
  domains?: Array<{
    host: string;
    scheme: string;
    port: number | null;
    isPrimary: boolean;
    isSslRequired: boolean;
  }>;
}

export interface UpdateStoreRequest {
  name: string;
  url: string;
  defaultLanguageId: number;
  defaultCurrencyId: number;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateLanguageRequest {
  name: string;
  languageCode: string;
  cultureCode: string;
  nativeName?: string;
  isRtl: boolean;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateLanguageRequest {
  name: string;
  cultureCode: string;
  nativeName?: string;
  isRtl: boolean;
  displayOrder: number;
  isActive: boolean;
}

export interface CreateCurrencyRequest {
  code: string;
  name: string;
  symbol?: string;
  displayName?: string;
  rate: number;
  decimalPlaces?: number;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateCurrencyRequest {
  name: string;
  symbol?: string;
  displayName?: string;
  rate: number;
  decimalPlaces: number;
  displayOrder: number;
  isActive: boolean;
}

export interface UpdateSettingsRequest {
  settings: Array<{ key: string; value: string }>;
  storeId?: number;
}
