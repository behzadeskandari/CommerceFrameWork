using Commerce.Framework.PluginContracts.Discovery;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.Plugins.Configuration;
using Commerce.Framework.PluginContracts.Manifest;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Commerce.Framework.Plugins.Discovery;

public sealed class PluginDiscoveryService(
    IHostEnvironment hostEnvironment,
    IOptions<CommercePluginOptions> options) : IPluginDiscoveryService
{
    public IReadOnlyList<DiscoveredPlugin> Discover()
    {
        var rootPath = ResolveRootPath();
        if (!Directory.Exists(rootPath))
        {
            return [];
        }

        var manifestFiles = Directory.EnumerateFiles(rootPath, "Plugin.json", SearchOption.AllDirectories).ToList();
        var commerceVersion = ParseCommerceVersion(options.Value.CommerceVersion);
        var discovered = new List<DiscoveredPlugin>();
        var knownSystemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var manifestPath in manifestFiles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var pluginDirectory = Path.GetDirectoryName(manifestPath)
                ?? throw new InvalidOperationException($"Unable to resolve plugin directory for '{manifestPath}'.");

            try
            {
                var manifest = PluginManifestParser.ParseFile(manifestPath);
                var validationErrors = PluginManifestValidator.Validate(
                    manifest,
                    pluginDirectory,
                    commerceVersion,
                    knownSystemNames);

                if (validationErrors.Count > 0)
                {
                    continue;
                }

                knownSystemNames.Add(manifest.SystemName);
                var descriptor = PluginManifestValidator.ToDescriptor(manifest, pluginDirectory);
                var assemblyPath = Path.Combine(pluginDirectory, manifest.Assembly);

                discovered.Add(new DiscoveredPlugin(manifest, descriptor, manifestPath, assemblyPath));
            }
            catch
            {
                // Invalid manifests are ignored during discovery; validation surfaces details later.
            }
        }

        return discovered;
    }

    public DiscoveredPlugin? FindBySystemName(string systemName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);
        return Discover().FirstOrDefault(x =>
            string.Equals(x.Descriptor.SystemName, systemName, StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveRootPath()
    {
        var configuredRoot = options.Value.RootPath;
        return Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(hostEnvironment.ContentRootPath, configuredRoot);
    }

    private static Version ParseCommerceVersion(string value) =>
        PluginManifestValidator.TryParseVersion(value, out var version) ? version : new Version(1, 0, 0);
}
