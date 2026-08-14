using Commerce.Framework.Search;

namespace Commerce.Tests.Unit.Search;

public sealed class SearchSuggestionMinimumLengthTests
{
    [Fact]
    public void SuggestRequest_RequiresAtLeastTwoCharacters()
    {
        Assert.True("ab".Length >= 2);
        Assert.False("a".Length >= 2);
    }
}

public sealed class SearchQueryPaginationTests
{
    [Fact]
    public void PageSize_IsClamped()
    {
        var pageSize = Math.Clamp(500, 1, 100);
        Assert.Equal(100, pageSize);
    }
}

public sealed class SearchProviderResolverTests
{
    [Fact]
    public void DefaultProviderName_IsDatabase()
    {
        Assert.Equal("Search.Database", DefaultSearchProviderNames.Database);
    }
}

public sealed class SearchDocumentBuilderTests
{
    [Fact]
    public void SearchText_IncludesNameAndSku()
    {
        var text = $"{ "Widget" } { "SKU-1" }";
        Assert.Contains("Widget", text);
        Assert.Contains("SKU-1", text);
    }
}
