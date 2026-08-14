using Commerce.Framework.Data.Db;
using Commerce.SmartstoreImport.Application.Abstractions;
using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.SmartstoreImport.Infrastructure.Import;

internal sealed class ImportIdRegistry(CommerceDbContext dbContext, int importRunId) : IImportIdRegistry
{
    private readonly Dictionary<(string EntityType, int SourceId), ImportIdMapping> _mappings = new();
    private readonly HashSet<(string EntityType, int SourceId)> _persistedKeys = [];

    public bool TryGetTargetId(string entityType, int sourceId, out int targetId)
    {
        if (_mappings.TryGetValue((entityType, sourceId), out var mapping))
        {
            targetId = mapping.TargetId;
            return true;
        }

        targetId = default;
        return false;
    }

    public void Register(string entityType, int sourceId, int targetId, string? sourceKey = null)
    {
        _mappings[(entityType, sourceId)] = new ImportIdMapping
        {
            ImportRunId = importRunId,
            EntityType = entityType,
            SourceId = sourceId,
            TargetId = targetId,
            SourceKey = sourceKey
        };
    }

    public async Task PersistAsync(CancellationToken cancellationToken = default)
    {
        var pending = _mappings.Values
            .Where(x => !_persistedKeys.Contains((x.EntityType, x.SourceId)))
            .ToList();

        if (pending.Count == 0)
        {
            return;
        }

        dbContext.Set<ImportIdMapping>().AddRange(pending);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var mapping in pending)
        {
            _persistedKeys.Add((mapping.EntityType, mapping.SourceId));
        }
    }

    public async Task LoadExistingAsync(int? importRunId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Set<ImportIdMapping>().AsNoTracking();
        if (importRunId.HasValue)
        {
            query = query.Where(x => x.ImportRunId == importRunId.Value);
        }

        var existing = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var mapping in existing)
        {
            _mappings[(mapping.EntityType, mapping.SourceId)] = mapping;
            _persistedKeys.Add((mapping.EntityType, mapping.SourceId));
        }
    }
}

internal sealed class ImportIssueReporter : IImportIssueReporter
{
    private readonly List<SmartstoreImportIssueDto> _issues = [];
    private readonly List<ImportIssue> _pending = [];
    private readonly int _importRunId;

    public ImportIssueReporter(int importRunId) => _importRunId = importRunId;

    public int WarningCount => _issues.Count(x => x.Severity == Domain.Enums.ImportIssueSeverity.Warning);

    public int ErrorCount => _issues.Count(x => x.Severity == Domain.Enums.ImportIssueSeverity.Error);

    public void Warning(string entityType, int? sourceId, string code, string message, string? details = null) =>
        Add(Domain.Enums.ImportIssueSeverity.Warning, entityType, sourceId, code, message, details);

    public void Error(string entityType, int? sourceId, string code, string message, string? details = null) =>
        Add(Domain.Enums.ImportIssueSeverity.Error, entityType, sourceId, code, message, details);

    public IReadOnlyList<SmartstoreImportIssueDto> GetIssues() => _issues;

    internal IReadOnlyList<ImportIssue> TakePendingIssues()
    {
        if (_pending.Count == 0)
        {
            return [];
        }

        var batch = _pending.ToList();
        _pending.Clear();
        return batch;
    }

    private void Add(Domain.Enums.ImportIssueSeverity severity, string entityType, int? sourceId, string code, string message, string? details)
    {
        _issues.Add(new SmartstoreImportIssueDto(severity, entityType, sourceId, code, message, details));
        _pending.Add(new ImportIssue
        {
            ImportRunId = _importRunId,
            Severity = severity,
            EntityType = entityType,
            SourceId = sourceId,
            Code = code,
            Message = message,
            Details = details
        });
    }
}
