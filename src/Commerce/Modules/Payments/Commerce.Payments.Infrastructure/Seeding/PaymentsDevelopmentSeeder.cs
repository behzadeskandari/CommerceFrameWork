using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Commerce.Payments.Contracts.Payments;
using Commerce.Payments.Domain.Entities;
using Commerce.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StoreEntity = Commerce.Store.Domain.Entities.Store;

namespace Commerce.Payments.Infrastructure.Seeding;

public sealed class PaymentsDevelopmentSeeder : IModuleSeeder
{
    public const string EnabledSettingKey = "Commerce:Payments:SeedDevelopmentData";

    public int Order => 220;

    public string Name => "Payments Development Data";

    public string ModuleSystemName => "Commerce.Payments";

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

        if (await dbContext.Set<PaymentMethod>().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var store = await dbContext.Set<StoreEntity>().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return;
        }

        var methods = new[]
        {
            PaymentMethod.Create(
                store.Id,
                "Bank Transfer",
                "bank-transfer",
                PaymentProviderNames.Manual,
                "Bank Transfer",
                isActive: true,
                displayOrder: 0,
                requiresRedirect: false,
                supportsGuest: true,
                supportsFreeOrders: false),
            PaymentMethod.Create(
                store.Id,
                "Cash on Delivery",
                "cash-on-delivery",
                PaymentProviderNames.Manual,
                "Cash on Delivery",
                isActive: true,
                displayOrder: 1,
                requiresRedirect: false,
                supportsGuest: true,
                supportsFreeOrders: false),
            PaymentMethod.Create(
                store.Id,
                "Free",
                PaymentProviderNames.FreeMethod,
                PaymentProviderNames.Manual,
                "No Payment Required",
                isActive: true,
                displayOrder: 2,
                requiresRedirect: false,
                supportsGuest: true,
                supportsFreeOrders: true),
            PaymentMethod.Create(
                store.Id,
                "ZarinPal",
                "zarinpal",
                PaymentProviderNames.ZarinPal,
                "ZarinPal",
                isActive: false,
                displayOrder: 3,
                requiresRedirect: true,
                supportsGuest: true,
                supportsFreeOrders: false),
            PaymentMethod.Create(
                store.Id,
                "Stripe",
                "stripe",
                PaymentProviderNames.Stripe,
                "Credit Card (Stripe)",
                isActive: false,
                displayOrder: 4,
                requiresRedirect: true,
                supportsGuest: true,
                supportsFreeOrders: false)
        };

        dbContext.Set<PaymentMethod>().AddRange(methods);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool IsEnabled(IConfiguration? configuration) =>
        bool.TryParse(configuration?[EnabledSettingKey], out var enabled) && enabled;
}
