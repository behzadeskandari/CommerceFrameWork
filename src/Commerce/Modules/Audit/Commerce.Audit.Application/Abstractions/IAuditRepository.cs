using Commerce.Audit.Domain.Entities;

namespace Commerce.Audit.Application.Abstractions;

public sealed record AuditListCriteria(
    int Page,
    int PageSize,
    int? StoreId,
    Domain.Enums.AuditCategory? Category,
    string? Action,
    string? ActorId,
    string? EntityType,
    string? EntityId,
    bool? Success,
    DateTime? FromUtc,
    DateTime? ToUtc);

public interface IAuditRepository
{
    Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    Task<string> GetLatestEntryHashAsync(int? storeId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AuditEntry> Items, int TotalCount)> ListAsync(
        AuditListCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEntry>> ListForChainVerificationAsync(
        int? storeId,
        CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
