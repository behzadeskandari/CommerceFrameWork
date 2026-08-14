using Commerce.Audit.Domain.Enums;
using Commerce.Framework.Core.Results;

namespace Commerce.Audit.Contracts;
public sealed record AuditEntryDto(
    long Id,
    int? StoreId,
    DateTime OccurredAtUtc,
    AuditCategory Category,
    string Action,
    AuditActorType ActorType,
    string? ActorId,
    string? ActorDisplay,
    string? EntityType,
    string? EntityId,
    string? IpAddress,
    string? CorrelationId,
    bool Success,
    string? DetailsJson,
    string PreviousEntryHash,
    string EntryHash);

public sealed record AuditQuery(
    int Page = 1,
    int PageSize = 50,
    int? StoreId = null,
    AuditCategory? Category = null,
    string? Action = null,
    string? ActorId = null,
    string? EntityType = null,
    string? EntityId = null,
    bool? Success = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);

public sealed record PagedAuditEntriesResult(
    IReadOnlyList<AuditEntryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AuditChainVerificationResult(
    bool IsValid,
    int VerifiedCount,
    long? FirstInvalidEntryId,
    string? Message);

public sealed record AuditRetentionResult(int DeletedCount, DateTime CutoffUtc);

public interface IAuditQueryService
{
    Task<Result<PagedAuditEntriesResult>> ListAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<AuditChainVerificationResult>> VerifyChainAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default);

    Task<Result<AuditRetentionResult>> ApplyRetentionPolicyAsync(
        CancellationToken cancellationToken = default);
}
