namespace Commerce.Framework.PluginContracts.Plugins;

public sealed class PluginManifest
{
    public string SystemName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Assembly { get; set; } = string.Empty;

    public string MinimumCommerceVersion { get; set; } = string.Empty;

    public string? MaximumCommerceVersion { get; set; }

    public string? Author { get; set; }

    public string? Description { get; set; }

    public string? Website { get; set; }

    public bool IsSystemPlugin { get; set; }

    public bool IsRequired { get; set; }

    public IReadOnlyList<PluginManifestDependency> Dependencies { get; set; } = [];
}

public sealed class PluginManifestDependency
{
    public string SystemName { get; set; } = string.Empty;

    public string? MinimumVersion { get; set; }

    public string? MaximumVersion { get; set; }
}
