export type PluginState =
  | 'Discovered'
  | 'Invalid'
  | 'Loaded'
  | 'Installed'
  | 'Enabled'
  | 'Disabled'
  | 'Failed';

export type PluginUninstallMode = 'KeepData' | 'RemoveData';

export interface PluginSummary {
  systemName: string;
  name: string;
  version: string;
  state: string;
  isInstalled: boolean;
  isEnabled: boolean;
  isSystemPlugin: boolean;
  author: string | null;
  description: string | null;
}

export interface PluginDependency {
  systemName: string;
  minimumVersion: string | null;
  maximumVersion: string | null;
}

export interface PluginDetail {
  systemName: string;
  name: string;
  version: string;
  state: string;
  isInstalled: boolean;
  isEnabled: boolean;
  isSystemPlugin: boolean;
  isRequired: boolean;
  author: string | null;
  description: string | null;
  website: string | null;
  assemblyName: string;
  pluginDirectory: string;
  dependencies: PluginDependency[];
  minimumCommerceVersion: string | null;
  maximumCommerceVersion: string | null;
  lastError: string | null;
  installedAt: string | null;
  updatedAt: string | null;
  requiresRestartForServiceChanges: boolean;
}

export interface PluginSettingEntry {
  key: string;
  value: string | null;
  description: string;
  valueType: string;
  isStoreScoped: boolean;
  isSecret: boolean;
  hasValue: boolean;
}

export interface PluginPermissionEntry {
  key: string;
  description: string;
}

export interface PluginStoreConfiguration {
  storeId: number;
  isEnabled: boolean;
  configurationJson: string | null;
}

export interface PluginMigrationStatus {
  name: string;
  version: string;
  description: string;
  isApplied: boolean;
}

export interface PluginUiMetadata {
  adminNavItems: PluginAdminNavItem[];
  contributions: PluginUiContribution[];
}

export interface PluginAdminNavItem {
  title: string;
  route: string;
  icon: string | null;
  displayOrder: number;
  permission: string | null;
}

export interface PluginUiContribution {
  target: string;
  title: string;
  permission: string | null;
  configurationComponent: string | null;
  displayOrder: number;
}
