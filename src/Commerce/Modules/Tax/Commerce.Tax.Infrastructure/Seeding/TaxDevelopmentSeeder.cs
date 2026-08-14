using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Tax.Application;
using Commerce.Tax.Domain.Entities;
using Commerce.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Tax.Infrastructure.Seeding;

public sealed class TaxDevelopmentSeeder : IModuleSeeder
{
    public const string EnabledSettingKey = "Commerce:Tax:SeedDevelopmentData";

    public int Order => 210;

    public string Name => "Tax Development Data";

    public string ModuleSystemName => "Commerce.Tax";

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

        if (await dbContext.Set<TaxCategory>().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var store = await dbContext.Set<StoreEntity>().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return;
        }

        var category = TaxCategory.Create(
            store.Id,
            "Standard",
            "standard",
            "Standard taxable goods.",
            isExempt: false,
            isActive: true,
            displayOrder: 0);

        dbContext.Set<TaxCategory>().Add(category);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var settingService = scope.ServiceProvider.GetService<ISettingService>();
        if (settingService is not null)
        {
            await settingService.SetAsync(
                TaxSettingKeys.DefaultCategoryId,
                category.Id.ToString(),
                store.Id,
                cancellationToken).ConfigureAwait(false);
        }

        var zone = TaxZone.Create(
            store.Id,
            "Default Zone",
            "default",
            isDefault: true,
            isActive: true,
            displayOrder: 0);

        zone.ReplaceCountries([TaxZoneCountry.Create(0, "US")]);
        dbContext.Set<TaxZone>().Add(zone);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var rate = TaxRate.CreatePercentage(
            store.Id,
            category.Id,
            zone.Id,
            percentage: 10m,
            taxShipping: true,
            priority: 0,
            effectiveFromUtc: null,
            effectiveToUtc: null);

        dbContext.Set<TaxRate>().Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsEnabled(IConfiguration? configuration) =>
        bool.TryParse(configuration?[EnabledSettingKey], out var enabled) && enabled;
}
