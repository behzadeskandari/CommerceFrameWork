using System.Text.RegularExpressions;
using Commerce.Framework.PluginContracts.Plugins;

namespace Commerce.Framework.PluginContracts.Manifest;

public static partial class PluginManifestValidator
{
    private static readonly Regex SystemNamePattern = SystemNameRegex();

    public static IReadOnlyList<string> Validate(
        PluginManifest manifest,
        string pluginDirectory,
        Version commerceVersion,
        IReadOnlySet<string>? knownSystemNames = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(manifest.SystemName))
        {
            errors.Add("Manifest systemName is required.");
        }
        else if (!SystemNamePattern.IsMatch(manifest.SystemName))
        {
            errors.Add($"Manifest systemName '{manifest.SystemName}' has an invalid format.");
        }
        else if (knownSystemNames?.Contains(manifest.SystemName) == true)
        {
            errors.Add($"Duplicate plugin systemName '{manifest.SystemName}' detected.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("Manifest name is required.");
        }

        if (!TryParseVersion(manifest.Version, out _))
        {
            errors.Add($"Manifest version '{manifest.Version}' is invalid.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Assembly))
        {
            errors.Add("Manifest assembly is required.");
        }
        else
        {
            var assemblyPath = Path.Combine(pluginDirectory, manifest.Assembly);
            if (!File.Exists(assemblyPath))
            {
                errors.Add($"Plugin assembly '{manifest.Assembly}' was not found in '{pluginDirectory}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(manifest.MinimumCommerceVersion))
        {
            errors.Add("Manifest minimumCommerceVersion is required.");
        }
        else if (TryParseVersion(manifest.MinimumCommerceVersion, out var minimumVersion))
        {
            if (commerceVersion < minimumVersion)
            {
                errors.Add(
                    $"Plugin '{manifest.SystemName}' requires Commerce {minimumVersion} but current version is {commerceVersion}.");
            }
        }
        else
        {
            errors.Add($"Manifest minimumCommerceVersion '{manifest.MinimumCommerceVersion}' is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(manifest.MaximumCommerceVersion))
        {
            if (TryParseVersion(manifest.MaximumCommerceVersion, out var maximumVersion) &&
                commerceVersion > maximumVersion)
            {
                errors.Add(
                    $"Plugin '{manifest.SystemName}' supports Commerce up to {maximumVersion} but current version is {commerceVersion}.");
            }
            else if (!TryParseVersion(manifest.MaximumCommerceVersion, out _))
            {
                errors.Add($"Manifest maximumCommerceVersion '{manifest.MaximumCommerceVersion}' is invalid.");
            }
        }

        foreach (var dependency in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency.SystemName))
            {
                errors.Add("Plugin dependency systemName is required.");
            }
        }

        return errors;
    }

    public static PluginDescriptor ToDescriptor(PluginManifest manifest, string pluginDirectory)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (!TryParseVersion(manifest.Version, out var version))
        {
            throw new InvalidOperationException($"Invalid plugin version '{manifest.Version}'.");
        }

        Version? minimumCommerceVersion = null;
        if (TryParseVersion(manifest.MinimumCommerceVersion, out var minVersion))
        {
            minimumCommerceVersion = minVersion;
        }

        Version? maximumCommerceVersion = null;
        if (!string.IsNullOrWhiteSpace(manifest.MaximumCommerceVersion) &&
            TryParseVersion(manifest.MaximumCommerceVersion, out var maxVersion))
        {
            maximumCommerceVersion = maxVersion;
        }

        var dependencies = manifest.Dependencies
            .Select(d => new PluginDependency(d.SystemName, d.MinimumVersion, d.MaximumVersion))
            .ToList();

        return new PluginDescriptor(
            Id: manifest.SystemName.ToLowerInvariant(),
            SystemName: manifest.SystemName,
            Name: manifest.Name,
            Version: version,
            Author: manifest.Author ?? string.Empty,
            Description: manifest.Description ?? string.Empty,
            Website: manifest.Website,
            Dependencies: dependencies,
            MinimumCommerceVersion: minimumCommerceVersion,
            MaximumCommerceVersion: maximumCommerceVersion,
            IsSystemPlugin: manifest.IsSystemPlugin,
            IsRequired: manifest.IsRequired,
            AssemblyName: manifest.Assembly,
            PluginDirectory: pluginDirectory);
    }

    public static bool TryParseVersion(string? value, out Version version)
    {
        version = new Version(0, 0);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Split('-', '+')[0];
        return Version.TryParse(normalized, out version!);
    }

    public static IReadOnlyList<string> ValidateArchiveEntries(
        PluginManifest manifest,
        IEnumerable<string> archiveEntryNames,
        Version commerceVersion)
    {
        var errors = new List<string>();
        var normalizedEntries = archiveEntryNames
            .Select(entry => entry.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!normalizedEntries.Contains(manifest.Assembly) &&
            !normalizedEntries.Any(entry => entry.EndsWith("/" + manifest.Assembly, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"Plugin assembly '{manifest.Assembly}' was not found in the package.");
        }

        errors.AddRange(ValidateManifestFields(manifest, commerceVersion));
        return errors;
    }

    public static IReadOnlyList<string> ValidateManifestFields(PluginManifest manifest, Version commerceVersion)
    {
        return Validate(manifest, pluginDirectory: Directory.GetCurrentDirectory(), commerceVersion)
            .Where(error => !error.Contains("was not found in", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*(\\.[A-Za-z][A-Za-z0-9]*)+$")]
    private static partial Regex SystemNameRegex();
}
