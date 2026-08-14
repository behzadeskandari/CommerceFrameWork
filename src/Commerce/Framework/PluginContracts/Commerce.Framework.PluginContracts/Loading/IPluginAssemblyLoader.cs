using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.PluginContracts.Loading;

public interface IPluginAssemblyLoader
{
    LoadedPluginAssembly Load(PluginDescriptor descriptor);

    void Unload(string systemName);
}

public sealed record LoadedPluginAssembly(
    PluginDescriptor Descriptor,
    ICommercePlugin Plugin,
    System.Reflection.Assembly Assembly,
    object LoadContext);
