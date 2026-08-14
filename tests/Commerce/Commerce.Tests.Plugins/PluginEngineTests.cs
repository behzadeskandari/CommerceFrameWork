using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.Plugins.Dependency;
using Commerce.Framework.Plugins.Localization;
using Commerce.Framework.Plugins.Loading;
using Commerce.Framework.PluginContracts.Manifest;
using Commerce.Framework.PluginContracts.Packages;
using Xunit;

namespace Commerce.Tests.Plugins;

public sealed class PluginManifestParserTests
{
    [Fact]
    public void Parse_ValidManifest_ReturnsModel()
    {
        const string json = """
            {
              "systemName": "Payment.Manual",
              "name": "Manual Payment",
              "version": "1.0.0",
              "assembly": "Commerce.Plugin.Payment.Manual.dll",
              "minimumCommerceVersion": "1.0.0"
            }
            """;

        var manifest = PluginManifestParser.Parse(json);

        Assert.Equal("Payment.Manual", manifest.SystemName);
        Assert.Equal("Manual Payment", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
    }
}

public sealed class PluginManifestValidatorTests
{
    [Fact]
    public void Validate_MissingAssembly_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            SystemName = "Payment.Manual",
            Name = "Manual Payment",
            Version = "1.0.0",
            Assembly = "Missing.dll",
            MinimumCommerceVersion = "1.0.0"
        };

        var errors = PluginManifestValidator.Validate(manifest, Directory.GetCurrentDirectory(), new Version(1, 0, 0));

        Assert.Contains(errors, x => x.Contains("assembly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_InvalidSystemName_ReturnsError()
    {
        var manifest = new PluginManifest
        {
            SystemName = "invalid",
            Name = "Manual Payment",
            Version = "1.0.0",
            Assembly = "test.dll",
            MinimumCommerceVersion = "1.0.0"
        };

        var errors = PluginManifestValidator.Validate(manifest, Directory.GetCurrentDirectory(), new Version(1, 0, 0));

        Assert.Contains(errors, x => x.Contains("systemName", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class PluginDependencyResolverTests
{
    [Fact]
    public void Resolve_ValidDependencies_ReturnsTopologicalOrder()
    {
        var plugins = new List<PluginDescriptor>
        {
            CreateDescriptor("Payment.Manual"),
            CreateDescriptor("Payment.ZarinPal", "Payment.Manual")
        };

        var ordered = PluginDependencyResolver.Resolve(plugins);

        Assert.Equal("Payment.Manual", ordered[0].SystemName);
        Assert.Equal("Payment.ZarinPal", ordered[1].SystemName);
    }

    [Fact]
    public void Resolve_CircularDependency_Throws()
    {
        var plugins = new List<PluginDescriptor>
        {
            CreateDescriptor("Payment.A", "Payment.B"),
            CreateDescriptor("Payment.B", "Payment.A")
        };

        Assert.Throws<PluginDependencyResolutionException>(() => PluginDependencyResolver.Resolve(plugins));
    }

    private static PluginDescriptor CreateDescriptor(string systemName, params string[] dependencies) =>
        new(
            systemName.ToLowerInvariant(),
            systemName,
            systemName,
            new Version(1, 0, 0),
            "Author",
            "Description",
            null,
            dependencies.Select(d => new PluginDependency(d)).ToList(),
            new Version(1, 0, 0),
            null,
            false,
            false,
            $"{systemName}.dll",
            ".");
}

public sealed class PluginPackageServiceTests
{
    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("folder/../../outside.txt")]
    [InlineData("/etc/passwd")]
    public void IsPathTraversal_DetectsUnsafePaths(string path)
    {
        Assert.True(PluginPackagePathSecurity.IsPathTraversal(path));
    }

    [Fact]
    public void IsPathTraversal_AllowsSafeRelativePaths()
    {
        Assert.False(PluginPackagePathSecurity.IsPathTraversal("assets/logo.png"));
    }
}

public sealed class PluginAssemblyRegistryTests
{
    [Fact]
    public void Register_TracksAssemblyBySystemName()
    {
        var registry = new PluginAssemblyRegistry();
        var assembly = typeof(PluginAssemblyRegistryTests).Assembly;
        registry.Register("Commerce.Test", assembly);

        Assert.True(registry.TryGetSystemName(assembly, out var systemName));
        Assert.Equal("Commerce.Test", systemName);
    }
}

public sealed class PluginLocalizationLoaderTests
{
    [Fact]
    public void LoadFromDirectory_NamespacesTranslationKeys()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var localizationDirectory = Path.Combine(tempDirectory, "Localization");
        Directory.CreateDirectory(localizationDirectory);
        File.WriteAllText(Path.Combine(localizationDirectory, "en.json"), """{"Title":"Test"}""");

        var descriptor = new PluginDescriptor(
            "commerce.test",
            "Commerce.Test",
            "Commerce Test",
            new Version(1, 0, 0),
            null,
            null,
            null,
            [],
            new Version(1, 0, 0),
            null,
            false,
            false,
            "Commerce.Plugin.Test.dll",
            tempDirectory);

        var catalog = new PluginLocalizationCatalog();
        PluginLocalizationLoader.LoadFromDirectory(catalog, descriptor);

        var translations = catalog.GetTranslations("Commerce.Test", "en");
        Assert.True(translations.ContainsKey("Commerce.Test.Title"));
        Assert.Equal("Test", translations["Commerce.Test.Title"]);
    }
}
