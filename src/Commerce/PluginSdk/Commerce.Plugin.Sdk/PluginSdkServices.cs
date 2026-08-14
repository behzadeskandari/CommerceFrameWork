using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Framework.PluginContracts.Manifest;
using Commerce.Plugin.Contracts;

namespace Commerce.Plugin.Sdk;

public sealed class PluginValidationOptions
{
    public Version CommerceVersion { get; init; } = new(1, 0, 0);

    public IReadOnlySet<string>? InstalledPluginSystemNames { get; init; }
}

public sealed class PluginValidationReport
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; } = [];

    public List<string> Warnings { get; } = [];

    public PluginManifest? Manifest { get; set; }
}

public static class PluginProjectValidator
{
    public static PluginValidationReport ValidateProject(string projectFilePath)
    {
        var report = new PluginValidationReport();
        if (!File.Exists(projectFilePath))
        {
            report.Errors.Add($"Project file not found: '{projectFilePath}'.");
            return report;
        }

        var document = XDocument.Load(projectFilePath);
        var references = document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        foreach (var reference in references)
        {
            if (PluginDevelopmentRules.ForbiddenReferencePrefixes.Any(prefix =>
                    reference.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                report.Errors.Add($"Forbidden project reference '{reference}'. Plugins must not reference host or engine assemblies.");
            }
        }

        var manifestPath = Path.Combine(Path.GetDirectoryName(projectFilePath)!, PluginPackageLayout.ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            report.Errors.Add("Plugin.json manifest is required next to the plugin project file.");
            return report;
        }

        return report;
    }
}

public static class PluginManifestWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Write(string path, PluginManifest manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(manifest);
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(path, json);
    }
}

public static class PluginPackagePacker
{
    public static void PackDirectory(string sourceDirectory, string outputZipPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputZipPath);

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Plugin output directory not found: '{sourceDirectory}'.");
        }

        var manifestPath = Path.Combine(sourceDirectory, PluginPackageLayout.ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Plugin.json was not found in the output directory.", manifestPath);
        }

        if (File.Exists(outputZipPath))
        {
            File.Delete(outputZipPath);
        }

        ZipFile.CreateFromDirectory(sourceDirectory, outputZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
    }
}

public static class PluginSdkValidator
{
    public static PluginValidationReport ValidateDirectory(string pluginDirectory, PluginValidationOptions? options = null)
    {
        options ??= new PluginValidationOptions();
        var report = new PluginValidationReport();

        var manifestPath = Path.Combine(pluginDirectory, PluginPackageLayout.ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            report.Errors.Add("Plugin.json was not found.");
            return report;
        }

        PluginManifest manifest;
        try
        {
            manifest = PluginManifestParser.ParseFile(manifestPath);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Failed to parse Plugin.json: {ex.Message}");
            return report;
        }

        report.Manifest = manifest;
        report.Errors.AddRange(PluginManifestValidator.Validate(
            manifest,
            pluginDirectory,
            options.CommerceVersion,
            options.InstalledPluginSystemNames));

        foreach (var dependency in manifest.Dependencies)
        {
            if (options.InstalledPluginSystemNames is not null &&
                !options.InstalledPluginSystemNames.Contains(dependency.SystemName))
            {
                report.Warnings.Add($"Dependency '{dependency.SystemName}' is not installed.");
            }
        }

        return report;
    }

    public static PluginCompatibilityInfo EvaluateCompatibility(PluginManifest manifest, Version commerceVersion)
    {
        var messages = new List<string>();
        var minimum = manifest.MinimumCommerceVersion;
        var maximum = manifest.MaximumCommerceVersion;
        var compatible = true;

        if (PluginManifestValidator.TryParseVersion(minimum, out var minVersion) && commerceVersion < minVersion)
        {
            compatible = false;
            messages.Add($"Requires Commerce {minVersion} or later.");
        }

        if (!string.IsNullOrWhiteSpace(maximum) &&
            PluginManifestValidator.TryParseVersion(maximum, out var maxVersion) &&
            commerceVersion > maxVersion)
        {
            compatible = false;
            messages.Add($"Supports Commerce up to {maxVersion}.");
        }

        return new PluginCompatibilityInfo
        {
            CommerceVersion = commerceVersion.ToString(),
            MinimumCommerceVersion = minimum,
            MaximumCommerceVersion = maximum,
            IsCompatible = compatible,
            Messages = messages
        };
    }
}
