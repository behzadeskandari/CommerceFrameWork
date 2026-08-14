using System.Text.Json;
using Commerce.Audit.Application.Abstractions;
using Commerce.Audit.Application.Security;
using Commerce.Audit.Domain.Entities;
using Commerce.Framework.Contracts.Audit;
using FrameworkAuditCategory = Commerce.Framework.Contracts.Audit.AuditCategory;
using FrameworkAuditActorType = Commerce.Framework.Contracts.Audit.AuditActorType;
using DomainAuditCategory = Commerce.Audit.Domain.Enums.AuditCategory;
using DomainAuditActorType = Commerce.Audit.Domain.Enums.AuditActorType;

namespace Commerce.Audit.Application.Writing;

public sealed class AuditWriter(IAuditRepository repository) : IAuditPublisher
{
    public async Task PublishAsync(AuditPublishRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sanitizedDetails = AuditSanitizer.SanitizeDetails(request.Details);
        var detailsJson = sanitizedDetails.Count == 0
            ? null
            : JsonSerializer.Serialize(sanitizedDetails);

        var previousHash = await repository
            .GetLatestEntryHashAsync(request.StoreId, cancellationToken)
            .ConfigureAwait(false);

        var occurredAtUtc = DateTime.UtcNow;
        var category = MapCategory(request.Category);
        var actorType = MapActorType(request.ActorType);
        var entryHash = AuditSanitizer.ComputeEntryHash(
            previousHash,
            occurredAtUtc,
            category,
            request.Action,
            actorType,
            request.ActorId,
            request.EntityType,
            request.EntityId,
            request.Success,
            canonicalPayload: AuditSanitizer.BuildCanonicalPayload(detailsJson));

        var entry = AuditEntry.Create(
            request.StoreId,
            occurredAtUtc,
            category,
            request.Action,
            actorType,
            request.ActorId,
            request.ActorDisplay,
            request.EntityType,
            request.EntityId,
            request.IpAddress,
            request.UserAgent,
            request.CorrelationId,
            request.Success,
            detailsJson,
            previousHash,
            entryHash);

        await repository.AppendAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private static DomainAuditCategory MapCategory(FrameworkAuditCategory category) =>
        category switch
        {
            FrameworkAuditCategory.Security => DomainAuditCategory.Security,
            FrameworkAuditCategory.Admin => DomainAuditCategory.Admin,
            FrameworkAuditCategory.Order => DomainAuditCategory.Order,
            FrameworkAuditCategory.Payment => DomainAuditCategory.Payment,
            FrameworkAuditCategory.Customer => DomainAuditCategory.Customer,
            FrameworkAuditCategory.Settings => DomainAuditCategory.Settings,
            FrameworkAuditCategory.Plugin => DomainAuditCategory.Plugin,
            FrameworkAuditCategory.Authorization => DomainAuditCategory.Authorization,
            _ => DomainAuditCategory.Admin
        };

    private static DomainAuditActorType MapActorType(FrameworkAuditActorType actorType) =>
        actorType switch
        {
            FrameworkAuditActorType.Anonymous => DomainAuditActorType.Anonymous,
            FrameworkAuditActorType.Administrator => DomainAuditActorType.Administrator,
            FrameworkAuditActorType.Customer => DomainAuditActorType.Customer,
            FrameworkAuditActorType.System => DomainAuditActorType.System,
            FrameworkAuditActorType.ApiClient => DomainAuditActorType.ApiClient,
            _ => DomainAuditActorType.System
        };
}
