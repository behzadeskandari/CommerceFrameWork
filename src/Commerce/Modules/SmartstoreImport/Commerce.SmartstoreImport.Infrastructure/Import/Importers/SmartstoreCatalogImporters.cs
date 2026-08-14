using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Domain.ValueObjects;
using Commerce.SmartstoreImport.Application.Abstractions;
using Commerce.SmartstoreImport.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Commerce.SmartstoreImport.Infrastructure.Import.Importers;

internal sealed class SmartstoreCategoryImporter : SmartstoreEntityImporterBase
{
    public override int Order => 60;
    public override string EntityType => "Category";
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Category];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Category);
        if (table is null)
        {
            return Summary("Category", SmartstoreImportTableNames.Category, 0, 0, 0, 0, 0, false);
        }

        var db = GetDb(context);
        var imported = 0;
        var skipped = 0;
        var errors = 0;
        var warnings = 0;

        foreach (var row in table.Rows.OrderBy(r => SmartstoreRowReader.GetInt(r, "ParentCategoryId")))
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Error("Category", null, "missing_id", "Category row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId("Category", sourceId, out _))
            {
                skipped++;
                continue;
            }

            var name = SmartstoreRowReader.GetString(row, "Name") ?? $"Category-{sourceId}";
            var description = SmartstoreRowReader.GetString(row, "Description");
            var published = SmartstoreRowReader.GetBool(row, "Published", true);
            var displayOrder = SmartstoreRowReader.GetInt(row, "DisplayOrder");
            var parentSourceId = SmartstoreRowReader.GetInt(row, "ParentCategoryId");
            int? parentCategoryId = null;

            if (parentSourceId > 0)
            {
                if (context.IdRegistry.TryGetTargetId("Category", parentSourceId, out var mappedParent))
                {
                    parentCategoryId = mappedParent;
                }
                else if (context.Options.ValidateRelationships)
                {
                    context.Issues.Warning("Category", sourceId, "parent_missing", $"Parent category {parentSourceId} not found; imported as root.");
                    warnings++;
                }
            }

            try
            {
                var category = Category.Create(
                    name,
                    parentCategoryId,
                    description,
                    Slug.Create(SmartstoreRowReader.ToSlug(name)),
                    published,
                    displayOrder);
                db.Set<Category>().Add(category);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register("Category", sourceId, category.Id, name);
                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error("Category", sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary("Category", SmartstoreImportTableNames.Category, table.Rows.Count, imported, skipped, errors, warnings, true);
    }
}

internal sealed class SmartstoreManufacturerImporter : SmartstoreEntityImporterBase
{
    public override int Order => 65;
    public override string EntityType => SmartstoreImportEntityTypes.Manufacturer;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Manufacturer];

    public override Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Manufacturer);
        if (table is null)
        {
            return Task.FromResult(Summary(EntityType, SmartstoreImportTableNames.Manufacturer, 0, 0, 0, 0, 0, false));
        }

        foreach (var row in table.Rows)
        {
            if (SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                context.Issues.Warning(
                    EntityType,
                    sourceId,
                    "unsupported_entity",
                    "Manufacturer records are present in Smartstore export but Commerce has no Manufacturer entity; rows were not discarded silently.",
                    SmartstoreRowReader.GetString(row, "Name"));
            }
        }

        return Task.FromResult(Summary(
            EntityType,
            SmartstoreImportTableNames.Manufacturer,
            table.Rows.Count,
            imported: 0,
            skipped: 0,
            errors: 0,
            warnings: table.Rows.Count,
            wasPresent: true));
    }
}

internal sealed class SmartstoreProductImporter : SmartstoreEntityImporterBase
{
    public override int Order => 70;
    public override string EntityType => SmartstoreImportEntityTypes.Product;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.Product];

    public override async Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.Product);
        if (table is null)
        {
            return Summary(EntityType, SmartstoreImportTableNames.Product, 0, 0, 0, 0, 0, false);
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
                context.Issues.Error(EntityType, null, "missing_id", "Product row is missing Id.", $"Line {row.SourceLineNumber}");
                errors++;
                continue;
            }

            if (context.IdRegistry.TryGetTargetId(EntityType, sourceId, out _))
            {
                skipped++;
                continue;
            }

            var name = SmartstoreRowReader.GetString(row, "Name") ?? $"Product-{sourceId}";
            var sku = SmartstoreRowReader.GetString(row, "Sku") ?? SmartstoreRowReader.GetString(row, "Gtin") ?? $"SKU-{sourceId}";
            var shortDescription = SmartstoreRowReader.GetString(row, "ShortDescription");
            var description = SmartstoreRowReader.GetString(row, "FullDescription") ?? SmartstoreRowReader.GetString(row, "Description");
            var published = SmartstoreRowReader.GetBool(row, "Published", true);
            var deleted = SmartstoreRowReader.GetBool(row, "Deleted");
            var displayOrder = SmartstoreRowReader.GetInt(row, "DisplayOrder");
            var productTypeId = SmartstoreRowReader.GetInt(row, "ProductTypeId");
            var productType = MapProductType(productTypeId);
            var weight = SmartstoreRowReader.GetDecimal(row, "Weight", 0m) * 1000m;

            try
            {
                var product = Product.Create(
                    name,
                    Sku.Create(sku),
                    productType,
                    shortDescription,
                    description,
                    Slug.Create(SmartstoreRowReader.ToSlug(name)),
                    published: published && !deleted,
                    displayOrder: displayOrder,
                    weightGrams: weight);

                if (deleted)
                {
                    product.SoftDelete();
                }

                db.Set<Product>().Add(product);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.IdRegistry.Register(EntityType, sourceId, product.Id, sku);

                await ImportProductOfferAsync(context, db, row, sourceId, product.Id, cancellationToken).ConfigureAwait(false);
                await ImportProductCategoryMappingsAsync(context, db, sourceId, product.Id, context.Issues, cancellationToken).ConfigureAwait(false);

                imported++;
            }
            catch (Exception ex)
            {
                context.Issues.Error(EntityType, sourceId, "create_failed", ex.Message, ex.ToString());
                errors++;
            }
        }

        return Summary(EntityType, SmartstoreImportTableNames.Product, table.Rows.Count, imported, skipped, errors, warnings, true);
    }

    private static ProductType MapProductType(int productTypeId) => productTypeId switch
    {
        5 => ProductType.Grouped,
        10 => ProductType.Bundle,
        20 => ProductType.Digital,
        25 => ProductType.Downloadable,
        30 => ProductType.Variant,
        _ => ProductType.Simple
    };

    private static async Task ImportProductOfferAsync(
        SmartstoreImportContext context,
        CommerceDbContext db,
        SmartstoreParsedRow row,
        int sourceProductId,
        int targetProductId,
        CancellationToken cancellationToken)
    {
        var storeId = await db.Set<Commerce.Store.Domain.Entities.Store>().OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (storeId <= 0)
        {
            return;
        }

        var currencyId = await db.Set<Commerce.Store.Domain.Entities.StoreCurrency>().OrderBy(x => x.DisplayOrder).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var currencyCode = await db.Set<Commerce.Store.Domain.Entities.StoreCurrency>().Where(x => x.Id == currencyId).Select(x => x.Code).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? "USD";
        var price = SmartstoreRowReader.GetDecimal(row, "Price", 0m);
        if (price <= 0)
        {
            return;
        }

        var offer = ProductOffer.Create(
            targetProductId,
            variantId: null,
            storeId,
            currencyId,
            currencyCode,
            Money.Create(price, Currency.FromCode(currencyCode)));

        db.Set<ProductOffer>().Add(offer);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        context.IdRegistry.Register(SmartstoreImportEntityTypes.ProductOffer, sourceProductId, offer.Id);
    }

    private static async Task ImportProductCategoryMappingsAsync(
        SmartstoreImportContext context,
        CommerceDbContext db,
        int sourceProductId,
        int targetProductId,
        IImportIssueReporter issues,
        CancellationToken cancellationToken)
    {
        var mappingTable = context.DataSet.GetTable(SmartstoreImportTableNames.ProductCategoryMapping);
        if (mappingTable is null)
        {
            return;
        }

        foreach (var row in mappingTable.Rows)
        {
            if (SmartstoreRowReader.GetInt(row, "ProductId") != sourceProductId)
            {
                continue;
            }

            var categorySourceId = SmartstoreRowReader.GetInt(row, "CategoryId");
            if (!context.IdRegistry.TryGetTargetId("Category", categorySourceId, out var categoryId))
            {
                issues.Warning(SmartstoreImportEntityTypes.Product, sourceProductId, "category_ref_missing", $"Product category mapping references missing category {categorySourceId}.");
                continue;
            }

            var displayOrder = SmartstoreRowReader.GetInt(row, "DisplayOrder");
            db.Set<ProductCategory>().Add(ProductCategory.Create(targetProductId, categoryId, displayOrder));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class SmartstoreProductVariantImporter : SmartstoreEntityImporterBase
{
    public override int Order => 75;
    public override string EntityType => SmartstoreImportEntityTypes.ProductVariant;
    public override IReadOnlyList<string> SourceTables => [SmartstoreImportTableNames.ProductVariantAttributeCombination];

    public override Task<SmartstoreEntityImportSummary> ImportAsync(
        SmartstoreImportContext context,
        CancellationToken cancellationToken = default)
    {
        var table = context.DataSet.GetTable(SmartstoreImportTableNames.ProductVariantAttributeCombination);
        if (table is null)
        {
            return Task.FromResult(Summary(EntityType, SmartstoreImportTableNames.ProductVariantAttributeCombination, 0, 0, 0, 0, 0, false));
        }

        var warnings = 0;
        foreach (var row in table.Rows)
        {
            if (!SmartstoreRowReader.TryGetInt(row, "Id", out var sourceId))
            {
                continue;
            }

            var productSourceId = SmartstoreRowReader.GetInt(row, "ProductId");
            if (!context.IdRegistry.TryGetTargetId(SmartstoreImportEntityTypes.Product, productSourceId, out _))
            {
                context.Issues.Warning(EntityType, sourceId, "product_ref_missing", $"Variant combination references missing product {productSourceId}.");
                warnings++;
                continue;
            }

            context.Issues.Warning(
                EntityType,
                sourceId,
                "variant_partial",
                "Variant combination metadata recorded; full variant SKU mapping requires attribute option import (Phase follow-up).",
                SmartstoreRowReader.GetString(row, "Sku"));
        }

        return Task.FromResult(Summary(
            EntityType,
            SmartstoreImportTableNames.ProductVariantAttributeCombination,
            table.Rows.Count,
            0,
            0,
            0,
            warnings,
            true));
    }
}
