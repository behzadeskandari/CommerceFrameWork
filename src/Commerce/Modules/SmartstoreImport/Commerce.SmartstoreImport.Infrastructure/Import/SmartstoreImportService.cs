using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Data.Db;
using Commerce.SmartstoreImport.Application.Abstractions;
using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Domain.Entities;
using Commerce.SmartstoreImport.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Commerce.SmartstoreImport.Infrastructure.Import;

internal sealed class SmartstoreImportService(
    ISmartstoreSqlParser sqlParser,
    IEnumerable<ISmartstoreEntityImporter> importers,
    IServiceScopeFactory scopeFactory,
    ILogger<SmartstoreImportService> logger) : ISmartstoreImportService
{
    public Task<Result<SmartstoreSchemaReport>> InspectSchemaAsync(string sqlFilePath, CancellationToken cancellationToken = default)
    {
        var parseResult = sqlParser.ParseFile(sqlFilePath, cancellationToken);
        if (parseResult.IsFailure)
        {
            return Task.FromResult(Result.Failure<SmartstoreSchemaReport>(parseResult.Error));
        }

        var dataSet = parseResult.Value;
        var tables = dataSet.Tables.Values
            .OrderBy(t => t.TableName, StringComparer.OrdinalIgnoreCase)
            .Select(t => new SmartstoreSchemaTableInfo(t.TableName, t.Columns, t.Rows.Count))
            .ToList();

        return Task.FromResult(Result.Success(new SmartstoreSchemaReport(
            dataSet.SourceFilePath,
            dataSet.SourceFileHash,
            tables,
            dataSet.ParseWarnings)));
    }

    public async Task<Result<SmartstoreImportResult>> ImportAsync(
        SmartstoreImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.SqlFilePath) || !File.Exists(options.SqlFilePath))
        {
            return Result.Failure<SmartstoreImportResult>(
                Error.Validation("smartstore.import.file_missing", "Smartstore SQL file was not found."));
        }

        var parseResult = sqlParser.ParseFile(options.SqlFilePath, cancellationToken);
        if (parseResult.IsFailure)
        {
            return Result.Failure<SmartstoreImportResult>(parseResult.Error);
        }

        var dataSet = parseResult.Value;
        if (dataSet.Tables.Count == 0)
        {
            return Result.Failure<SmartstoreImportResult>(
                Error.Validation("smartstore.import.no_tables", "No Smartstore tables were discovered in the SQL file."));
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();

        if (!options.AllowDuplicateRun)
        {
            var priorRun = await dbContext.Set<ImportRun>()
                .AsNoTracking()
                .Where(x => x.SourceFileHash == dataSet.SourceFileHash && x.Status != ImportRunStatus.Failed)
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (priorRun is not null)
            {
                return Result.Failure<SmartstoreImportResult>(Error.Conflict(
                    "smartstore.import.duplicate",
                    $"This SQL export was already imported (run #{priorRun.Id}). Set AllowDuplicateRun=true to repeat."));
            }
        }

        var importRun = ImportRun.Start(dataSet.SourceFilePath, dataSet.SourceFileHash, dataSet.Tables.Count);
        dbContext.Set<ImportRun>().Add(importRun);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var issueReporter = new ImportIssueReporter(importRun.Id);
        var idRegistry = new ImportIdRegistry(dbContext, importRun.Id);
        await idRegistry.LoadExistingAsync(importRunId: null, cancellationToken).ConfigureAwait(false);

        var importContext = new SmartstoreImportContext
        {
            DataSet = dataSet,
            ImportRunId = importRun.Id,
            Options = options,
            IdRegistry = idRegistry,
            Issues = issueReporter,
            Services = scope.ServiceProvider
        };

        var summaries = new List<SmartstoreEntityImportSummary>();
        var orderedImporters = importers.OrderBy(x => x.Order).ToList();

        foreach (var importer in orderedImporters)
        {
            if (!importer.CanImport(dataSet))
            {
                logger.LogInformation("Skipping importer {Importer} — source tables not present.", importer.EntityType);
                continue;
            }

            logger.LogInformation("Running Smartstore importer {Importer}.", importer.EntityType);

            IDbContextTransaction? transaction = null;
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var summary = await importer.ImportAsync(importContext, cancellationToken).ConfigureAwait(false);
                summaries.Add(summary);
                await idRegistry.PersistAsync(cancellationToken).ConfigureAwait(false);

                var pendingIssues = issueReporter.TakePendingIssues();
                if (pendingIssues.Count > 0)
                {
                    dbContext.Set<ImportIssue>().AddRange(pendingIssues);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }
                issueReporter.Error(importer.EntityType, null, "importer_failed", ex.Message, ex.ToString());
                logger.LogError(ex, "Smartstore importer {Importer} failed.", importer.EntityType);

                if (options.StopOnFirstError)
                {
                    importRun.Fail($"Importer {importer.EntityType} failed: {ex.Message}");
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    break;
                }
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync().ConfigureAwait(false);
                }
            }

            if (options.StopOnFirstError && issueReporter.ErrorCount > 0)
            {
                break;
            }
        }

        var recordsImported = summaries.Sum(x => x.ImportedCount);
        var summaryText =
            $"Imported {recordsImported} records from {dataSet.Tables.Count} tables with {issueReporter.WarningCount} warnings and {issueReporter.ErrorCount} errors.";
        importRun.Complete(recordsImported, issueReporter.WarningCount, issueReporter.ErrorCount, summaryText);
        var finalIssues = issueReporter.TakePendingIssues();
        if (finalIssues.Count > 0)
        {
            dbContext.Set<ImportIssue>().AddRange(finalIssues);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var status = importRun.Status;
        var result = new SmartstoreImportResult(
            importRun.Id,
            status,
            dataSet.SourceFilePath,
            dataSet.SourceFileHash,
            dataSet.Tables.Count,
            recordsImported,
            issueReporter.WarningCount,
            issueReporter.ErrorCount,
            summaries,
            issueReporter.GetIssues(),
            summaryText);

        return status == ImportRunStatus.Failed
            ? Result.Failure<SmartstoreImportResult>(Error.Failure(summaryText, ErrorCode.OperationFailed))
            : Result.Success(result);
    }
}
