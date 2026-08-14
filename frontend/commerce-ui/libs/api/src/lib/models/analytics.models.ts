export type ReportGranularity = 'Day' | 'Week' | 'Month';

export type ReportType =
  | 'Revenue'
  | 'Orders'
  | 'Customers'
  | 'Products'
  | 'Inventory'
  | 'Payments'
  | 'Refunds'
  | 'Discounts'
  | 'Downloads'
  | 'Conversion';

export interface ReportFilterQuery {
  storeId?: number;
  fromUtc?: string;
  toUtc?: string;
  productId?: number;
  customerId?: number;
  granularity?: ReportGranularity;
  topProductsLimit?: number;
}

export interface TimeSeriesPoint {
  periodStartUtc: string;
  value: number;
  count: number;
}

export interface StatusBreakdown {
  status: string;
  count: number;
  amount?: number;
}

export interface TopProductRow {
  productId: number;
  productName: string;
  quantitySold: number;
  revenue: number;
  currencyCode: string;
}

export interface DashboardSummary {
  fromUtc: string;
  toUtc: string;
  storeId?: number;
  totalRevenue: number;
  orderCount: number;
  averageOrderValue: number;
  newCustomers: number;
  totalRefunded: number;
  lowStockItems: number;
  outOfStockItems: number;
  cartToOrderConversionRate: number;
  revenueTimeSeries: TimeSeriesPoint[];
  ordersByStatus: StatusBreakdown[];
  topProducts: TopProductRow[];
}

export interface RevenueReport {
  fromUtc: string;
  toUtc: string;
  grossRevenue: number;
  discountTotal: number;
  taxTotal: number;
  shippingTotal: number;
  netRevenue: number;
  paidOrderCount: number;
  timeSeries: TimeSeriesPoint[];
}

export interface ConversionReport {
  fromUtc: string;
  toUtc: string;
  cartsCreated: number;
  checkoutsStarted: number;
  ordersCompleted: number;
  cartToCheckoutRate: number;
  checkoutToOrderRate: number;
  cartToOrderRate: number;
  ordersTimeSeries: TimeSeriesPoint[];
}
