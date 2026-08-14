using System.Reflection;
using Commerce.Framework.Data.Migrations;
using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.Plugins.Migrations;

public static class PluginMigrationDiscoverer
{
    public static IReadOnlyList<ICommerceMigration> Discover(Assembly assembly, PluginDescriptor descriptor)
    {
        var migrations = new List<ICommerceMigration>();

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false, IsPublic: true })
            {
                continue;
            }

            if (!typeof(ICommerceMigration).IsAssignableFrom(type))
            {
                continue;
            }

            if (Activator.CreateInstance(type) is ICommerceMigration migration &&
                string.Equals(migration.Module, descriptor.SystemName, StringComparison.OrdinalIgnoreCase))
            {
                migrations.Add(migration);
            }
        }

        return migrations
            .OrderBy(x => x.Version, StringComparer.Ordinal)
            .ToList();
    }
}
