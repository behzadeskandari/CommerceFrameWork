using Commerce.Cms.Application.Security;
using Commerce.Cms.Domain.Entities;

namespace Commerce.Tests.Unit.Cms;

public sealed class ContentHtmlSanitizerTests
{
    private readonly ContentHtmlSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_RemovesScriptTags()
    {
        var result = _sanitizer.Sanitize("<p>Hello</p><script>alert(1)</script>");
        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Hello</p>", result);
    }

    [Fact]
    public void Sanitize_RemovesEventHandlers()
    {
        var result = _sanitizer.Sanitize("<a href=\"/test\" onclick=\"alert(1)\">Link</a>");
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_RemovesIframe()
    {
        var result = _sanitizer.Sanitize("<iframe src=\"http://evil.com\"></iframe><p>Safe</p>");
        Assert.DoesNotContain("iframe", result, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ContentPageSlugTests
{
    [Fact]
    public void NormalizeSlug_LowercasesValue()
    {
        Assert.Equal("about-us", ContentPageLocalization.NormalizeSlug("About-Us"));
    }

    [Fact]
    public void NormalizeSlug_RejectsPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => ContentPageLocalization.NormalizeSlug("../admin"));
        Assert.Throws<ArgumentException>(() => ContentPageLocalization.NormalizeSlug("foo/bar"));
    }
}

public sealed class ContentPageVisibilityTests
{
    [Fact]
    public void IsVisible_RespectsPublishSchedule()
    {
        var page = ContentPage.Create(1, "about", isPublished: true, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.True(page.IsVisible(DateTime.UtcNow));
        Assert.False(page.IsVisible(DateTime.UtcNow.AddDays(2)));
    }

    [Fact]
    public void IsVisible_ReturnsFalseWhenUnpublished()
    {
        var page = ContentPage.Create(1, "about", isPublished: false, null, null);
        Assert.False(page.IsVisible(DateTime.UtcNow));
    }
}

public sealed class TopicSystemNameTests
{
    [Fact]
    public void Create_NormalizesSystemName()
    {
        var topic = Topic.Create(1, "Footer-Notice", true, null, null);
        Assert.Equal("footer-notice", topic.SystemName);
    }
}
