namespace Commerce.Framework.Contracts.Audit;

public enum AuditCategory
{
    Security = 0,
    Admin = 1,
    Order = 2,
    Payment = 3,
    Customer = 4,
    Settings = 5,
    Plugin = 6,
    Authorization = 7
}

public enum AuditActorType
{
    Anonymous = 0,
    Administrator = 1,
    Customer = 2,
    System = 3,
    ApiClient = 4
}

public sealed record AuditPublishRequest(
    AuditCategory Category,
    string Action,
    bool Success,
    string? EntityType = null,
    string? EntityId = null,
    int? StoreId = null,
    AuditActorType ActorType = AuditActorType.System,
    string? ActorId = null,
    string? ActorDisplay = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CorrelationId = null,
    IReadOnlyDictionary<string, string?>? Details = null);

public interface IAuditPublisher
{
    Task PublishAsync(AuditPublishRequest request, CancellationToken cancellationToken = default);
}

public static class AuditActions
{
    public const string LoginSucceeded = "security.login.succeeded";
    public const string LoginFailed = "security.login.failed";
    public const string Logout = "security.logout";
    public const string AccessDenied = "authorization.access.denied";
    public const string AdminRequest = "admin.request";
    public const string OrderCancelled = "order.cancelled";
    public const string PaymentCaptured = "payment.captured";
    public const string PaymentVoided = "payment.voided";
    public const string PaymentRefunded = "payment.refunded";
    public const string CustomerUpdated = "customer.updated";
    public const string SettingChanged = "settings.changed";
    public const string PluginInstalled = "plugin.installed";
    public const string PluginEnabled = "plugin.enabled";
    public const string PluginDisabled = "plugin.disabled";
    public const string PluginUninstalled = "plugin.uninstalled";
    public const string PluginSettingsChanged = "plugin.settings.changed";
}
