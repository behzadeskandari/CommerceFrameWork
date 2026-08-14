namespace Commerce.Framework.Scheduling;

public static class BackgroundJobTypes
{
    public const string NotificationRetry = "notifications.retry";
    public const string NotificationDeliver = "notifications.deliver";
    public const string EmailSend = "email.send";
    public const string SmsSend = "sms.send";
    public const string SearchIndexProcess = "search.index.process";
    public const string ReportsGenerate = "reports.generate";
    public const string MaintenanceCleanup = "maintenance.cleanup";
    public const string ExpiredDownloads = "downloads.expired";
    public const string InventoryTasks = "inventory.tasks";
    public const string InventoryReservationExpire = "inventory.reservations.expire";
    public const string PromotionsTasks = "promotions.tasks";
    public const string PluginTasks = "plugins.tasks";
    public const string WebhookDeliveryProcess = "integration.webhooks.deliver";
    public const string BackupCreate = "backup.create";
    public const string BackupRetention = "backup.retention";
}
