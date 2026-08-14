using Commerce.Framework.Core.Entities;
using Commerce.SmartstoreImport.Domain.Enums;

namespace Commerce.SmartstoreImport.Domain.Entities;

public sealed class ImportRun : AggregateRoot
{
    public string SourceFilePath { get; private set; } = string.Empty;

    public string SourceFileHash { get; private set; } = string.Empty;

    public ImportRunStatus Status { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public int TablesDiscovered { get; private set; }

    public int RecordsImported { get; private set; }

    public int WarningCount { get; private set; }

    public int ErrorCount { get; private set; }

    public string? Summary { get; private set; }

    public static ImportRun Start(string sourceFilePath, string sourceFileHash, int tablesDiscovered)
    {
        return new ImportRun
        {
            SourceFilePath = sourceFilePath,
            SourceFileHash = sourceFileHash,
            Status = ImportRunStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            TablesDiscovered = tablesDiscovered
        };
    }

    public void Complete(int recordsImported, int warningCount, int errorCount, string summary)
    {
        RecordsImported = recordsImported;
        WarningCount = warningCount;
        ErrorCount = errorCount;
        Summary = summary;
        CompletedAtUtc = DateTime.UtcNow;
        Status = errorCount > 0 && recordsImported == 0
            ? ImportRunStatus.Failed
            : errorCount > 0
                ? ImportRunStatus.CompletedWithWarnings
                : warningCount > 0
                    ? ImportRunStatus.CompletedWithWarnings
                    : ImportRunStatus.Completed;
    }

    public void Fail(string summary)
    {
        Summary = summary;
        CompletedAtUtc = DateTime.UtcNow;
        Status = ImportRunStatus.Failed;
    }
}

public sealed class ImportIdMapping : Entity
{
    public int ImportRunId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public int SourceId { get; set; }

    public int TargetId { get; set; }

    public string? SourceKey { get; set; }
}

public sealed class ImportIssue : Entity
{
    public int ImportRunId { get; set; }

    public ImportIssueSeverity Severity { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public int? SourceId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }
}
