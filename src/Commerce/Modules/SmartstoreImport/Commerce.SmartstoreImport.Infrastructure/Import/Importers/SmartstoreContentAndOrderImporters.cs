using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Data.Db;
using Commerce.Media.Domain.Entities;
using Commerce.Media.Domain.Enums;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Orders.Domain.ValueObjects;
using Commerce.Pricing.Domain.Entities;
using Commerce.Pricing.Domain.Enums;
using Commerce.Reviews.Domain.Entities;
using Commerce.Reviews.Domain.Enums;
using Commerce.Seo.Domain.Entities;
using Commerce.SmartstoreImport.Application.Abstractions;
using Commerce.SmartstoreImport.Contracts;
using Commerce.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OrderEntity = Commerce.Orders.Domain.Entities.Order;
using TopicEntity = Commerce.Cms.Domain.Entities.Topic;

namespace Commerce.SmartstoreImport.Infrastructure.Import.Importers;

internal sealed class SmartstoreMediaImporter : SmartstoreEntityImporterBase
{
    public override int Order => 80;
    public override string EntityType => SmartstoreImportEntityTypes.MediaAsset;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.MediaFile];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.MediaFile);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.MediaFile, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;
        var defaultStoreId = await db.Set<Commerce.Store.Domain.Entities.Store>().OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Error(EntityType, null, "missing_id", "MediaFile row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var fileName = SmartstoreRowReader.GetString(row, "Name") ?? SmartstoreRowReader.GetString(row, "FileName") ?? $"media-{sourceId}";
            var extension = SmartstoreRowReader.GetString(row, "Extension") ?? Path.GetExtension(fileName).TrimStart('.');
            var mimeType = SmartstoreRowReader.GetString(row, "MimeType") ?? "application/octet-stream";
            var size = SmartstoreRowReader.GetInt(row, "Size");
            var storagePath = SmartstoreRowReader.GetString(row, "Path") ?? SmartstoreRowReader.GetString(row, "StoragePath");

            if (string.IsNullOrWhiteSpace(storagePath))
            {
                context.Issues.Warning(EntityType, sourceId, "missing_media", "Media file binary path missing; placeholder asset created.", fileName);
                storagePath = $"smartstore-import/missing/{sourceId}/{fileName}";
                warnings++;
            }
            else
            {
                storagePath = NormalizeStorageKey(storagePath, sourceId, fileName);
            }

            if (defaultStoreId <= 0)
            {
                context.Issues.Error(EntityType, sourceId, "store_missing", "Cannot import media without a target store.");
                errors++;
                continue;
            }

            try
            {
                var asset = MediaAsset.Create(
                    defaultStoreId,
                    fileName,
                    mimeType,
                    extension,
                    size,
                    storagePath,
                    "smartstore-import",
                    MediaType.Image,
                    contentHash: SmartstoreRowReader.GetString(row, "Hash"),
                    width: SmartstoreRowReader.TryGetInt(row, "Width", out var width) ? width : null);

                db.Set<MediaAsset>().Add(asset);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, asset.Id, fileName);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        await ImportProductMediaMappingsAsync(context, db, context.Issues, cancellationToken).ConfigureAwait(false);
        return Summary(EntityType, SmartstoreImportTableNames.MediaFile, table.Rows.Count, imported, skipped, errors, warnings, true);
    }

    private static string NormalizeStorageKey(string rawPath, int sourceId, string fileName)
    {
        var normalized = rawPath.Trim().Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return $"smartstore-import/{sourceId}/{fileName}";
        }

        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            return $"smartstore-import/{sourceId}/{fileName}";
        }

        return normalized.StartsWith("smartstore-import/", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"smartstore-import/{normalized}";
    }

    private static async Task ImportProductMediaMappingsAsync(
        SmartstoreImportContext context,
        CommerceDbContext db,
        IImportIssueReporter issues,
        CancellationToken cancellationToken)
    {
        var mappingTable = context.DataSet.GetTable(SmartstoreImportTableNames.ProductMediaMapping);
        if (mappingTable is null)
        {
            return;
        }

        foreach (var row in mappingTable.Rows)
        {
            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            var mediaSourceId = SmartstoreRowReader.GetInt(row, "MediaFileId");
            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out var productId))
            {
                issues.Warning(SmartstoreImportEntityTypes.MediaAsset, mediaSourceId, "product_ref_missing", $"Product media mapping references missing product {productSourceId}.");
                continue;
            }

            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.MediaAsset, mediaSourceId, out var mediaId))
            {
                issues.Warning(SmartstoreImportEntityTypes.MediaAsset, mediaSourceId, "media_ref_missing", $"Product media mapping references missing media {mediaSourceId}.");
                continue;
            }

            var displayOrder = SmartstoreRowReader.GetInt(row, "DisplayOrder");
            db.Set<ProductMedia>().Add(ProductMedia.Create(productId, mediaId, ProductMediaRole.Primary, displayOrder));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class SmartstoreOrderImporter : SmartstoreEntityImporterBase
{
    public override int Order => 90;
    public override string EntityType => SmartstoreImportEntityTypes.Order;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Order, SmartstoreImportTableNames.OrderItem];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var orderTable = context.DataSet.GetTable(SmartstoreImportTableNames.Order);
        if (orderTable is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Order, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;
        var itemTable = context.DataSet.GetTable(SmartstoreImportTableNames.OrderItem);

        foreach (var row in orderTable.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Error(EntityType, null, "missing_id", "Order row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var storeSourceId = SmartstoreRowReader.GetInt(row, "StoreId", 1);
            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Store, storeSourceId, out var storeId))
            {
                storeId = await db.Set<Commerce.Store.Domain.Entities.Store>().Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (storeId <= 0)
                {
                    context.Issues.Error(EntityType, sourceId, "store_missing", "Order references missing store.");
                    errors++;
                    continue;
                }

                context.Issues.Warning(EntityType, sourceId, "store_ref_missing", $"Order store {storeSourceId} mapped to default store {storeId}.");
                warnings++;
            }

            var customerSourceId = SmartstoreRowReader.GetInt(row, "CustomerId");
            int? customerId = null;
            if (customerSourceId > 0)
            {
                if (context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Customer, customerSourceId, out var mappedCustomer))
                {
                    customerId = mappedCustomer;
                }
                else if (context.Options.ValidateRelationships)
                {
                    context.Issues.Warning(EntityType, sourceId, "customer_ref_missing", $"Order references missing customer {customerSourceId}; imported as guest-linked order.");
                    warnings++;
                }
            }

            var currencyCode = SmartstoreRowReader.GetString(row, "CustomerCurrencyCode") ?? "USD";
            var currencyId = await db.Set<StoreCurrency>().Where(x => x.Code == currencyCode).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (currencyId <= 0)
            {
                currencyId = await db.Set<StoreCurrency>().OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                currencyCode = await db.Set<StoreCurrency>().Where(x => x.Id == currencyId).Select(x => x.Code).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? "USD";
                context.Issues.Warning(EntityType, sourceId, "currency_fallback", "Order currency mapped to default store currency.");
                warnings++;
            }

            var items = BuildOrderItems(context, itemTable, sourceId, currencyCode ?? "USD", context.Issues);
            if (items.Count == 0)
            {
                context.Issues.Error(EntityType, sourceId, "no_items", "Order has no importable line items.");
                errors++;
                continue;
            }

            var checkoutId = context.SyntheticCheckoutBase + sourceId;
            var cartId = context.SyntheticCartBase + sourceId;
            var orderNumber = SmartstoreRowReader.GetString(row, "OrderNumber") ?? SmartstoreRowReader.GetString(row, "CustomOrderNumber") ?? $"SS-{sourceId}";

            try
            {
                var order = OrderEntity.CreateFromCheckout(
                    orderNumber,
                    storeId,
                    checkoutId,
                    cartId,
                    customerId,
                    guestEmail: customerId.HasValue ? null : SmartstoreRowReader.GetString(row, "BillingEmail"),
                    customerEmail: SmartstoreRowReader.GetString(row, "BillingEmail"),
                    customerDisplayName: SmartstoreRowReader.GetString(row, "CustomerTaxDisplayTypeId") is null ? null : SmartstoreRowReader.GetString(row, "CustomerOrderComment"),
                    guestAccessToken: null,
                    currencyId,
                    currencyCode!,
                    requiresShipping: SmartstoreRowReader.GetInt(row, "ShippingStatusId") > 0,
                    billingAddress: null,
                    shippingAddress: null,
                    selectedShippingMethodId: SmartstoreRowReader.GetString(row, "ShippingMethod"),
                    selectedShippingProviderSystemName: SmartstoreRowReader.GetString(row, "ShippingRateComputationMethodSystemName"),
                    selectedPaymentMethodId: SmartstoreRowReader.GetString(row, "PaymentMethodSystemName"),
                    selectedPaymentMethodSystemName: SmartstoreRowReader.GetString(row, "PaymentMethodSystemName"),
                    subtotal: SmartstoreRowReader.GetDecimal(row, "OrderSubtotalInclTax"),
                    discountTotal: SmartstoreRowReader.GetDecimal(row, "OrderDiscount"),
                    shippingTotal: SmartstoreRowReader.GetDecimal(row, "OrderShippingInclTax"),
                    taxTotal: SmartstoreRowReader.GetDecimal(row, "OrderTax"),
                    grandTotal: SmartstoreRowReader.GetDecimal(row, "OrderTotal"),
                    items,
                    storeCreditApplied: SmartstoreRowReader.GetDecimal(row, "CreditBalance"));

                ApplyLegacyStatuses(order, row, context, sourceId);
                db.Set<OrderEntity>().Add(order);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                foreach (var item in items)
                {
                    db.Set<OrderItem>().Add(OrderItem.Create(
                        order.Id,
                        item.CheckoutId,
                        item.CartItemId,
                        item.OfferId,
                        item.ProductId,
                        item.VariantId,
                        item.ProductName,
                        item.VariantName,
                        item.Sku,
                        item.Quantity,
                        item.UnitPrice,
                        item.LineSubtotal,
                        item.DiscountTotal,
                        item.TaxTotal,
                        item.LineTotal,
                        item.CurrencyCode,
                        item.PrimaryImageUrl,
                        item.PrimaryImageThumbnailUrl));
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, order.Id, orderNumber);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Order, orderTable.Rows.Count, imported, skipped, errors, warnings, true);
    }

    private static List<OrderItem> BuildOrderItems(
        SmartstoreImportContext context,
        SmartstoreParsedTable? itemTable,
        int orderSourceId,
        string currencyCode,
        IImportIssueReporter issues)
    {
        var items = new List<OrderItem>();
        if (itemTable is null)
        {
            return items;
        }

        foreach (var row in itemTable.Rows.Where(r => SmartstoreRowReader.GetInt(r, "OrderId") == orderSourceId))
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var itemSourceId))
            {
                issues.Error(SmartstoreImportEntityTypes.OrderItem, null, "missing_id", "Order item row is missing Id.");
                continue;
            }

            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out var productId))
            {
                issues.Warning(SmartstoreImportEntityTypes.OrderItem, itemSourceId, "product_ref_missing", $"Order item references missing product {productSourceId}.");
                continue;
            }

            context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.ProductOffer, productSourceId, out var offerId);
            if (offerId <= 0)
            {
                offerId = 1;
            }

            var quantity = SmartstoreRowReader.GetInt(row, "Quantity", 1);
            var unitPrice = SmartstoreRowReader.GetDecimal(row, "UnitPriceInclTax");
            if (unitPrice <= 0)
            {
                unitPrice = SmartstoreRowReader.GetDecimal(row, "PriceInclTax");
            }

            var lineTotal = SmartstoreRowReader.GetDecimal(row, "PriceInclTax", unitPrice * quantity);
            items.Add(OrderItem.Create(
                orderId: 0,
                checkoutId: context.SyntheticCheckoutBase + orderSourceId,
                cartItemId: context.SyntheticCartItemBase + itemSourceId,
                offerId,
                productId,
                variantId: null,
                SmartstoreRowReader.GetString(row, "ProductName") ?? $"Product-{productSourceId}",
                variantName: null,
                SmartstoreRowReader.GetString(row, "Sku") ?? $"SKU-{productSourceId}",
                quantity,
                unitPrice,
                lineTotal,
                discountTotal: 0m,
                taxTotal: 0m,
                lineTotal,
                currencyCode));
        }

        return items;
    }

    private static void ApplyLegacyStatuses(
        OrderEntity order,
        SmartstoreParsedRow row,
        SmartstoreImportContext context,
        int sourceId)
    {
        var orderStatusId = SmartstoreRowReader.GetInt(row, "OrderStatusId");
        var paymentStatusId = SmartstoreRowReader.GetInt(row, "PaymentStatusId");

        try
        {
            switch (orderStatusId)
            {
                case >= 30:
                    order.Complete("Imported completed order.");
                    break;
                case 20:
                    order.Confirm("Imported processing order.");
                    order.MarkProcessing("Imported processing order.");
                    break;
                case 10:
                    order.Confirm("Imported pending order.");
                    break;
            }

            if (paymentStatusId >= 30)
            {
                order.MarkPaymentPaid("Imported paid order.");
            }
            else if (paymentStatusId is 40 or 50)
            {
                order.MarkPaymentFailed("Imported failed/refunded payment status.");
                context.Issues.Warning(SmartstoreImportEntityTypes.Order, sourceId, "payment_status_mapping", "Legacy payment status mapped to failed.");
            }
        }
        catch (Exception ex)
        {
            context.Issues.Warning(SmartstoreImportEntityTypes.Order, sourceId, "status_mapping", "Could not map legacy order/payment status.", ex.Message);
        }
    }
}

internal sealed class SmartstoreTopicImporter : SmartstoreEntityImporterBase
{
    public override int Order => 100;
    public override string EntityType => SmartstoreImportEntityTypes.Topic;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Topic];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Topic);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Topic, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var defaultStoreId = await db.Set<Commerce.Store.Domain.Entities.Store>().OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            if (defaultStoreId <= 0)
            {
                context.Issues.Error(EntityType, sourceId, "store_missing", "Cannot import topic without store.");
                errors++;
                continue;
            }

            var systemName = SmartstoreRowReader.GetString(row, "SystemName") ?? $"topic-{sourceId}";
            var published = SmartstoreRowReader.GetBool(row, "IsPublished", SmartstoreRowReader.GetBool(row, "Published", true));

            try
            {
                var topic = TopicEntity.Create(defaultStoreId, systemName, published, null, null);
                db.Set<TopicEntity>().Add(topic);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, topic.Id, systemName);

                var title = SmartstoreRowReader.GetString(row, "Title") ?? systemName;
                var body = SmartstoreRowReader.GetString(row, "Body") ?? string.Empty;
                var languageId = await db.Set<Language>().OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (languageId > 0)
                {
                    topic.AddLocalization(languageId, title, body, title, SmartstoreRowReader.GetString(row, "MetaDescription"));
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Topic, table.Rows.Count, imported, skipped, errors, 0, true);
    }
}

internal sealed class SmartstoreUrlRecordImporter : SmartstoreEntityImporterBase
{
    public override int Order => 110;
    public override string EntityType => SmartstoreImportEntityTypes.UrlRecord;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.UrlRecord];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.UrlRecord);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.UrlRecord, 0, 0, 0, 0, 0, false);
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
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var entityName = SmartstoreRowReader.GetString(row, "EntityName") ?? "Unknown";
            var entitySourceId = SmartstoreRowReader.GetInt(row, "EntityId");
            var slug = SmartstoreRowReader.GetString(row, "Slug");
            if (string.IsNullOrWhiteSpace(slug))
            {
                context.Issues.Error(EntityType, sourceId, "missing_slug", "UrlRecord row missing slug.");
                errors++;
                continue;
            }

            if (!TryMapEntityId(context, entityName, entitySourceId, out var targetEntityId))
            {
                context.Issues.Warning(EntityType, sourceId, "entity_ref_missing", $"UrlRecord references unmapped {entityName} {entitySourceId}.");
                warnings++;
                continue;
            }

            var languageSourceId = SmartstoreRowReader.GetInt(row, "LanguageId");
            int? languageId = null;
            if (languageSourceId > 0 && context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Language, languageSourceId, out var mappedLanguage))
            {
                languageId = mappedLanguage;
            }

            var storeSourceId = SmartstoreRowReader.GetInt(row, "StoreId");
            int? storeId = null;
            if (storeSourceId > 0 && context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Store, storeSourceId, out var mappedStore))
            {
                storeId = mappedStore;
            }

            var isActive = SmartstoreRowReader.GetBool(row, "IsActive", true);
            try
            {
                var record = UrlRecord.Create(entityName, targetEntityId, slug, languageId, storeId, isActive);
                db.Set<UrlRecord>().Add(record);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, record.Id, slug);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.UrlRecord, table.Rows.Count, imported, skipped, errors, warnings, true);
    }

    private static bool TryMapEntityId(SmartstoreImportContext context, string entityName, int sourceId, out int targetId)
    {
        var entityType = entityName switch
        {
            "Product" => SmartstoreImportEntityTypes.Product,
            "Category" => "Category",
            "Topic" => SmartstoreImportEntityTypes.Topic,
            _ => entityName
        };

        return context.IdRegistry.TryGetTargetId(entityType, sourceId, out targetId);
    }
}

internal sealed class SmartstoreLocalizationImporter : SmartstoreEntityImporterBase
{
    public override int Order => 120;
    public override string EntityType => SmartstoreImportEntityTypes.Localization;
    public override IReadOnlyList<string> SourceTables =>
        [SmartstoreImportTableNames.LocaleStringResource, SmartstoreImportTableNames.LocalizedProperty];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var localeTable = context.DataSet.GetTable(SmartstoreImportTableNames.LocaleStringResource);
        var propertyTable = context.DataSet.GetTable(SmartstoreImportTableNames.LocalizedProperty);
        var sourceCount = (localeTable?.Rows.Count ?? 0) + (propertyTable?.Rows.Count ?? 0);
        if (sourceCount == 0)
        {
            return Summary(EntityType, "LocaleStringResource+LocalizedProperty", 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;

        if (propertyTable is not null)
        {
            foreach (var row in propertyTable.Rows)
            {
                if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
                {
                    errors++;
                    continue;
                }

                var entitySourceId = SmartstoreRowReader.GetInt(row, "EntityId");
                var localeKeyGroup = SmartstoreRowReader.GetString(row, "LocaleKeyGroup") ?? "Unknown";
                if (!TryMapEntityId(context, localeKeyGroup, entitySourceId, out var entityId))
                {
                    context.Issues.Warning(EntityType, sourceId, "entity_ref_missing", $"LocalizedProperty references unmapped {localeKeyGroup} {entitySourceId}.");
                    warnings++;
                    continue;
                }

                var languageSourceId = SmartstoreRowReader.GetInt(row, "LanguageId");
                if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Language, languageSourceId, out var languageId))
                {
                    context.Issues.Warning(EntityType, sourceId, "language_ref_missing", $"LocalizedProperty references missing language {languageSourceId}.");
                    warnings++;
                    continue;
                }

                var property = SmartstoreRowReader.GetString(row, "LocaleKey") ?? "Name";
                var value = SmartstoreRowReader.GetString(row, "LocaleValue") ?? string.Empty;
                db.Set<EntityTranslation>().Add(EntityTranslation.Create(localeKeyGroup, entityId, languageId, property, value));
                imported++;
            }
        }

        if (localeTable is not null)
        {
            foreach (var row in localeTable.Rows)
            {
                var resourceName = SmartstoreRowReader.GetString(row, "ResourceName");
                if (string.IsNullOrWhiteSpace(resourceName))
                {
                    skipped++;
                    continue;
                }

                context.Issues.Warning(
                    EntityType,
                    SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId) ? sourceId : null,
                    "locale_string_resource",
                    "LocaleStringResource entries are preserved in import report; map to Commerce localization store separately.",
                    resourceName);
                warnings++;
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Summary(EntityType, "LocaleStringResource+LocalizedProperty", sourceCount, imported, skipped, errors, warnings, true);
    }

    private static bool TryMapEntityId(SmartstoreImportContext context, string entityName, int sourceId, out int targetId)
    {
        var entityType = entityName switch
        {
            "Product" => SmartstoreImportEntityTypes.Product,
            "Category" => "Category",
            "Topic" => SmartstoreImportEntityTypes.Topic,
            _ => entityName
        };

        return context.IdRegistry.TryGetTargetId(entityType, sourceId, out targetId);
    }
}

internal sealed class SmartstoreDiscountImporter : SmartstoreEntityImporterBase
{
    public override int Order => 85;
    public override string EntityType => SmartstoreImportEntityTypes.Discount;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Discount];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Discount);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Discount, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var name = SmartstoreRowReader.GetString(row, "Name") ?? $"Discount-{sourceId}";
            var systemName = SmartstoreRowReader.ToSystemName(name, 128);
            var discountTypeId = SmartstoreRowReader.GetInt(row, "DiscountTypeId");
            var discountType = discountTypeId == 2 ? DiscountType.FixedAmount : DiscountType.Percentage;
            var value = SmartstoreRowReader.GetDecimal(row, "DiscountPercentage");
            if (value <= 0)
            {
                value = SmartstoreRowReader.GetDecimal(row, "DiscountAmount");
            }

            try
            {
                var discount = Discount.Create(
                    name,
                    systemName,
                    SmartstoreRowReader.GetString(row, "AdminComment"),
                    discountType,
                    value,
                    currencyCode: null,
                    priority: 0,
                    isActive: SmartstoreRowReader.GetBool(row, "IsActive", true),
                    startsAtUtc: null,
                    endsAtUtc: null,
                    storeId: null,
                    StackingMode.NonStackable,
                    maximumDiscountAmount: null,
                    minimumCartSubtotal: null,
                    minimumQuantity: null,
                    CustomerEligibility.All,
                    specificCustomerId: null,
                    customerGroupId: null,
                    DiscountApplicationScope.Cart,
                    targets: []);

                db.Set<Discount>().Add(discount);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, discount.Id, systemName);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Discount, table.Rows.Count, imported, skipped, errors, 0, true);
    }
}

internal sealed class SmartstoreProductReviewImporter : SmartstoreEntityImporterBase
{
    public override int Order => 95;
    public override string EntityType => SmartstoreImportEntityTypes.ProductReview;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.ProductReview];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.ProductReview);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.ProductReview, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;
        var defaultStoreId = await db.Set<Commerce.Store.Domain.Entities.Store>().OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            var customerSourceId = SmartstoreRowReader.GetInt(row, "CustomerId");
            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out var productId) ||
                !context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Customer, customerSourceId, out var customerId))
            {
                context.Issues.Warning(EntityType, sourceId, "reference_missing", "Review references missing product or customer.");
                warnings++;
                continue;
            }

            if (defaultStoreId <= 0)
            {
                errors++;
                continue;
            }

            var rating = SmartstoreRowReader.GetInt(row, "Rating", 5);
            if (rating is < 1 or > 5)
            {
                context.Issues.Warning(EntityType, sourceId, "invalid_rating", "Review rating out of range; clamped to 1-5.");
                rating = Math.Clamp(rating, 1, 5);
                warnings++;
            }

            try
            {
                var review = ProductReview.Create(
                    productId,
                    customerId,
                    defaultStoreId,
                    rating,
                    SmartstoreRowReader.GetString(row, "Title") ?? "Imported review",
                    SmartstoreRowReader.GetString(row, "ReviewText") ?? SmartstoreRowReader.GetString(row, "Comment") ?? string.Empty,
                    SmartstoreRowReader.GetBool(row, "IsVerifiedPurchase"),
                    DateTime.UtcNow);

                if (SmartstoreRowReader.GetBool(row, "IsApproved", true))
                {
                    review.Approve(DateTime.UtcNow);
                }

                db.Set<ProductReview>().Add(review);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, review.Id);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.ProductReview, table.Rows.Count, imported, skipped, errors, warnings, true);
    }
}
