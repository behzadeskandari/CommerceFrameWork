using Commerce.Framework.Data.Db;
using Commerce.Framework.Plugins.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Framework.Plugins.Persistence;

public sealed class PluginModelContributor : ICommerceModelContributor
{
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CommercePluginInstallationConfiguration());
        modelBuilder.ApplyConfiguration(new CommercePluginStoreConfigurationConfiguration());
    }
}
