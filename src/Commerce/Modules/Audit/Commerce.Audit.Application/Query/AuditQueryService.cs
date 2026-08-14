using Commerce.Audit.Application.Abstractions;
using Commerce.Audit.Application.Security;
using Commerce.Audit.Contracts;
using Commerce.Audit.Domain.Entities;
using Commerce.Framework.Core.Results;
using Microsoft.Extensions.Options;

namespace Commerce.Audit.Application.Query;

public sealed class AuditQueryService(IAuditRepository repository, IOptions<AuditRetentionOptions> retentionOptions)
    : IAuditQueryService
{
    public async Task<Result<PagedAuditEntriesResult>> ListAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var (items, totalCount) = await repository.ListAsync(
            new AuditListCriteria(
                page,
                pageSize,
                query.StoreId,
                query.Category,
                query.Action,
                query.ActorId,
                query.EntityType,
                query.EntityId,
                query.Success,
                query.FromUtc,
                query.ToUtc),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new PagedAuditEntriesResult(
            items.Select(Map).ToList(),
            page,
            pageSize,
            totalCount));
    }

    public async Task<Result<AuditChainVerificationResult>> VerifyChainAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await repository.ListForChainVerificationAsync(storeId, cancellationToken).ConfigureAwait(false);
        var expectedPrevious = AuditEntry.GenesisHash;

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            if (!string.Equals(entry.PreviousEntryHash, expectedPrevious, StringComparison.Ordinal))
            {
                return Result.Success(new AuditChainVerificationResult(
                    false,
                    index,
                    entry.Id,
                    $"Entry {entry.Id} has unexpected previous hash."));
            }

            var canonicalPayload = AuditSanitizer.BuildCanonicalPayload(entry.DetailsJson);
            var computedHash = AuditSanitizer.ComputeEntryHash(entry, canonicalPayload);
            if (!string.Equals(entry.EntryHash, computedHash, StringComparison.Ordinal))
            {
                return Result.Success(new AuditChainVerificationResult(
                    false,
                    index,
                    entry.Id,
                    $"Entry {entry.Id} failed hash verification."));
            }

            expectedPrevious = entry.EntryHash;
        }

        return Result.Success(new AuditChainVerificationResult(true, entries.Count, null, null));
    }

    public async Task<Result<AuditRetentionResult>> ApplyRetentionPolicyAsync(
        CancellationToken cancellationToken = default)
    {
        var retentionDays = Math.Max(1, retentionOptions.Value.RetentionDays);
        var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, cancellationToken).ConfigureAwait(false);
        return Result.Success(new AuditRetentionResult(deletedCount, cutoffUtc));
    }

    private static AuditEntryDto Map(AuditEntry entry) =>
        new(
            entry.Id,
            entry.StoreId,
            entry.OccurredAtUtc,
            entry.Category,
            entry.Action,
            entry.ActorType,
            entry.ActorId,
            entry.ActorDisplay,
            entry.EntityType,
            entry.EntityId,
            entry.IpAddress,
            entry.CorrelationId,
            entry.Success,
            entry.DetailsJson,
            entry.PreviousEntryHash,
            entry.EntryHash);
}

public sealed class AuditRetentionOptions
{
    public const string SectionName = "Audit:Retention";

    public int RetentionDays { get; set; } = 365;
}
