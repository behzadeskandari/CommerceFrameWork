using Commerce.Framework.PluginContracts.Plugins;
using Commerce.Plugin.Testing;

namespace Commerce.Tests.Plugin.Sdk;

public sealed class PluginTestHostBuilderTests
{
    [Fact]
    public void Build_WithValidManifest_CreatesContext()
    {
        using var temp = new TempDirectory();
        var manifest = PluginManifestTestFactory.Create("Sample.Test", "Sample Test", "Sample.Test.dll");
        File.WriteAllBytes(Path.Combine(temp.Path, "Sample.Test.dll"), [1]);

        var host = new PluginTestHostBuilder()
            .WithManifest(manifest)
            .WithPluginDirectory(temp.Path)
            .Build();

        Assert.Equal("Sample.Test", host.Context.Descriptor.SystemName);
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
