export type NotificationChannel = 'Email' | 'Sms' | 'InApp';

export type NotificationEventType =
  | 'CustomerRegistered'
  | 'OrderCreated'
  | 'PaymentSucceeded'
  | 'PaymentFailed'
  | 'OrderCancelled'
  | 'ShipmentCreated'
  | 'RefundCreated'
  | 'DownloadAvailable';

export type NotificationDeliveryStatus = 'Pending' | 'Sent' | 'Failed' | 'Cancelled';

export interface NotificationTemplateSummary {
  id: number;
  systemName: string;
  eventType: NotificationEventType;
  channel: NotificationChannel;
  languageId: number | null;
  storeId: number | null;
  isEnabled: boolean;
}

export interface NotificationTemplateDetail extends NotificationTemplateSummary {
  subject: string;
  body: string;
  variablesJson: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface NotificationLogSummary {
  id: number;
  eventType: NotificationEventType;
  channel: NotificationChannel;
  storeId: number | null;
  customerId: number | null;
  recipient: string;
  subject: string;
  status: NotificationDeliveryStatus;
  attemptCount: number;
  createdAtUtc: string;
  sentAtUtc: string | null;
  lastError: string | null;
}

export interface CreateNotificationTemplateRequest {
  systemName: string;
  eventType: NotificationEventType;
  channel: NotificationChannel;
  subject: string;
  body: string;
  languageId: number | null;
  storeId: number | null;
  variablesJson: string | null;
  isEnabled: boolean;
}

export interface UpdateNotificationTemplateRequest {
  subject: string;
  body: string;
  languageId: number | null;
  storeId: number | null;
  variablesJson: string | null;
  isEnabled: boolean;
}
