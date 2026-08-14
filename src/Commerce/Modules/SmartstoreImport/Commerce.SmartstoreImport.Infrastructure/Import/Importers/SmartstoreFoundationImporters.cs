using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Commerce.Framework.Data.Identity;
using Commerce.Framework.Domain.ValueObjects;
using Commerce.SmartstoreImport.Application.Abstractions;
using Commerce.SmartstoreImport.Contracts;
using Commerce.Store.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.SmartstoreImport.Infrastructure.Import.Importers;

internal abstract class SmartstoreEntityImporterBase : ISmartstoreEntityImporter
{
    public abstract int Order { get; }
    public abstract string EntityType { get; }
    public abstract IReadOnlyList<string> SourceTables { get; }

    public virtual bool CanImport(SmartstoreParsedDataSet dataSet) =>
        SourceTables.Any(dataSet.HasTable);

    public abstract Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default);

    protected static SmartstoreEntityImportSummary Summary(
        string entityType,
        string sourceTable,
        int sourceCount,
        int imported,
        int skipped,
        int errors,
        int warnings,
        bool wasPresent) =>
        new(entityType, sourceTable, sourceCount, imported, skipped, errors, warnings, wasPresent);

    protected static CommerceDbContext GetDb(SmartstoreImportContext context) =>
        context.Services.GetRequiredService<CommerceDbContext>();
}

internal sealed class SmartstoreStoreImporter : SmartstoreEntityImporterBase
{
    public override int Order => 30;
    public override string EntityType => SmartstoreImportEntityTypes.Store;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Store];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Store);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Store, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Error(EntityType, null, "missing_id", "Store row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var name = SmartstoreRowReader.GetString(row, "Name") ?? $"Store-{sourceId}";
            var url = SmartstoreRowReader.GetString(row, "Url") ?? "https://localhost/";
            var displayOrder = SmartstoreRowReader.GetInt(row, "DisplayOrder");
            var defaultCurrencySourceId = SmartstoreRowReader.GetInt(row, "PrimaryStoreCurrencyId");
            if (defaultCurrencySourceId == 0)
            {
                defaultCurrencySourceId = SmartstoreRowReader.GetInt(row, "DefaultCurrencyId");
            }

            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Language, 1, out var defaultLanguageId))
            {
                defaultLanguageId = await db.Set<Language>()
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            if (defaultLanguageId <= 0)
            {
                context.Issues.Error(EntityType, sourceId, "language_missing", "No target language exists for store import.");
                errors++;
                continue;
            }

            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Currency, defaultCurrencySourceId, out var defaultCurrencyId))
            {
                defaultCurrencyId = await db.Set<StoreCurrency>()
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            if (defaultCurrencyId <= 0)
            {
                context.Issues.Error(EntityType, sourceId, "currency_missing", "No target currency exists for store import.");
                errors++;
                continue;
            }

            try
            {
                var store = StoreEntity.Create(
                    SmartstoreRowReader.ToSystemName(name),
                    name,
                    url,
                    defaultLanguageId,
                    defaultCurrencyId,
                    displayOrder,
                    isActive: true);

                db.Set<StoreEntity>().Add(store);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, store.Id, name);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Store, table.Rows.Count, imported, skipped, errors, 0, true);
    }
}

internal sealed class SmartstoreLanguageImporter : SmartstoreEntityImporterBase
{
    public override int Order => 10;
    public override string EntityType => SmartstoreImportEntityTypes.Language;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Language];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Language);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Language, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Error(EntityType, null, "missing_id", "Language row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var name = SmartstoreRowReader.GetString(row, "Name") ?? $"Language-{sourceId}";
            var languageCode = SmartstoreRowReader.GetString(row, "UniqueSeoCode")
                ?? SmartstoreRowReader.GetString(row, "LanguageCode")
                ?? SmartstoreRowReader.ToSystemName(name, 10);
            var culture = SmartstoreRowReader.GetString(row, "LanguageCulture") ?? $"{languageCode}-{languageCode.ToUpperInvariant()}";
            var isRtl = SmartstoreRowReader.GetBool(row, "Rtl");
            var displayOrder = SmartstoreRowReader.GetInt(row, "DisplayOrder");
            var published = SmartstoreRowReader.GetBool(row, "Published", true);

            try
            {
                var language = Language.Create(name, languageCode, culture, name, isRtl, displayOrder, published);
                db.Set<Language>().Add(language);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, language.Id, languageCode);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Language, table.Rows.Count, imported, skipped, errors, 0, true);
    }
}

internal sealed class SmartstoreCurrencyImporter : SmartstoreEntityImporterBase
{
    public override int Order => 20;
    public override string EntityType => SmartstoreImportEntityTypes.Currency;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Currency];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Currency);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Currency, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Error(EntityType, null, "missing_id", "Currency row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var code = SmartstoreRowReader.GetString(row, "CurrencyCode") ?? SmartstoreRowReader.GetString(row, "Code");
            if (string.IsNullOrWhiteSpace(code))
            {
                context.Issues.Error(EntityType, sourceId, "missing_code", "Currency row is missing CurrencyCode.");
                errors++;
                continue;
            }

            var name = SmartstoreRowReader.GetString(row, "Name") ?? code;
            var rate = SmartstoreRowReader.GetDecimal(row, "Rate", 1m);
            if (rate <= 0)
            {
                context.Issues.Warning(EntityType, sourceId, "invalid_rate", "Currency rate was invalid; defaulted to 1.", rate.ToString());
                rate = 1m;
                warnings++;
            }

            var displayOrder = SmartstoreRowReader.GetInt(row, "DisplayOrder");
            var published = SmartstoreRowReader.GetBool(row, "Published", true);

            try
            {
                var currency = StoreCurrency.Create(code, name, code, name, rate, decimalPlaces: 2, displayOrder, published);
                db.Set<StoreCurrency>().Add(currency);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, currency.Id, code);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Currency, table.Rows.Count, imported, skipped, errors, warnings, true);
    }
}

internal sealed class SmartstoreSettingImporter : SmartstoreEntityImporterBase
{
    public override int Order => 40;
    public override string EntityType => SmartstoreImportEntityTypes.Setting;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Setting];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Setting);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Setting, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;

        foreach (var row in table.Rows)
        {
            var name = SmartstoreRowReader.GetString(row, "Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                context.Issues.Error(EntityType, null, "missing_name", "Setting row is missing Name.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            var storeSourceId = SmartstoreRowReader.GetInt(row, "StoreId");
            var targetStoreId = 0;
            if (storeSourceId > 0 &&
                !context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Store, storeSourceId, out targetStoreId))
            {
                context.Issues.Warning(EntityType, storeSourceId, "store_ref_missing", $"Setting '{name}' references missing store {storeSourceId}; imported as global.");
                targetStoreId = 0;
                warnings++;
            }

            var value = SmartstoreRowReader.GetString(row, "Value") ?? string.Empty;
            var exists = await db.Set<Setting>()
                .AnyAsync(x => x.Name == name && x.StoreId == targetStoreId, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                skipped++;
                continue;
            }

            db.Set<Setting>().Add(new Setting
            {
                Name = name,
                Value = value,
                StoreId = targetStoreId,
                DataType = "string"
            });
            imported++;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Summary(EntityType, SmartstoreImportTableNames.Setting, table.Rows.Count, imported, skipped, errors, warnings, true);
    }
}

internal sealed class SmartstoreCustomerImporter : SmartstoreEntityImporterBase
{
    public override int Order => 50;
    public override string EntityType => SmartstoreImportEntityTypes.Customer;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Customer];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Customer);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Customer, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var userManager = context.Services.GetRequiredService<UserManager<CommerceIdentityUser>>();
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Error(EntityType, null, "missing_id", "Customer row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var isSystem = SmartstoreRowReader.GetBool(row, "IsSystemAccount");
            var deleted = SmartstoreRowReader.GetBool(row, "Deleted");
            var email = SmartstoreRowReader.GetString(row, "Email");
            if (isSystem || string.IsNullOrWhiteSpace(email))
            {
                context.Issues.Warning(
                    EntityType,
                    sourceId,
                    "customer_skipped",
                    "System or email-less customer was not imported; legacy ID preserved in report only.",
                    SmartstoreRowReader.GetString(row, "SystemName"));
                warnings++;
                continue;
            }

            var firstName = SmartstoreRowReader.GetString(row, "FirstName") ?? "Imported";
            var lastName = SmartstoreRowReader.GetString(row, "LastName") ?? $"Customer-{sourceId}";
            var active = SmartstoreRowReader.GetBool(row, "Active", true) && !deleted;

            try
            {
                var identityUser = new CommerceIdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = $"{firstName} {lastName}".Trim()
                };

                var createResult = await userManager.CreateAsync(identityUser, $"Import!{Guid.NewGuid():N}").ConfigureAwait(false);
                if (!createResult.Succeeded)
                {
                    var details = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    context.Issues.Error(EntityType, sourceId, "identity_create_failed", "Failed to create identity user.", details);
                    errors++;
                    continue;
                }

                var customer = Commerce.Customers.Domain.Entities.Customer.Create(
                    identityUser.Id,
                    email,
                    firstName,
                    lastName,
                    active: active);

                if (!active)
                {
                    customer.Deactivate();
                }

                if (deleted)
                {
                    customer.MarkDeleted();
                }

                db.Set<Commerce.Customers.Domain.Entities.Customer>().Add(customer);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, customer.Id, email);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Customer, table.Rows.Count, imported, skipped, errors, warnings, true);
    }
}
