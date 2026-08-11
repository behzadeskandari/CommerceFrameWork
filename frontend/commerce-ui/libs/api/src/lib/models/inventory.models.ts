export type InventoryAvailabilityStatus =
  | 'NotTracked'
  | 'InStock'
  | 'OutOfStock'
  | 'Backorder';

export type InventoryMovementType =
  | 'InitialStock'
  | 'PurchaseReceipt'
  | 'ManualAdjustment'
  | 'Return'
  | 'Correction'
  | 'Damage'
  | 'Loss'
  | 'Sale';

export type InventoryReservationStatus =
  | 'Active'
  | 'Released'
  | 'Converted'
  | 'Expired'
  | 'Cancelled';

export interface InventoryItemSummary {
  id: number;
  storeId: number;
  offerId: number;
  productId: number;
  variantId: number | null;
  trackInventory: boolean;
  allowBackorder: boolean;
  onHand: number;
  reserved: number;
  available: number;
  availabilityStatus: InventoryAvailabilityStatus;
  updatedAtUtc: string;
}

export interface InventoryItemDetail extends InventoryItemSummary {
  warehouseId: number | null;
  createdAtUtc: string;
}

export interface InventoryMovement {
  id: number;
  inventoryItemId: number;
  quantityDelta: number;
  movementType: InventoryMovementType;
  reason: string;
  referenceType: string;
  referenceId: number | null;
  createdBy: string | null;
  createdAtUtc: string;
}

export interface InventoryReservation {
  id: number;
  inventoryItemId: number;
  quantity: number;
  referenceType: string;
  referenceId: number;
  status: InventoryReservationStatus;
  expiresAtUtc: string;
  releaseReason: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateInventoryItemRequest {
  offerId: number;
  trackInventory: boolean;
  allowBackorder: boolean;
  initialOnHand?: number;
  warehouseId?: number | null;
}

export interface AdjustInventoryStockRequest {
  quantityDelta: number;
  movementType: InventoryMovementType;
  reason: string;
}

export interface InventoryListQuery {
  page?: number;
  pageSize?: number;
  storeId?: number;
  offerId?: number;
  productId?: number;
  availabilityStatus?: InventoryAvailabilityStatus;
}

export interface PagedInventorySummaryResult {
  items: InventoryItemSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface StorefrontAvailability {
  status: string;
  canPurchase: boolean;
  isBackorder: boolean;
}
