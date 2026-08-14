using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Data.Db;
using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Domain.Entities;
using Commerce.SmartstoreImport.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.SmartstoreImport.Infrastructure.Reconciliation;

internal sealed class SmartstoreReconciliationService(
    ISmartstoreSqlParser sqlParser,
    IServiceScopeFactory scopeFactory) : ISmartstoreReconciliationService
{
    public async Task<Result<SmartstoreReconciliationResult>> ReconcileAsync(
        SmartstoreReconciliationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.SqlFilePath) || !File.Exists(options.SqlFilePath))
        {
            return Result.Failure<SmartstoreReconciliationResult>(
                Error.Validation("smartstore.reconciliation.file_missing", "Smartstore SQL file was not found."));
        }

        var parseResult = sqlParser.ParseFile(options.SqlFilePath, cancellationToken);
        if (parseResult.IsFailure)
        {
            return Result.Failure<SmartstoreReconciliationResult>(parseResult.Error!);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var dataSet = parseResult.Value!;

        var importRun = await ResolveImportRunAsync(db, dataSet.SourceFileHash, options.ImportRunId, cancellationToken)
            .ConfigureAwait(false);

        if (importRun is null)
        {
            return Result.Failure<SmartstoreReconciliationResult>(
                Error.NotFound("smartstore.reconciliation.run_missing",
                    "No completed import run found for this SQL file. Run import before reconciliation."));
        }

        var runIds = await db.Set<ImportRun>()
            .AsNoTracking()
            .Where(x => x.SourceFileHash == importRun.SourceFileHash && x.Status != ImportRunStatus.Failed)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var mappings = await db.Set<ImportIdMapping>()
            .AsNoTracking()
            .Where(x => runIds.Contains(x.ImportRunId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var issues = await db.Set<ImportIssue>()
            .AsNoTracking()
            .Where(x => x.ImportRunId == importRun.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var context = new ReconciliationContext
        {
            DataSet = dataSet,
            ImportRun = importRun,
            MappingIndex = ReconciliationMappingIndex.FromMappings(mappings),
            ImportIssues = issues,
            Db = db,
            IncludeRecordLevelDetails = options.IncludeRecordLevelDetails
        };

        var checkSummaries = await SmartstoreReconciliationChecks.RunAllAsync(context, cancellationToken).ConfigureAwait(false);
        var classificationCounts = context.Discrepancies
            .GroupBy(x => x.Classification)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var classification in Enum.GetValues<ReconciliationClassification>())
        {
            classificationCounts.TryAdd(classification, 0);
        }

        var blocking = context.Discrepancies.Count(x =>
            x.Classification is ReconciliationClassification.Missing
                or ReconciliationClassification.Duplicate
                or ReconciliationClassification.Invalid);

        var summary =
            $"Reconciliation for import run #{importRun.Id}: {checkSummaries.Count} checks, {context.Discrepancies.Count} discrepancies ({blocking} blocking).";

        var result = new SmartstoreReconciliationResult(
            importRun.Id,
            dataSet.SourceFilePath,
            dataSet.SourceFileHash,
            DateTime.UtcNow,
            IsFullyReconciled: blocking == 0,
            TotalDiscrepancies: context.Discrepancies.Count,
            ClassificationCounts: classificationCounts,
            CheckSummaries: checkSummaries,
            Discrepancies: context.Discrepancies,
            Summary: summary);

        return Result.Success(result);
    }

    private static async Task<ImportRun?> ResolveImportRunAsync(
        CommerceDbContext db,
        string sourceFileHash,
        int? importRunId,
        CancellationToken cancellationToken)
    {
        if (importRunId.HasValue)
        {
            return await db.Set<ImportRun>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == importRunId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        return await db.Set<ImportRun>()
            .AsNoTracking()
            .Where(x => x.SourceFileHash == sourceFileHash && x.Status != ImportRunStatus.Failed)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
