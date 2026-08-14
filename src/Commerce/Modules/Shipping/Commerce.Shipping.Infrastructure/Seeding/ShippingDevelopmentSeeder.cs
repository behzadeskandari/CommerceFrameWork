using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Domain.Entities;
using Commerce.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Shipping.Infrastructure.Seeding;

public sealed class ShippingDevelopmentSeeder : IModuleSeeder
{
    public const string EnabledSettingKey = "Commerce:Shipping:SeedDevelopmentData";

    public int Order => 200;

    public string Name => "Shipping Development Data";

    public string ModuleSystemName => "Commerce.Shipping";

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

        if (await dbContext.Set<ShippingMethod>().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var store = await dbContext.Set<StoreEntity>().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var currency = await dbContext.Set<StoreCurrency>()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (store is null || currency is null)
        {
            return;
        }

        var method = ShippingMethod.Create(
            store.Id,
            "Flat Rate",
            "flat-rate",
            "Standard flat rate shipping.",
            ShippingProviderNames.FlatRate,
            isActive: true,
            displayOrder: 0,
            requiresAddress: true,
            supportsTracking: false,
            estimatedDeliveryDaysMin: 3,
            estimatedDeliveryDaysMax: 7);

        dbContext.Set<ShippingMethod>().Add(method);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var zone = ShippingZone.Create(
            store.Id,
            "Default Zone",
            "default",
            isDefault: true,
            isActive: true,
            displayOrder: 0);

        dbContext.Set<ShippingZone>().Add(zone);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var rate = ShippingRate.CreateFlat(
            store.Id,
            method.Id,
            zone.Id,
            currency.Code,
            basePrice: 10m,
            freeShippingThreshold: 100m,
            minOrderSubtotal: null,
            maxOrderSubtotal: null,
            pricePerWeightUnit: 2m);

        dbContext.Set<ShippingRate>().Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsEnabled(IConfiguration? configuration) =>
        bool.TryParse(configuration?[EnabledSettingKey], out var enabled) && enabled;
}
