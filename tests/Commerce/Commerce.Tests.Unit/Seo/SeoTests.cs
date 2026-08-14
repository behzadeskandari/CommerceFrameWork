using Commerce.Framework.Seo;
using Commerce.Seo.Domain.Entities;
using Xunit;

namespace Commerce.Tests.Unit.Seo;

public sealed class SeoTests
{
    [Fact]
    public void SlugNormalizer_RejectsPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => SlugNormalizer.Normalize("../admin"));
        Assert.Throws<ArgumentException>(() => SlugNormalizer.Normalize("products/../secret"));
    }

    [Fact]
    public void SlugNormalizer_NormalizesCaseAndWhitespace()
    {
        Assert.Equal("blue-widget", SlugNormalizer.Normalize("  Blue-Widget "));
    }

    [Fact]
    public void SlugFromTitle_ReplacesNonAlphanumericWithHyphens()
    {
        Assert.Equal("summer-sale-2026", SlugNormalizer.FromTitle("Summer Sale 2026!"));
    }

    [Fact]
    public void UrlRecord_Create_NormalizesSlug()
    {
        var record = UrlRecord.Create("Product", 42, "  Cool-Gadget ", languageId: 1, storeId: 1, isActive: true);
        Assert.Equal("cool-gadget", record.Slug);
        Assert.Equal("Product", record.EntityName);
    }

    [Fact]
    public void SeoSettings_Update_PersistsRobotsAndSitemapFlag()
    {
        var settings = SeoSettings.CreateDefault(1);
        settings.Update(
            defaultMetaTitle: "Shop",
            defaultMetaDescription: "Welcome",
            robotsTxt: "User-agent: *\nDisallow: /admin",
            sitemapEnabled: false);

        Assert.False(settings.SitemapEnabled);
        Assert.Contains("Disallow: /admin", settings.RobotsTxt);
    }
}
