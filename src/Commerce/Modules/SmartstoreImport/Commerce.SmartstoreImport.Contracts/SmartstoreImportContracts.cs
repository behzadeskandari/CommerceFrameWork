using Commerce.Framework.Core.Results;
using Commerce.SmartstoreImport.Domain.Enums;

namespace Commerce.SmartstoreImport.Contracts;

public sealed record SmartstoreImportOptions(
    string SqlFilePath,
    bool AllowDuplicateRun = false,
    bool StopOnFirstError = false,
    bool ValidateRelationships = true,
    int? MaxRecordsPerEntity = null);

public sealed record SmartstoreSchemaTableInfo(
    string TableName,
    IReadOnlyList<string> Columns,
    int RowCount);

public sealed record SmartstoreSchemaReport(
    string SourceFilePath,
    string SourceFileHash,
    IReadOnlyList<SmartstoreSchemaTableInfo> Tables,
    IReadOnlyList<string> Warnings);

public sealed record SmartstoreImportIssueDto(
    ImportIssueSeverity Severity,
    string EntityType,
    int? SourceId,
    string Code,
    string Message,
    string? Details);

public sealed record SmartstoreEntityImportSummary(
    string EntityType,
    string SourceTable,
    int SourceCount,
    int ImportedCount,
    int SkippedCount,
    int ErrorCount,
    int WarningCount,
    bool WasPresent);

public sealed record SmartstoreImportResult(
    int ImportRunId,
    ImportRunStatus Status,
    string SourceFilePath,
    string SourceFileHash,
    int TablesDiscovered,
    int RecordsImported,
    int WarningCount,
    int ErrorCount,
    IReadOnlyList<SmartstoreEntityImportSummary> EntitySummaries,
    IReadOnlyList<SmartstoreImportIssueDto> Issues,
    string Summary);

public interface ISmartstoreImportService
{
    Task<Result<SmartstoreSchemaReport>> InspectSchemaAsync(string sqlFilePath, CancellationToken cancellationToken = default);

    Task<Result<SmartstoreImportResult>> ImportAsync(SmartstoreImportOptions options, CancellationToken cancellationToken = default);
}

public sealed record SmartstoreReconciliationOptions(
    string SqlFilePath,
    int? ImportRunId = null,
    bool IncludeRecordLevelDetails = true);

public sealed record SmartstoreReconciliationDiscrepancy(
    string CheckName,
    ReconciliationClassification Classification,
    string EntityType,
    int? SourceId,
    string? SourceKey,
    string Explanation,
    string Remediation);

public sealed record SmartstoreReconciliationCheckSummary(
    string CheckName,
    string Category,
    ReconciliationClassification OverallClassification,
    int SourceCount,
    int TargetCount,
    int ExpectedCount,
    int MatchCount,
    int MissingCount,
    int DuplicateCount,
    int TransformedCount,
    int InvalidCount,
    int NotApplicableCount,
    string Summary);

public sealed record SmartstoreReconciliationResult(
    int ImportRunId,
    string SourceFilePath,
    string SourceFileHash,
    DateTime GeneratedAtUtc,
    bool IsFullyReconciled,
    int TotalDiscrepancies,
    IReadOnlyDictionary<ReconciliationClassification, int> ClassificationCounts,
    IReadOnlyList<SmartstoreReconciliationCheckSummary> CheckSummaries,
    IReadOnlyList<SmartstoreReconciliationDiscrepancy> Discrepancies,
    string Summary);

public interface ISmartstoreReconciliationService
{
    Task<Result<SmartstoreReconciliationResult>> ReconcileAsync(
        SmartstoreReconciliationOptions options,
        CancellationToken cancellationToken = default);
}

public interface ISmartstoreSqlParser
{
    Result<SmartstoreParsedDataSet> ParseFile(string sqlFilePath, CancellationToken cancellationToken = default);
}

public sealed record SmartstoreParsedDataSet(
    string SourceFilePath,
    string SourceFileHash,
    IReadOnlyDictionary<string, SmartstoreParsedTable> Tables,
    IReadOnlyList<string> ParseWarnings);

public sealed record SmartstoreParsedTable(
    string TableName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<SmartstoreParsedRow> Rows);

public sealed record SmartstoreParsedRow(
    int SourceLineNumber,
    IReadOnlyDictionary<string, object?> Values);
