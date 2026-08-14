namespace Commerce.Framework.Plugins.Configuration;

public sealed class CommercePluginOptions
{
    public const string SectionName = "Commerce:Plugins";

    public string RootPath { get; set; } = "Plugins";

    public bool SeedDevelopmentData { get; set; }

    /// <summary>
    /// When false, plugin assemblies are not loaded during host service registration.
    /// Integration tests disable this and install plugins via the admin API after setup.
    /// </summary>
    public bool RegisterServicesAtStartup { get; set; } = true;

    public string CommerceVersion { get; set; } = "1.0.0";

    public long MaxPackageSizeBytes { get; set; } = 50 * 1024 * 1024;

    public int MaxPackageFileCount { get; set; } = 500;
}
