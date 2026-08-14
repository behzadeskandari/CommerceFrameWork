export interface WarehouseSummary {
  id: number;
  storeId: number;
  name: string;
  systemName: string;
  isDefault: boolean;
  isActive: boolean;
  displayOrder: number;
}

export interface StockLocationSummary {
  id: number;
  warehouseId: number;
  code: string;
  name: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface WarehouseDetail extends WarehouseSummary {
  createdAtUtc: string;
  updatedAtUtc: string;
  locations: StockLocationSummary[];
}

export interface CreateWarehouseRequest {
  name: string;
  systemName: string;
  isDefault: boolean;
  displayOrder?: number;
}

export interface UpdateWarehouseRequest {
  name: string;
  displayOrder: number;
}

export interface CreateStockLocationRequest {
  code: string;
  name: string;
  isDefault: boolean;
}

export interface TransferInventoryStockRequest {
  sourceInventoryItemId: number;
  destinationInventoryItemId: number;
  quantity: number;
  reason: string;
}

export interface ReceiveIncomingStockRequest {
  quantity: number;
  reason: string;
}

export interface SetLowStockThresholdRequest {
  threshold: number | null;
}
