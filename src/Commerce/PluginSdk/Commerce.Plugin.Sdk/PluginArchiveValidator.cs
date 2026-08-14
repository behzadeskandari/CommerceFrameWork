using System.IO.Compression;
using Commerce.Framework.PluginContracts.Manifest;
using Commerce.Framework.PluginContracts.Packages;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Plugin.Contracts;

namespace Commerce.Plugin.Sdk;

public sealed class PluginPackageLimits
{
    public long MaxPackageSizeBytes { get; init; } = 50 * 1024 * 1024;

    public int MaxPackageFileCount { get; init; } = 500;
}

public static class PluginArchiveValidator
{
    public static PluginValidationReport ValidateZip(string zipPath, PluginPackageLimits? limits = null, Version? commerceVersion = null)
    {
        limits ??= new PluginPackageLimits();
        commerceVersion ??= new Version(1, 0, 0);
        var report = new PluginValidationReport();

        if (!File.Exists(zipPath))
        {
            report.Errors.Add($"Package not found: '{zipPath}'.");
            return report;
        }

        var fileInfo = new FileInfo(zipPath);
        if (fileInfo.Length > limits.MaxPackageSizeBytes)
        {
            report.Errors.Add("Plugin package exceeds the maximum allowed size.");
            return report;
        }

        using var stream = File.OpenRead(zipPath);
        return ValidateZipStream(stream, limits, commerceVersion);
    }

    public static PluginValidationReport ValidateZipStream(
        Stream packageStream,
        PluginPackageLimits? limits = null,
        Version? commerceVersion = null)
    {
        limits ??= new PluginPackageLimits();
        commerceVersion ??= new Version(1, 0, 0);
        var report = new PluginValidationReport();

        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > limits.MaxPackageFileCount)
        {
            report.Errors.Add("Plugin package exceeds the maximum allowed file count.");
            return report;
        }

        foreach (var entry in archive.Entries)
        {
            if (PluginPackagePathSecurity.IsPathTraversal(entry.FullName))
            {
                report.Errors.Add($"Unsafe path in plugin package: '{entry.FullName}'.");
                return report;
            }
        }

        var manifestEntry = archive.Entries.FirstOrDefault(entry =>
            entry.FullName.Equals(PluginPackageLayout.ManifestFileName, StringComparison.OrdinalIgnoreCase) ||
            entry.FullName.EndsWith("/" + PluginPackageLayout.ManifestFileName, StringComparison.OrdinalIgnoreCase));

        if (manifestEntry is null)
        {
            report.Errors.Add("Plugin package must contain Plugin.json.");
            return report;
        }

        using var manifestStream = manifestEntry.Open();
        using var reader = new StreamReader(manifestStream);
        var manifest = PluginManifestParser.Parse(reader.ReadToEnd());
        report.Manifest = manifest;
        report.Errors.AddRange(PluginManifestValidator.ValidateArchiveEntries(
            manifest,
            archive.Entries.Select(entry => entry.FullName),
            commerceVersion));

        return report;
    }
}
