using Commerce.Framework.Themes;

namespace Commerce.Tests.Unit.Themes;

public sealed class ThemeValueSanitizerTests
{
    [Fact]
    public void Sanitize_AcceptsValidHexColor()
    {
        var result = ThemeValueSanitizer.Sanitize("primaryColor", "#0f766e", "color");
        Assert.Equal("#0f766e", result);
    }

    [Fact]
    public void Sanitize_RejectsScriptInTextSetting()
    {
        Assert.Throws<ArgumentException>(() =>
            ThemeValueSanitizer.Sanitize("brand", "<script>alert(1)</script>", "text"));
    }

    [Fact]
    public void Sanitize_RejectsInvalidColor()
    {
        Assert.Throws<ArgumentException>(() =>
            ThemeValueSanitizer.Sanitize("primaryColor", "javascript:alert(1)", "color"));
    }

    [Fact]
    public void ToCssVariables_MapsPrimaryColor()
    {
        var variables = ThemeCssVariableMapper.ToCssVariables(new Dictionary<string, string>
        {
            ["primaryColor"] = "#0f766e"
        });

        Assert.Equal("#0f766e", variables["--primary"]);
    }
}

public sealed class ThemeRegistryTests
{
    [Fact]
    public void GetDefault_ReturnsRegisteredProvider()
    {
        var registry = new ThemeRegistry([new TestThemeProvider()], Microsoft.Extensions.Logging.Abstractions.NullLogger<ThemeRegistry>.Instance);
        var theme = registry.GetDefault();
        Assert.Equal("Themes.Test", theme.Manifest.SystemName);
    }

    [Fact]
    public void GetBySystemName_IsCaseInsensitive()
    {
        var registry = new ThemeRegistry([new TestThemeProvider()], Microsoft.Extensions.Logging.Abstractions.NullLogger<ThemeRegistry>.Instance);
        var theme = registry.GetBySystemName("themes.test");
        Assert.NotNull(theme);
    }

    private sealed class TestThemeProvider : IThemeProvider
    {
        public ThemeDescriptor GetDescriptor() => new(new ThemeManifest(
            "Themes.Test",
            "Test",
            "1.0.0",
            "Test",
            "Test theme",
            new ThemeAssetReferences([], []),
            [new ThemeSettingDefinition("primaryColor", "Primary", "color", "#000000")],
            [new ThemeLayoutDefinition(ThemeLayoutType.Homepage, [ThemeLayoutZoneNames.Header], false)]));
    }
}

public sealed class ThemeContextLayoutTests
{
    [Fact]
    public void GetLayout_UsesFallbackWhenMissing()
    {
        var registry = new ThemeRegistry([new ThemeRegistryTestsTestThemeProvider()], Microsoft.Extensions.Logging.Abstractions.NullLogger<ThemeRegistry>.Instance);
        var context = new ThemeContext(registry, "Themes.Test", null, null);
        var layout = context.GetLayout(ThemeLayoutType.Product, null);
        Assert.Contains(ThemeLayoutZoneNames.ProductBefore, layout.Zones);
    }

    private sealed class ThemeRegistryTestsTestThemeProvider : IThemeProvider
    {
        public ThemeDescriptor GetDescriptor() => new(new ThemeManifest(
            "Themes.Test",
            "Test",
            "1.0.0",
            "Test",
            "Test theme",
            new ThemeAssetReferences([], []),
            [new ThemeSettingDefinition("primaryColor", "Primary", "color", "#000000")],
            [new ThemeLayoutDefinition(ThemeLayoutType.Homepage, [ThemeLayoutZoneNames.Header], false)]));
    }
}
