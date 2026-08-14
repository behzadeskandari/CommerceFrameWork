using System.IO.Compression;
using System.Text;
using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Plugin.Sdk;

namespace Commerce.Tests.Plugin.Sdk;

public sealed class PluginSdkValidatorTests
{
    [Fact]
    public void ValidateDirectory_ValidOutput_ReturnsNoErrors()
    {
        using var temp = CreatePluginOutput("Sample.Hello", "Sample Hello");

        var report = PluginSdkValidator.ValidateDirectory(temp.Path);

        Assert.True(report.IsValid, string.Join(", ", report.Errors));
        Assert.Equal("Sample.Hello", report.Manifest?.SystemName);
    }

    [Fact]
    public void ValidateDirectory_MissingManifest_ReturnsError()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var report = PluginSdkValidator.ValidateDirectory(directory);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, error => error.Contains("Plugin.json", StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateCompatibility_ReportsIncompatibleVersion()
    {
        var manifest = new PluginManifest
        {
            SystemName = "Sample.Hello",
            Name = "Hello",
            Version = "1.0.0",
            Assembly = "Sample.Hello.dll",
            MinimumCommerceVersion = "2.0.0"
        };

        var compatibility = PluginSdkValidator.EvaluateCompatibility(manifest, new Version(1, 0, 0));

        Assert.False(compatibility.IsCompatible);
    }

    private static TempPluginDirectory CreatePluginOutput(string systemName, string name)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "Plugin.json"), $$"""
            {
              "systemName": "{{systemName}}",
              "name": "{{name}}",
              "version": "1.0.0",
              "assembly": "{{systemName}}.dll",
              "minimumCommerceVersion": "1.0.0"
            }
            """);
        File.WriteAllBytes(Path.Combine(directory, $"{systemName}.dll"), Encoding.UTF8.GetBytes("stub"));
        return new TempPluginDirectory(directory);
    }

    private sealed class TempPluginDirectory(string path) : IDisposable
    {
        public string Path { get; } = path;

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}

public sealed class PluginPackagePackerTests
{
    [Fact]
    public void PackDirectory_CreatesZipWithManifest()
    {
        using var temp = new TempDirectory();
        var pluginDirectory = Path.Combine(temp.Path, "plugin");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "Plugin.json"), """{"systemName":"Sample.Pack","name":"Pack","version":"1.0.0","assembly":"Sample.Pack.dll","minimumCommerceVersion":"1.0.0"}""");
        File.WriteAllBytes(Path.Combine(pluginDirectory, "Sample.Pack.dll"), [1, 2, 3]);

        var zipPath = Path.Combine(temp.Path, "Sample.Pack.zip");
        PluginPackagePacker.PackDirectory(pluginDirectory, zipPath);

        using var archive = ZipFile.OpenRead(zipPath);
        Assert.Contains(archive.Entries, entry => entry.Name == "Plugin.json");
        Assert.Contains(archive.Entries, entry => entry.Name == "Sample.Pack.dll");
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
