using Commerce.Catalog.Domain.Entities;
using Commerce.Catalog.Domain.Enums;
using Commerce.Catalog.Domain.ValueObjects;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Domain.ValueObjects;
using Commerce.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Catalog.Infrastructure.Seeding;

public sealed class CatalogDevelopmentSeeder : IModuleSeeder
{
    public const string EnabledSettingKey = "Commerce:Catalog:SeedDevelopmentData";

    public int Order => 100;

    public string Name => "Catalog Development Data";

    public string ModuleSystemName => "Commerce.Catalog";

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var configuration = context.Services.GetService<IConfiguration>();
        if (!IsEnabled(configuration))
        {
            return;
        }

        await using var scope = context.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();

        if (await dbContext.Set<Product>().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var store = await dbContext.Set<StoreEntity>().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var irr = await dbContext.Set<StoreCurrency>().FirstOrDefaultAsync(x => x.Code == "IRR", cancellationToken)
            .ConfigureAwait(false);
        var usd = await dbContext.Set<StoreCurrency>().FirstOrDefaultAsync(x => x.Code == "USD", cancellationToken)
            .ConfigureAwait(false);

        if (store is null || irr is null || usd is null)
        {
            return;
        }

        var color = ProductAttributeDefinition.Create("Color", "color", AttributeType.Option, displayOrder: 0);
        var size = ProductAttributeDefinition.Create("Size", "size", AttributeType.Option, displayOrder: 1);
        dbContext.Set<ProductAttributeDefinition>().AddRange(color, size);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var colorOptions = new[]
        {
            ProductAttributeOption.Create(color.Id, "Black", displayOrder: 0),
            ProductAttributeOption.Create(color.Id, "White", displayOrder: 1),
            ProductAttributeOption.Create(color.Id, "Red", displayOrder: 2)
        };

        var sizeOptions = new[]
        {
            ProductAttributeOption.Create(size.Id, "S", displayOrder: 0),
            ProductAttributeOption.Create(size.Id, "M", displayOrder: 1),
            ProductAttributeOption.Create(size.Id, "L", displayOrder: 2),
            ProductAttributeOption.Create(size.Id, "XL", displayOrder: 3)
        };

        dbContext.Set<ProductAttributeOption>().AddRange(colorOptions);
        dbContext.Set<ProductAttributeOption>().AddRange(sizeOptions);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var course = Product.Create(
            "C# Course",
            Sku.Create("COURSE-CSHARP"),
            ProductType.Digital,
            shortDescription: "Learn C# from scratch.",
            description: "A comprehensive digital course covering C# fundamentals and advanced topics.",
            slug: Slug.Create("csharp-course"),
            published: true,
            isVisible: true,
            isAvailable: true,
            displayOrder: 0);

        var tShirt = Product.Create(
            "Classic T-Shirt",
            Sku.Create("TSHIRT-CLASSIC"),
            ProductType.Variant,
            shortDescription: "Comfortable cotton t-shirt.",
            description: "Available in multiple colors and sizes.",
            slug: Slug.Create("classic-t-shirt"),
            published: true,
            isVisible: true,
            isAvailable: true,
            displayOrder: 1);

        dbContext.Set<Product>().AddRange(course, tShirt);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        dbContext.Set<ProductAttributeAssignment>().AddRange(
            ProductAttributeAssignment.Create(tShirt.Id, color.Id, displayOrder: 0),
            ProductAttributeAssignment.Create(tShirt.Id, size.Id, displayOrder: 1));

        var courseIrrOffer = ProductOffer.Create(
            course.Id,
            variantId: null,
            store.Id,
            irr.Id,
            irr.Code,
            Money.Create(2_500_000m, Currency.Irr),
            Money.Create(3_000_000m, Currency.Irr));

        var courseUsdOffer = ProductOffer.Create(
            course.Id,
            variantId: null,
            store.Id,
            usd.Id,
            usd.Code,
            Money.Create(59.99m, Currency.Usd),
            Money.Create(79.99m, Currency.Usd));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var variants = new List<ProductVariant>();
        var displayOrder = 0;
        var isFirst = true;

        foreach (var colorOption in colorOptions.Take(2))
        {
            foreach (var sizeOption in sizeOptions.Take(3))
            {
                var optionIds = new[] { colorOption.Id, sizeOption.Id }.OrderBy(x => x).ToList();
                var variant = ProductVariant.Create(
                    tShirt.Id,
                    Sku.Create($"TSHIRT-{colorOption.Value.ToUpperInvariant()}-{sizeOption.Value}"),
                    $"{tShirt.Name} {colorOption.Value} / {sizeOption.Value}",
                    optionIds,
                    isActive: true,
                    isDefault: isFirst,
                    displayOrder: displayOrder++);

                variants.Add(variant);
                isFirst = false;
            }
        }

        dbContext.Set<ProductVariant>().AddRange(variants);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var variant in variants)
        {
            variant.MaterializeAttributes();
        }

        dbContext.Set<ProductOffer>().AddRange(courseIrrOffer, courseUsdOffer);

        foreach (var variant in variants)
        {
            dbContext.Set<ProductOffer>().Add(ProductOffer.Create(
                tShirt.Id,
                variant.Id,
                store.Id,
                irr.Id,
                irr.Code,
                Money.Create(450_000m, Currency.Irr)));

            dbContext.Set<ProductOffer>().Add(ProductOffer.Create(
                tShirt.Id,
                variant.Id,
                store.Id,
                usd.Id,
                usd.Code,
                Money.Create(24.99m, Currency.Usd)));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static bool IsEnabled(IConfiguration? configuration) =>
        string.Equals(configuration?[EnabledSettingKey], "true", StringComparison.OrdinalIgnoreCase);
}
