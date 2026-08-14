export type ThemeLayoutType =
  | 'Homepage'
  | 'Product'
  | 'Category'
  | 'Search'
  | 'Cart'
  | 'Checkout'
  | 'Account'
  | 'CmsPage';

export interface ThemeSettingDefinition {
  key: string;
  label: string;
  type: string;
  defaultValue: string;
  description?: string | null;
}

export interface ThemeLayoutDefinition {
  layoutType: ThemeLayoutType | string;
  zones: string[];
  showSidebar: boolean;
}

export interface ThemeSummary {
  systemName: string;
  name: string;
  version: string;
  author: string;
  description: string;
  isRegistered: boolean;
}

export interface ThemeDetail extends ThemeSummary {
  cssAssets: string[];
  fontAssets: string[];
  settings: ThemeSettingDefinition[];
  layouts: ThemeLayoutDefinition[];
}

export interface StoreThemeAssignment {
  storeId: number;
  themeSystemName: string;
  settings: Record<string, string>;
  layoutOverridesJson: string;
  updatedAtUtc: string;
}

export interface ThemeRuntime {
  themeSystemName: string;
  themeName: string;
  direction: 'ltr' | 'rtl';
  cssVariables: Record<string, string>;
  cssAssets: string[];
  layouts: ThemeLayoutDefinition[];
}

export interface UpdateStoreThemeAssignmentRequest {
  themeSystemName: string;
  settings?: Record<string, string> | null;
  layoutOverridesJson?: string | null;
}
