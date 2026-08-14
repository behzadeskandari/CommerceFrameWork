using Commerce.Framework.Core.Entities;

namespace Commerce.Seo.Domain.Entities;

public sealed class SeoSettings : AggregateRoot
{
    public const int RobotsTxtMaxLength = 8000;
    public const int DefaultTitleMaxLength = 200;
    public const int DefaultDescriptionMaxLength = 1000;

    public int StoreId { get; private set; }

    public string? DefaultMetaTitle { get; private set; }

    public string? DefaultMetaDescription { get; private set; }

    public string? RobotsTxt { get; private set; }

    public bool SitemapEnabled { get; private set; }

    public static SeoSettings CreateDefault(int storeId) =>
        new()
        {
            StoreId = storeId,
            DefaultMetaTitle = null,
            DefaultMetaDescription = null,
            RobotsTxt = "User-agent: *\nAllow: /",
            SitemapEnabled = true
        };

    public void Update(string? defaultMetaTitle, string? defaultMetaDescription, string? robotsTxt, bool sitemapEnabled)
    {
        DefaultMetaTitle = defaultMetaTitle;
        DefaultMetaDescription = defaultMetaDescription;
        RobotsTxt = robotsTxt;
        SitemapEnabled = sitemapEnabled;
    }
}
