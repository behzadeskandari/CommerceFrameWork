using Commerce.Framework.Data.Db;
using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Domain.Entities;
using Commerce.SmartstoreImport.Domain.Enums;

namespace Commerce.SmartstoreImport.Infrastructure.Reconciliation;

internal sealed class ReconciliationContext
{
    public required SmartstoreParsedDataSet DataSet { get; init; }

    public required ImportRun ImportRun { get; init; }

    public required ReconciliationMappingIndex MappingIndex { get; init; }

    public required IReadOnlyList<ImportIssue> ImportIssues { get; init; }

    public required CommerceDbContext Db { get; init; }

    public required bool IncludeRecordLevelDetails { get; init; }

    public List<SmartstoreReconciliationDiscrepancy> Discrepancies { get; } = [];

    public ImportIssue? FindIssue(string entityType, int? sourceId) =>
        ImportIssues.FirstOrDefault(x =>
            string.Equals(x.EntityType, entityType, StringComparison.OrdinalIgnoreCase) &&
            x.SourceId == sourceId);

    public void AddDiscrepancy(
        string checkName,
        ReconciliationClassification classification,
        string entityType,
        int? sourceId,
        string? sourceKey,
        string explanation,
        string remediation)
    {
        if (!IncludeRecordLevelDetails &&
            classification is ReconciliationClassification.Match or ReconciliationClassification.NotApplicable)
        {
            return;
        }

        Discrepancies.Add(new SmartstoreReconciliationDiscrepancy(
            checkName,
            classification,
            entityType,
            sourceId,
            sourceKey,
            explanation,
            remediation));
    }
}
