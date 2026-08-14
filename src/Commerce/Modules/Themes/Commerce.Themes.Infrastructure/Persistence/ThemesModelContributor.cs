using Commerce.Themes.Infrastructure.Persistence.Configurations;
using Commerce.Framework.Data.Db;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Themes.Infrastructure.Persistence;

public sealed class ThemesModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new StoreThemeConfigurationConfiguration());
}
