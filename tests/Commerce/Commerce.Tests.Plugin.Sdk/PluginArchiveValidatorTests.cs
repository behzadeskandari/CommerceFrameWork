using System.IO.Compression;
using System.Text;
using Commerce.Plugin.Sdk;

namespace Commerce.Tests.Plugin.Sdk;

public sealed class PluginArchiveValidatorTests
{
    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("folder/../../outside.txt")]
    [InlineData("/etc/passwd")]
    public void IsPathTraversal_DetectsUnsafePaths(string path)
    {
        Assert.True(Commerce.Framework.PluginContracts.Packages.PluginPackagePathSecurity.IsPathTraversal(path));
    }

    [Fact]
    public void ValidateZip_ValidPackage_ReturnsNoErrors()
    {
        using var temp = new TempDirectory();
        var pluginDirectory = Path.Combine(temp.Path, "plugin");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "Plugin.json"), """{"systemName":"Sample.Zip","name":"Zip","version":"1.0.0","assembly":"Sample.Zip.dll","minimumCommerceVersion":"1.0.0"}""");
        File.WriteAllBytes(Path.Combine(pluginDirectory, "Sample.Zip.dll"), [1, 2, 3]);
        var zipPath = Path.Combine(temp.Path, "Sample.Zip.zip");
        ZipFile.CreateFromDirectory(pluginDirectory, zipPath);

        var report = PluginArchiveValidator.ValidateZip(zipPath);

        Assert.True(report.IsValid, string.Join(", ", report.Errors));
        Assert.Equal("Sample.Zip", report.Manifest?.SystemName);
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
