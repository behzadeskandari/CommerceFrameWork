using Commerce.Catalog.Domain.Entities;
using Commerce.Framework.Contracts.Seeding;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        if (await dbContext.Set<ProductAttributeDefinition>().AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var definitions = new[]
        {
            ProductAttributeDefinition.Create("Brand", "brand", 1),
            ProductAttributeDefinition.Create("Publisher", "publisher", 2),
            ProductAttributeDefinition.Create("Platform", "platform", 3),
            ProductAttributeDefinition.Create("Language", "language", 4),
            ProductAttributeDefinition.Create("Genre", "genre", 5)
        };

        dbContext.Set<ProductAttributeDefinition>().AddRange(definitions);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static bool IsEnabled(IConfiguration? configuration) =>
        string.Equals(configuration?[EnabledSettingKey], "true", StringComparison.OrdinalIgnoreCase);
}
