using Commerce.Audit.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Audit.Domain.Entities;

public sealed class AuditEntry : Entity
{
    public const int CategoryMaxLength = 64;
    public const int ActionMaxLength = 128;
    public const int EntityTypeMaxLength = 128;
    public const int EntityIdMaxLength = 64;
    public const int ActorIdMaxLength = 128;
    public const int ActorDisplayMaxLength = 256;
    public const int IpAddressMaxLength = 45;
    public const int UserAgentMaxLength = 512;
    public const int CorrelationIdMaxLength = 64;
    public const int DetailsJsonMaxLength = 8000;
    public const int HashMaxLength = 128;
    public const string GenesisHash = "GENESIS";

    private AuditEntry()
    {
    }

    public int? StoreId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public AuditCategory Category { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public AuditActorType ActorType { get; private set; }

    public string? ActorId { get; private set; }

    public string? ActorDisplay { get; private set; }

    public string? EntityType { get; private set; }

    public string? EntityId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? CorrelationId { get; private set; }

    public bool Success { get; private set; }

    public string? DetailsJson { get; private set; }

    public string PreviousEntryHash { get; private set; } = GenesisHash;

    public string EntryHash { get; private set; } = string.Empty;

    public static AuditEntry Create(
        int? storeId,
        DateTime occurredAtUtc,
        AuditCategory category,
        string action,
        AuditActorType actorType,
        string? actorId,
        string? actorDisplay,
        string? entityType,
        string? entityId,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        bool success,
        string? detailsJson,
        string previousEntryHash,
        string entryHash) =>
        new()
        {
            StoreId = storeId,
            OccurredAtUtc = occurredAtUtc,
            Category = category,
            Action = Trim(action, ActionMaxLength),
            ActorType = actorType,
            ActorId = Trim(actorId, ActorIdMaxLength),
            ActorDisplay = Trim(actorDisplay, ActorDisplayMaxLength),
            EntityType = Trim(entityType, EntityTypeMaxLength),
            EntityId = Trim(entityId, EntityIdMaxLength),
            IpAddress = Trim(ipAddress, IpAddressMaxLength),
            UserAgent = Trim(userAgent, UserAgentMaxLength),
            CorrelationId = Trim(correlationId, CorrelationIdMaxLength),
            Success = success,
            DetailsJson = Trim(detailsJson, DetailsJsonMaxLength),
            PreviousEntryHash = Trim(previousEntryHash, HashMaxLength) ?? GenesisHash,
            EntryHash = Trim(entryHash, HashMaxLength) ?? string.Empty
        };

    private static string Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
