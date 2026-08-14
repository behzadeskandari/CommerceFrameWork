using Commerce.Framework.Core.Entities;
using Commerce.Integration.Domain.Enums;

namespace Commerce.Integration.Domain.Entities;

public sealed class WebhookSubscription : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int UrlMaxLength = 2000;
    public const int SecretMaxLength = 256;
    public const int EventTypesMaxLength = 2000;

    private WebhookSubscription()
    {
    }

    public int? StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public string Secret { get; private set; } = string.Empty;

    public string EventTypesCsv { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<string> EventTypes =>
        string.IsNullOrWhiteSpace(EventTypesCsv)
            ? []
            : EventTypesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static WebhookSubscription Create(
        int? storeId,
        string name,
        string url,
        string secret,
        IEnumerable<string> eventTypes,
        bool isActive)
    {
        ValidateName(name);
        ValidateUrl(url);
        ValidateSecret(secret);

        var utcNow = DateTime.UtcNow;
        return new WebhookSubscription
        {
            StoreId = storeId,
            Name = name.Trim(),
            Url = url.Trim(),
            Secret = secret.Trim(),
            EventTypesCsv = string.Join(',', eventTypes.Select(x => x.Trim()).Where(x => x.Length > 0)),
            IsActive = isActive,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(string name, string url, IEnumerable<string> eventTypes, bool isActive)
    {
        EnsureNotDeleted();
        ValidateName(name);
        ValidateUrl(url);

        Name = name.Trim();
        Url = url.Trim();
        EventTypesCsv = string.Join(',', eventTypes.Select(x => x.Trim()).Where(x => x.Length > 0));
        IsActive = isActive;
        Touch();
    }

    public void RotateSecret(string secret)
    {
        EnsureNotDeleted();
        ValidateSecret(secret);
        Secret = secret.Trim();
        Touch();
    }

    public void SoftDelete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public bool SubscribesTo(string eventType) =>
        EventTypes.Any(x => string.Equals(x, eventType, StringComparison.OrdinalIgnoreCase));

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Webhook subscription has been deleted.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("A valid absolute URL is required.", nameof(url));
        }
    }

    private static void ValidateSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 16)
        {
            throw new ArgumentException("Webhook secret must be at least 16 characters.", nameof(secret));
        }
    }
}

public sealed class WebhookDelivery : Entity
{
    public const int PayloadMaxLength = 8000;
    public const int ErrorMaxLength = 2000;
    public const int IdempotencyKeyMaxLength = 128;

    private WebhookDelivery()
    {
    }

    public int WebhookSubscriptionId { get; private set; }

    public Guid IntegrationEventId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    public WebhookDeliveryStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? NextRetryAtUtc { get; private set; }

    public int? ResponseStatusCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static WebhookDelivery Create(
        int webhookSubscriptionId,
        Guid integrationEventId,
        string eventType,
        string payloadJson,
        string idempotencyKey)
    {
        if (webhookSubscriptionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(webhookSubscriptionId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        var utcNow = DateTime.UtcNow;
        return new WebhookDelivery
        {
            WebhookSubscriptionId = webhookSubscriptionId,
            IntegrationEventId = integrationEventId,
            EventType = eventType.Trim(),
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey.Trim(),
            Status = WebhookDeliveryStatus.Pending,
            AttemptCount = 0,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public bool CanRetry(DateTime utcNow, int maxAttempts) =>
        Status is WebhookDeliveryStatus.Pending or WebhookDeliveryStatus.Failed &&
        AttemptCount < maxAttempts &&
        (!NextRetryAtUtc.HasValue || NextRetryAtUtc.Value <= utcNow);

    public void MarkDelivering()
    {
        AttemptCount++;
        Status = WebhookDeliveryStatus.Delivering;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkSucceeded(int responseStatusCode)
    {
        Status = WebhookDeliveryStatus.Succeeded;
        ResponseStatusCode = responseStatusCode;
        ErrorMessage = null;
        NextRetryAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(int? responseStatusCode, string? errorMessage, DateTime? nextRetryAtUtc, int maxAttempts)
    {
        ResponseStatusCode = responseStatusCode;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        NextRetryAtUtc = nextRetryAtUtc;
        Status = AttemptCount >= maxAttempts ? WebhookDeliveryStatus.DeadLetter : WebhookDeliveryStatus.Failed;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class ApiClient : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int KeyPrefixMaxLength = 16;
    public const int KeyHashMaxLength = 128;
    public const int ScopesMaxLength = 1000;

    private ApiClient()
    {
    }

    public int? StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string KeyPrefix { get; private set; } = string.Empty;

    public string KeyHash { get; private set; } = string.Empty;

    public string ScopesCsv { get; private set; } = string.Empty;

    public ApiClientStatus Status { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<string> Scopes =>
        string.IsNullOrWhiteSpace(ScopesCsv)
            ? []
            : ScopesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static ApiClient Create(
        int? storeId,
        string name,
        string keyPrefix,
        string keyHash,
        IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(keyPrefix) || string.IsNullOrWhiteSpace(keyHash))
        {
            throw new ArgumentException("API key material is required.");
        }

        var utcNow = DateTime.UtcNow;
        return new ApiClient
        {
            StoreId = storeId,
            Name = name.Trim(),
            KeyPrefix = keyPrefix.Trim(),
            KeyHash = keyHash.Trim(),
            ScopesCsv = string.Join(',', scopes.Select(x => x.Trim()).Where(x => x.Length > 0)),
            Status = ApiClientStatus.Active,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(string name, IEnumerable<string> scopes)
    {
        EnsureActive();
        Name = name.Trim();
        ScopesCsv = string.Join(',', scopes.Select(x => x.Trim()).Where(x => x.Length > 0));
        Touch();
    }

    public void Revoke()
    {
        EnsureActive();
        Status = ApiClientStatus.Revoked;
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        Status = ApiClientStatus.Revoked;
        Touch();
    }

    public bool HasScope(string scope) =>
        Scopes.Any(x => string.Equals(x, scope, StringComparison.OrdinalIgnoreCase));

    public bool IsCurrentlyActive() => !IsDeleted && Status == ApiClientStatus.Active;

    private void EnsureActive()
    {
        if (IsDeleted || Status != ApiClientStatus.Active)
        {
            throw new InvalidOperationException("API client is not active.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

public sealed class ProcessedIntegrationEvent : Entity
{
    public const int EventTypeMaxLength = 128;
    public const int ConsumerKeyMaxLength = 128;

    private ProcessedIntegrationEvent()
    {
    }

    public Guid IntegrationEventId { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string ConsumerKey { get; private set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; private set; }

    public static ProcessedIntegrationEvent Record(Guid integrationEventId, string eventType, string consumerKey)
    {
        return new ProcessedIntegrationEvent
        {
            IntegrationEventId = integrationEventId,
            EventType = eventType.Trim(),
            ConsumerKey = consumerKey.Trim(),
            ProcessedAtUtc = DateTime.UtcNow
        };
    }
}
