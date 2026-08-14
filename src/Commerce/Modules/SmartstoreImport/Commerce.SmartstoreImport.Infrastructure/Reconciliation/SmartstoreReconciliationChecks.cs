using Commerce.Catalog.Domain.Entities;
using Commerce.Cms.Domain.Entities;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Commerce.Media.Domain.Entities;
using Commerce.Orders.Domain.Entities;
using Commerce.Pricing.Domain.Entities;
using Commerce.Reviews.Domain.Entities;
using Commerce.Seo.Domain.Entities;
using Commerce.SmartstoreImport.Application.Abstractions;
using Commerce.SmartstoreImport.Contracts;
using Commerce.SmartstoreImport.Domain.Enums;
using Commerce.SmartstoreImport.Infrastructure.Import;
using Commerce.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OrderEntity = Commerce.Orders.Domain.Entities.Order;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.SmartstoreImport.Infrastructure.Reconciliation;

internal static class SmartstoreReconciliationChecks
{
    public static async Task<IReadOnlyList<SmartstoreReconciliationCheckSummary>> RunAllAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        var checks = new List<SmartstoreReconciliationCheckSummary>
        {
            await CheckStoreDataAsync(context, cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.Product, SmartstoreImportTableNames.Product, "Products", "Catalog", cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, "Category", SmartstoreImportTableNames.Category, "Categories", "Catalog", cancellationToken).ConfigureAwait(false),
            await CheckCustomersAsync(context, cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.Order, SmartstoreImportTableNames.Order, "Orders", "Orders", cancellationToken).ConfigureAwait(false),
            await CheckOrderItemsAsync(context, cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.ProductReview, SmartstoreImportTableNames.ProductReview, "Reviews", "Reviews", cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.MediaAsset, SmartstoreImportTableNames.MediaFile, "Media", "Media", cancellationToken).ConfigureAwait(false),
            CheckDownloads(context),
            await CheckPricesAsync(context, cancellationToken).ConfigureAwait(false),
            await CheckRelationshipsAsync(context, cancellationToken).ConfigureAwait(false),
            await CheckLocalizationAsync(context, cancellationToken).ConfigureAwait(false),
            await CheckSeoUrlsAsync(context, cancellationToken).ConfigureAwait(false),
            CheckManufacturer(context),
            CheckDuplicateMappings(context)
        };

        return checks;
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckStoreDataAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        const string checkName = "StoreData";
        var subChecks = new[]
        {
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.Language, SmartstoreImportTableNames.Language, "Languages", checkName, cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.Currency, SmartstoreImportTableNames.Currency, "Currencies", checkName, cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.Store, SmartstoreImportTableNames.Store, "Stores", checkName, cancellationToken).ConfigureAwait(false),
            await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.Setting, SmartstoreImportTableNames.Setting, "Settings", checkName, cancellationToken).ConfigureAwait(false)
        };

        var present = subChecks.Where(x => x.SourceCount > 0 || x.TargetCount > 0).ToList();
        if (present.Count == 0)
        {
            return Summary(checkName, "Store", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "No store foundation tables present in source export.");
        }

        return Aggregate(checkName, "Store", present);
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckEntityCountsAsync(
        ReconciliationContext context,
        string entityType,
        string sourceTable,
        string checkName,
        string category,
        CancellationToken cancellationToken)
    {
        var table = context.DataSet.GetTable(sourceTable);
        if (table is null)
        {
            return Summary(checkName, category, ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                $"Source table '{sourceTable}' not present in export.");
        }

        var match = 0;
        var missing = 0;
        var duplicate = 0;
        var transformed = 0;
        var invalid = 0;
        var notApplicable = 0;
        var targetExistsCache = new Dictionary<int, bool>();

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                invalid++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Invalid, entityType, null, null,
                    "Source row is missing Id.",
                    "Fix source export or exclude malformed rows before import.");
                continue;
            }

            var sourceKey = SmartstoreRowReader.GetString(row, "Name")
                ?? SmartstoreRowReader.GetString(row, "Sku")
                ?? SmartstoreRowReader.GetString(row, "Email")
                ?? SmartstoreRowReader.GetString(row, "OrderNumber");

            if (context.MappingIndex.HasDuplicate(entityType, sourceId))
            {
                duplicate++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Duplicate, entityType, sourceId, sourceKey,
                    $"Multiple legacy ID mappings exist for source Id {sourceId}.",
                    "Review ImportIdMapping for duplicate runs; keep a single canonical mapping per source Id.");
                continue;
            }

            var issue = context.FindIssue(entityType, sourceId);
            if (issue?.Severity == ImportIssueSeverity.Error)
            {
                invalid++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Invalid, entityType, sourceId, sourceKey,
                    issue.Message,
                    "Resolve import error and re-run import for this record.");
                continue;
            }

            if (!context.MappingIndex.TryGetTargetId(entityType, sourceId, out var targetId))
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, entityType, sourceId, sourceKey,
                    "Source record has no Commerce mapping after import.",
                    "Re-run import, inspect ImportIssue log, or manually map legacy Id.");
                continue;
            }

            if (!targetExistsCache.TryGetValue(targetId, out var exists))
            {
                exists = await TargetEntityExistsAsync(context.Db, entityType, targetId, cancellationToken).ConfigureAwait(false);
                targetExistsCache[targetId] = exists;
            }

            if (!exists)
            {
                invalid++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Invalid, entityType, sourceId, sourceKey,
                    $"Mapping points to missing Commerce entity Id {targetId}.",
                    "Restore target entity or clear stale mapping and re-import.");
                continue;
            }

            if (issue?.Severity == ImportIssueSeverity.Warning)
            {
                transformed++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Transformed, entityType, sourceId, sourceKey,
                    issue.Message,
                    "Review transformed field values in Commerce; adjust manually if business-critical.");
            }
            else
            {
                match++;
            }
        }

        var sourceCount = table.Rows.Count;
        var targetCount = context.MappingIndex.Count(entityType);
        var expectedCount = sourceCount - notApplicable;
        var overall = OverallClassification(match, missing, duplicate, transformed, invalid, notApplicable, sourceCount);

        return Summary(checkName, category, overall, sourceCount, targetCount, expectedCount,
            match, missing, duplicate, transformed, invalid, notApplicable,
            $"{checkName}: {match} matched, {missing} missing, {duplicate} duplicate, {transformed} transformed, {invalid} invalid of {sourceCount} source rows.");
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckCustomersAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        const string checkName = "Customers";
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Customer);
        if (table is null)
        {
            return Summary(checkName, "Customers", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "Customer table not present in export.");
        }

        var match = 0;
        var missing = 0;
        var duplicate = 0;
        var transformed = 0;
        var invalid = 0;
        var notApplicable = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                invalid++;
                continue;
            }

            var email = SmartstoreRowReader.GetString(row, "Email");
            if (SmartstoreRowReader.GetBool(row, "IsSystemAccount"))
            {
                notApplicable++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.NotApplicable,
                    SmartstoreImportEntityTypes.Customer, sourceId, email,
                    "System account customers are not migrated.",
                    "Recreate system accounts manually in Commerce if required.");
                continue;
            }

            if (context.MappingIndex.HasDuplicate(SmartstoreImportEntityTypes.Customer, sourceId))
            {
                duplicate++;
                continue;
            }

            if (!context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Customer, sourceId, out var targetId))
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing,
                    SmartstoreImportEntityTypes.Customer, sourceId, email,
                    "Customer was not imported.",
                    "Re-run customer import or create customer manually and add ImportIdMapping.");
                continue;
            }

            var exists = await context.Db.Set<Customer>().AnyAsync(x => x.Id == targetId, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                invalid++;
                continue;
            }

            match++;
        }

        var sourceCount = table.Rows.Count;
        var expectedCount = sourceCount - notApplicable;
        var overall = OverallClassification(match, missing, duplicate, transformed, invalid, notApplicable, expectedCount);

        return Summary(checkName, "Customers", overall, sourceCount, context.MappingIndex.Count(SmartstoreImportEntityTypes.Customer),
            expectedCount, match, missing, duplicate, transformed, invalid, notApplicable,
            $"Customers: {match}/{expectedCount} migratable customers matched.");
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckOrderItemsAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        const string checkName = "OrderItems";
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.OrderItem);
        if (table is null)
        {
            return Summary(checkName, "Orders", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "OrderItem table not present in export.");
        }

        var match = 0;
        var missing = 0;
        var invalid = 0;
        var targetCount = await context.Db.Set<OrderItem>().CountAsync(cancellationToken).ConfigureAwait(false);

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                invalid++;
                continue;
            }

            var orderSourceId = SmartstoreRowReader.GetInt(row, "OrderId");
            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            var sku = SmartstoreRowReader.GetString(row, "Sku");

            if (!context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Order, orderSourceId, out var orderId))
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "OrderItem", sourceId, sku,
                    $"Order item references unimported order {orderSourceId}.",
                    "Import parent order first or remove orphan order item from source.");
                continue;
            }

            if (!context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out _))
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "OrderItem", sourceId, sku,
                    $"Order item references unimported product {productSourceId}.",
                    "Import product or adjust order item to reference an existing product.");
                continue;
            }

            var orderExists = await context.Db.Set<OrderEntity>().AnyAsync(x => x.Id == orderId, cancellationToken).ConfigureAwait(false);
            if (!orderExists)
            {
                invalid++;
                continue;
            }

            var itemExists = await context.Db.Set<OrderItem>()
                .AnyAsync(x => x.OrderId == orderId && x.Sku == sku, cancellationToken)
                .ConfigureAwait(false);

            if (itemExists)
            {
                match++;
            }
            else
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "OrderItem", sourceId, sku,
                    "Order imported but line item not found in Commerce.",
                    "Re-import order or manually add OrderItem from import log.");
            }
        }

        var sourceCount = table.Rows.Count;
        var overall = OverallClassification(match, missing, 0, 0, invalid, 0, sourceCount);

        return Summary(checkName, "Orders", overall, sourceCount, targetCount, sourceCount,
            match, missing, 0, 0, invalid, 0,
            $"Order items: {match}/{sourceCount} verified in Commerce ({targetCount} total line items).");
    }

    private static SmartstoreReconciliationCheckSummary CheckDownloads(ReconciliationContext context)
    {
        const string checkName = "Downloads";
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Download);
        if (table is null)
        {
            return Summary(checkName, "Downloads", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "Download table not present in export.");
        }

        foreach (var row in table.Rows)
        {
            SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId);
            context.AddDiscrepancy(checkName, ReconciliationClassification.NotApplicable, "Download", sourceId,
                SmartstoreRowReader.GetString(row, "DownloadGuid") ?? SmartstoreRowReader.GetString(row, "Filename"),
                "Download entities are not imported in Phase 46/47.",
                "Implement Download importer and binary file migration in a follow-up phase.");
        }

        return Summary(checkName, "Downloads", ReconciliationClassification.NotApplicable, table.Rows.Count, 0, 0,
            0, 0, 0, 0, 0, table.Rows.Count,
            $"{table.Rows.Count} download rows require a future importer.");
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckPricesAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        const string checkName = "Prices";
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Product);
        if (table is null)
        {
            return Summary(checkName, "Pricing", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "Product table not present; price reconciliation skipped.");
        }

        var match = 0;
        var missing = 0;
        var transformed = 0;
        var notApplicable = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                continue;
            }

            var sourcePrice = SmartstoreRowReader.GetDecimal(row, "Price");
            if (sourcePrice <= 0)
            {
                notApplicable++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.NotApplicable,
                    SmartstoreImportEntityTypes.ProductOffer, sourceId, SmartstoreRowReader.GetString(row, "Sku"),
                    "Source product has no positive Price; ProductOffer not expected.",
                    "Create ProductOffer manually if a price is required.");
                continue;
            }

            if (!context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.ProductOffer, sourceId, out var offerId))
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing,
                    SmartstoreImportEntityTypes.ProductOffer, sourceId, SmartstoreRowReader.GetString(row, "Sku"),
                    "Product has source price but no imported ProductOffer mapping.",
                    "Re-import product or create ProductOffer for default store/currency.");
                continue;
            }

            var offer = await context.Db.Set<ProductOffer>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == offerId, cancellationToken)
                .ConfigureAwait(false);

            if (offer is null)
            {
                missing++;
                continue;
            }

            if (offer.Price == sourcePrice)
            {
                match++;
            }
            else
            {
                transformed++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Transformed,
                    SmartstoreImportEntityTypes.ProductOffer, sourceId, SmartstoreRowReader.GetString(row, "Sku"),
                    $"Source price {sourcePrice} differs from Commerce offer price {offer.Price}.",
                    "Verify currency/rate/tax rules; update ProductOffer if incorrect.");
            }
        }

        var pricedSourceCount = table.Rows.Count(r =>
            SmartstoreRowReader.TryGetInt(r, "Id", out _) && SmartstoreRowReader.GetDecimal(r, "Price") > 0);
        var targetCount = context.MappingIndex.Count(SmartstoreImportEntityTypes.ProductOffer);
        var overall = OverallClassification(match, missing, 0, transformed, 0, notApplicable, pricedSourceCount);

        return Summary(checkName, "Pricing", overall, pricedSourceCount, targetCount, pricedSourceCount,
            match, missing, 0, transformed, 0, notApplicable,
            $"Prices: {match} exact matches, {transformed} transformed, {missing} missing offers.");
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckRelationshipsAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        const string checkName = "Relationships";
        var match = 0;
        var missing = 0;
        var invalid = 0;
        var sourceCount = 0;

        var categoryResult = await CheckProductCategoryMappingsAsync(context, checkName, cancellationToken).ConfigureAwait(false);
        sourceCount += categoryResult.SourceCount;
        match += categoryResult.MatchCount;
        missing += categoryResult.MissingCount;

        var mediaResult = await CheckProductMediaMappingsAsync(context, checkName, cancellationToken).ConfigureAwait(false);
        sourceCount += mediaResult.SourceCount;
        match += mediaResult.MatchCount;
        missing += mediaResult.MissingCount;

        var orderCustomerResult = CheckOrderCustomerRefs(context, checkName);
        sourceCount += orderCustomerResult.SourceCount;
        match += orderCustomerResult.MatchCount;
        missing += orderCustomerResult.MissingCount;

        var orderItemResult = CheckOrderItemProductRefs(context, checkName);
        sourceCount += orderItemResult.SourceCount;
        match += orderItemResult.MatchCount;
        missing += orderItemResult.MissingCount;

        if (sourceCount == 0)
        {
            return Summary(checkName, "Relationships", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "No relationship mapping tables present in export.");
        }

        var overall = OverallClassification(match, missing, 0, 0, invalid, 0, sourceCount);
        return Summary(checkName, "Relationships", overall, sourceCount, match, sourceCount,
            match, missing, 0, 0, invalid, 0,
            $"Relationships: {match}/{sourceCount} references resolve in Commerce.");
    }

    private readonly record struct RelationshipCheckResult(int SourceCount, int MatchCount, int MissingCount);

    private static async Task<RelationshipCheckResult> CheckProductCategoryMappingsAsync(
        ReconciliationContext context,
        string checkName,
        CancellationToken cancellationToken)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.ProductCategoryMapping);
        if (table is null)
        {
            return default;
        }

        var match = 0;
        var missing = 0;

        foreach (var row in table.Rows)
        {
            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            var categorySourceId = SmartstoreRowReader.GetInt(row, "CategoryId");

            if (!context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out var productId) ||
                !context.MappingIndex.TryGetTargetId("Category", categorySourceId, out var categoryId))
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "ProductCategory", productSourceId,
                    $"{productSourceId}->{categorySourceId}",
                    "Product-category mapping references unmapped product or category.",
                    "Import missing entities or fix source mapping.");
                continue;
            }

            var exists = await context.Db.Set<ProductCategory>()
                .AnyAsync(x => x.ProductId == productId && x.CategoryId == categoryId, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                match++;
            }
            else
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "ProductCategory", productSourceId,
                    $"{productSourceId}->{categorySourceId}",
                    "Mapped product-category pair not found in Commerce.",
                    "Re-import product category mappings.");
            }
        }

        return new RelationshipCheckResult(table.Rows.Count, match, missing);
    }

    private static async Task<RelationshipCheckResult> CheckProductMediaMappingsAsync(
        ReconciliationContext context,
        string checkName,
        CancellationToken cancellationToken)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.ProductMediaMapping);
        if (table is null)
        {
            return default;
        }

        var match = 0;
        var missing = 0;

        foreach (var row in table.Rows)
        {
            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            var mediaSourceId = SmartstoreRowReader.GetInt(row, "MediaFileId");

            if (!context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out var productId) ||
                !context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.MediaAsset, mediaSourceId, out var mediaId))
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "ProductMedia", mediaSourceId,
                    $"{productSourceId}->{mediaSourceId}",
                    "Product-media mapping references unmapped product or media.",
                    "Import missing media/product or copy binary files to normalized storage keys.");
                continue;
            }

            var exists = await context.Db.Set<ProductMedia>()
                .AnyAsync(x => x.ProductId == productId && x.MediaAssetId == mediaId, cancellationToken)
                .ConfigureAwait(false);

            if (exists)
            {
                match++;
            }
            else
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "ProductMedia", mediaSourceId,
                    $"{productSourceId}->{mediaSourceId}",
                    "Mapped product-media link not found in Commerce.",
                    "Re-import media mappings after media assets exist.");
            }
        }

        return new RelationshipCheckResult(table.Rows.Count, match, missing);
    }

    private static RelationshipCheckResult CheckOrderCustomerRefs(
        ReconciliationContext context,
        string checkName)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Order);
        if (table is null)
        {
            return default;
        }

        var match = 0;
        var missing = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                continue;
            }

            var customerSourceId = SmartstoreRowReader.GetInt(row, "CustomerId");
            if (customerSourceId <= 0)
            {
                match++;
                continue;
            }

            if (context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Customer, customerSourceId, out _))
            {
                match++;
            }
            else if (context.FindIssue(SmartstoreImportEntityTypes.Order, sourceId) is { Code: "customer_ref_missing" })
            {
                missing++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing,
                    SmartstoreImportEntityTypes.Order, sourceId, SmartstoreRowReader.GetString(row, "OrderNumber"),
                    $"Order references missing customer {customerSourceId}; imported with guest fallback.",
                    "Create customer and update order association if required.");
            }
            else
            {
                missing++;
            }
        }

        return new RelationshipCheckResult(table.Rows.Count, match, missing);
    }

    private static RelationshipCheckResult CheckOrderItemProductRefs(
        ReconciliationContext context,
        string checkName)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.OrderItem);
        if (table is null)
        {
            return default;
        }

        var match = 0;
        var missing = 0;

        foreach (var row in table.Rows)
        {
            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            if (context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out _))
            {
                match++;
            }
            else
            {
                missing++;
                SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId);
                context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "OrderItem", sourceId,
                    SmartstoreRowReader.GetString(row, "Sku"),
                    $"Order item references missing product {productSourceId}.",
                    "Import product or remove line item from historical order.");
            }
        }

        return new RelationshipCheckResult(table.Rows.Count, match, missing);
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckLocalizationAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        const string checkName = "Localization";
        var propertyTable = context.DataSet.GetTable(SmartstoreImportTableNames.LocalizedProperty);
        var resourceTable = context.DataSet.GetTable(SmartstoreImportTableNames.LocaleStringResource);

        if (propertyTable is null && resourceTable is null)
        {
            return Summary(checkName, "Localization", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "No localization tables present in export.");
        }

        var match = 0;
        var missing = 0;
        var notApplicable = 0;

        if (propertyTable is not null)
        {
            foreach (var row in propertyTable.Rows)
            {
                if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
                {
                    continue;
                }

                var localeKeyGroup = SmartstoreRowReader.GetString(row, "LocaleKeyGroup") ?? "Unknown";
                var entitySourceId = SmartstoreRowReader.GetInt(row, "EntityId");
                var localeKey = SmartstoreRowReader.GetString(row, "LocaleKey") ?? "Value";
                var entityType = MapLocalizationEntityType(localeKeyGroup);

                if (!context.MappingIndex.TryGetTargetId(entityType, entitySourceId, out var entityId) ||
                    !context.MappingIndex.TryGetTargetId(SmartstoreImportEntityTypes.Language, SmartstoreRowReader.GetInt(row, "LanguageId"), out var languageId))
                {
                    missing++;
                    context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "LocalizedProperty", sourceId,
                        $"{localeKeyGroup}:{localeKey}",
                        "Localized property references unmapped entity or language.",
                        "Import parent entity and language before localization.");
                    continue;
                }

                var exists = await context.Db.Set<EntityTranslation>()
                    .AnyAsync(x => x.EntityType == entityType && x.EntityId == entityId && x.LanguageId == languageId && x.Property == localeKey, cancellationToken)
                    .ConfigureAwait(false);

                if (exists)
                {
                    match++;
                }
                else
                {
                    missing++;
                    context.AddDiscrepancy(checkName, ReconciliationClassification.Missing, "LocalizedProperty", sourceId,
                        $"{localeKeyGroup}:{localeKey}",
                        "Localized property not found in EntityTranslation.",
                        "Re-run localization importer for this entity.");
                }
            }
        }

        if (resourceTable is not null)
        {
            foreach (var row in resourceTable.Rows)
            {
                SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId);
                notApplicable++;
                context.AddDiscrepancy(checkName, ReconciliationClassification.NotApplicable, "LocaleStringResource", sourceId,
                    SmartstoreRowReader.GetString(row, "ResourceName"),
                    "Locale string resources use Commerce framework resource files, not direct SQL import.",
                    "Map critical resources to Commerce localization JSON/resx manually.");
            }
        }

        var sourceCount = (propertyTable?.Rows.Count ?? 0) + (resourceTable?.Rows.Count ?? 0);
        var targetCount = await context.Db.Set<EntityTranslation>().CountAsync(cancellationToken).ConfigureAwait(false);
        var expectedCount = propertyTable?.Rows.Count ?? 0;
        var overall = OverallClassification(match, missing, 0, 0, 0, notApplicable, expectedCount);

        return Summary(checkName, "Localization", overall, sourceCount, targetCount, expectedCount,
            match, missing, 0, 0, 0, notApplicable,
            $"Localization: {match}/{expectedCount} LocalizedProperty rows verified; {notApplicable} locale resources not applicable.");
    }

    private static async Task<SmartstoreReconciliationCheckSummary> CheckSeoUrlsAsync(
        ReconciliationContext context,
        CancellationToken cancellationToken)
    {
        const string checkName = "SeoUrls";
        return await CheckEntityCountsAsync(context, SmartstoreImportEntityTypes.UrlRecord,
            SmartstoreImportTableNames.UrlRecord, checkName, "SEO", cancellationToken).ConfigureAwait(false);
    }

    private static SmartstoreReconciliationCheckSummary CheckManufacturer(ReconciliationContext context)
    {
        const string checkName = "Manufacturers";
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Manufacturer);
        if (table is null)
        {
            return Summary(checkName, "Catalog", ReconciliationClassification.NotApplicable, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "Manufacturer table not present in export.");
        }

        foreach (var row in table.Rows)
        {
            SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId);
            context.AddDiscrepancy(checkName, ReconciliationClassification.NotApplicable,
                SmartstoreImportEntityTypes.Manufacturer, sourceId, SmartstoreRowReader.GetString(row, "Name"),
                "Commerce has no Manufacturer entity; Smartstore rows are reported but not imported.",
                "Model manufacturers as categories, attributes, or a future Manufacturer module.");
        }

        return Summary(checkName, "Catalog", ReconciliationClassification.NotApplicable, table.Rows.Count, 0, 0,
            0, 0, 0, 0, 0, table.Rows.Count,
            $"{table.Rows.Count} manufacturer rows documented as not applicable.");
    }

    private static SmartstoreReconciliationCheckSummary CheckDuplicateMappings(ReconciliationContext context)
    {
        const string checkName = "DuplicateMappings";
        var duplicateGroups = context.ImportIssues
            .Where(x => x.Code.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var count = duplicateGroups.Count;
        if (count == 0)
        {
            return Summary(checkName, "Integrity", ReconciliationClassification.Match, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                "No duplicate mapping issues recorded.");
        }

        foreach (var issue in duplicateGroups)
        {
            context.AddDiscrepancy(checkName, ReconciliationClassification.Duplicate, issue.EntityType, issue.SourceId, null,
                issue.Message, "Deduplicate ImportIdMapping and re-run validation.");
        }

        return Summary(checkName, "Integrity", ReconciliationClassification.Duplicate, count, 0, 0,
            0, 0, count, 0, 0, 0,
            $"{count} duplicate mapping issues found in import log.");
    }

    private static async Task<bool> TargetEntityExistsAsync(
        CommerceDbContext db,
        string entityType,
        int targetId,
        CancellationToken cancellationToken) =>
        entityType switch
        {
            SmartstoreImportEntityTypes.Store => await db.Set<StoreEntity>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Language => await db.Set<Language>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Currency => await db.Set<StoreCurrency>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Setting => await db.Set<Setting>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Customer => await db.Set<Customer>().AnyAsync(x => x.Id == targetId, cancellationToken),
            "Category" => await db.Set<Category>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Product => await db.Set<Product>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.ProductOffer => await db.Set<ProductOffer>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.MediaAsset => await db.Set<MediaAsset>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Order => await db.Set<OrderEntity>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.ProductReview => await db.Set<ProductReview>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Topic => await db.Set<Topic>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.UrlRecord => await db.Set<UrlRecord>().AnyAsync(x => x.Id == targetId, cancellationToken),
            SmartstoreImportEntityTypes.Discount => await db.Set<Discount>().AnyAsync(x => x.Id == targetId, cancellationToken),
            _ => false
        };

    private static string MapLocalizationEntityType(string localeKeyGroup) => localeKeyGroup switch
    {
        "Product" => SmartstoreImportEntityTypes.Product,
        "Category" => "Category",
        "Topic" => SmartstoreImportEntityTypes.Topic,
        _ => localeKeyGroup
    };

    private static SmartstoreReconciliationCheckSummary Aggregate(
        string checkName,
        string category,
        IReadOnlyList<SmartstoreReconciliationCheckSummary> parts)
    {
        var overall = Worst(parts.Select(x => x.OverallClassification));
        return Summary(
            checkName,
            category,
            overall,
            parts.Sum(x => x.SourceCount),
            parts.Sum(x => x.TargetCount),
            parts.Sum(x => x.ExpectedCount),
            parts.Sum(x => x.MatchCount),
            parts.Sum(x => x.MissingCount),
            parts.Sum(x => x.DuplicateCount),
            parts.Sum(x => x.TransformedCount),
            parts.Sum(x => x.InvalidCount),
            parts.Sum(x => x.NotApplicableCount),
            string.Join(" ", parts.Select(x => x.Summary)));
    }

    private static ReconciliationClassification OverallClassification(
        int match,
        int missing,
        int duplicate,
        int transformed,
        int invalid,
        int notApplicable,
        int sourceCount)
    {
        if (invalid > 0)
        {
            return ReconciliationClassification.Invalid;
        }

        if (missing > 0)
        {
            return ReconciliationClassification.Missing;
        }

        if (duplicate > 0)
        {
            return ReconciliationClassification.Duplicate;
        }

        if (transformed > 0)
        {
            return ReconciliationClassification.Transformed;
        }

        if (notApplicable > 0 && match == 0 && missing == 0 && duplicate == 0 && invalid == 0)
        {
            return ReconciliationClassification.NotApplicable;
        }

        if (match >= sourceCount && missing == 0 && duplicate == 0 && invalid == 0)
        {
            return ReconciliationClassification.Match;
        }

        return match > 0 ? ReconciliationClassification.Transformed : ReconciliationClassification.Missing;
    }

    private static ReconciliationClassification Worst(IEnumerable<ReconciliationClassification> values)
    {
        ReconciliationClassification[] severityOrder =
        [
            ReconciliationClassification.Invalid,
            ReconciliationClassification.Missing,
            ReconciliationClassification.Duplicate,
            ReconciliationClassification.Transformed,
            ReconciliationClassification.NotApplicable,
            ReconciliationClassification.Match
        ];

        var set = values.ToHashSet();
        foreach (var classification in severityOrder)
        {
            if (set.Contains(classification))
            {
                return classification;
            }
        }

        return ReconciliationClassification.Match;
    }

    private static SmartstoreReconciliationCheckSummary Summary(
        string checkName,
        string category,
        ReconciliationClassification overall,
        int sourceCount,
        int targetCount,
        int expectedCount,
        int match,
        int missing,
        int duplicate,
        int transformed,
        int invalid,
        int notApplicable,
        string summary) =>
        new(checkName, category, overall, sourceCount, targetCount, expectedCount,
            match, missing, duplicate, transformed, invalid, notApplicable, summary);
}
