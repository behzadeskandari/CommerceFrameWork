export interface ProductDownloadSettings {
  productId: number;
  isEnabled: boolean;
  maxDownloadCount: number | null;
  expirationDays: number | null;
}

export interface ProductDownloadFile {
  id: number;
  productId: number;
  mediaAssetId: number;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  displayName: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface DownloadHistoryEntry {
  id: number;
  entitlementId: number;
  productDownloadFileId: number;
  customerId: number | null;
  downloadedAtUtc: string;
  wasSuccessful: boolean;
  failureReason: string | null;
}

export interface CustomerDownloadFile {
  fileId: number;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  displayName: string | null;
}

export interface CustomerDownloadEntitlement {
  entitlementId: number;
  orderId: number;
  orderNumber: string;
  productId: number;
  productName: string;
  grantedAtUtc: string;
  expiresAtUtc: string | null;
  maxDownloadCount: number | null;
  downloadCount: number;
  remainingDownloads: number | null;
  files: CustomerDownloadFile[];
}
