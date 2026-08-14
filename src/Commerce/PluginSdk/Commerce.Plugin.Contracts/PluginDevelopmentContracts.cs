namespace Commerce.Plugin.Contracts;

public static class PluginPackageLayout
{
    public const string ManifestFileName = "Plugin.json";
    public const string LocalizationFolder = "Localization";
    public const string DependenciesFolder = "dependencies";
    public const string AssetsFolder = "assets";
}

public static class PluginDevelopmentRules
{
    public static readonly string[] AllowedContractReferences =
    [
        "Commerce.Framework.PluginContracts",
        "Commerce.Plugin.Contracts",
        "Commerce.Plugin.Sdk"
    ];

    public static readonly string[] ForbiddenReferencePrefixes =
    [
        "Commerce.Framework.Plugins",
        "Commerce.Host",
        "Commerce.Modules."
    ];
}

public sealed class PluginCompatibilityInfo
{
    public required string CommerceVersion { get; init; }

    public required string MinimumCommerceVersion { get; init; }

    public string? MaximumCommerceVersion { get; init; }

    public bool IsCompatible { get; init; }

    public IReadOnlyList<string> Messages { get; init; } = [];
}
