using System.Reflection;
using Commerce.Framework.PluginContracts.Security;
using Commerce.Framework.PluginContracts.Settings;
using Commerce.Framework.PluginContracts.Ui;
using Commerce.Framework.Plugins.Loading;

namespace Commerce.Framework.Plugins.Discovery;

internal static class PluginAssemblyScanner
{
    public static IReadOnlyList<IPluginSettingDefinitionProvider> FindSettingProviders(Assembly assembly) =>
        InstantiateProviders<IPluginSettingDefinitionProvider>(assembly);

    public static IReadOnlyList<IPluginPermissionContributor> FindPermissionContributors(Assembly assembly) =>
        InstantiateProviders<IPluginPermissionContributor>(assembly);

    public static IReadOnlyList<IPluginUiMetadataProvider> FindUiMetadataProviders(Assembly assembly) =>
        InstantiateProviders<IPluginUiMetadataProvider>(assembly);

    public static IReadOnlyList<T> InstantiateProviders<T>(Assembly assembly)
        where T : class
    {
        var results = new List<T>();

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false, IsPublic: true })
            {
                continue;
            }

            if (!typeof(T).IsAssignableFrom(type))
            {
                continue;
            }

            if (Activator.CreateInstance(type) is T instance)
            {
                results.Add(instance);
            }
        }

        return results;
    }
}
