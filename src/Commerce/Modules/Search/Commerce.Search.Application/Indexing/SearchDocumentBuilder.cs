using System.Text;
using System.Text.Json;
using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Contracts.Categories;
using Commerce.Catalog.Contracts.Products;
using Commerce.Framework.Search;
using Commerce.Search.Domain.Entities;
using Commerce.Store.Contracts.Stores;

namespace Commerce.Search.Application.Indexing;

public sealed class SearchDocumentBuilder(
    IProductReader productReader,
    ICategoryReader categoryReader,
    IProductOfferRepository offerRepository,
    IStoreReader storeReader)
{
    public async Task<IReadOnlyList<SearchDocument>> BuildForProductAsync(int productId, CancellationToken cancellationToken)
    {
        var productResult = await productReader.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (!productResult.IsSuccess || productResult.Value is null || productResult.Value.Deleted)
        {
            return [];
        }

        var product = productResult.Value;
        var storesResult = await storeReader.ListAsync(includeInactive: false, cancellationToken).ConfigureAwait(false);
        if (!storesResult.IsSuccess || storesResult.Value is null)
        {
            return [];
        }

        var categoryNames = await ResolveCategoryNamesAsync(product.CategoryIds, cancellationToken).ConfigureAwait(false);
        var attributes = product.Attributes.ToDictionary(x => x.AttributeCode, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var documents = new List<SearchDocument>();

        foreach (var store in storesResult.Value)
        {
            var offer = await offerRepository.FindActiveOfferAsync(
                product.Id,
                variantId: null,
                store.Id,
                store.DefaultCurrencyId,
                DateTime.UtcNow,
                cancellationToken).ConfigureAwait(false);

            var searchText = BuildSearchText(product, categoryNames, attributes);
            documents.Add(new SearchDocument(
                product.Id,
                store.Id,
                store.DefaultLanguageId,
                product.Name,
                product.Sku,
                product.Slug,
                product.Description,
                product.ShortDescription,
                product.CategoryIds,
                categoryNames,
                Manufacturer: null,
                Tags: [],
                attributes,
                product.ProductType,
                offer?.Price,
                product.Published,
                product.IsVisible,
                product.IsAvailable,
                PopularityScore: 0,
                Rating: null,
                product.CreatedAtUtc,
                product.UpdatedAtUtc,
                product.Deleted,
                searchText));
        }

        return documents;
    }

    public async Task<IReadOnlyList<SearchDocument>> BuildAllAsync(CancellationToken cancellationToken)
    {
        var productsResult = await productReader.ListAsync(includeDeleted: false, cancellationToken).ConfigureAwait(false);
        if (!productsResult.IsSuccess || productsResult.Value is null)
        {
            return [];
        }

        var documents = new List<SearchDocument>();
        foreach (var summary in productsResult.Value)
        {
            documents.AddRange(await BuildForProductAsync(summary.Id, cancellationToken).ConfigureAwait(false));
        }

        return documents;
    }

    private async Task<IReadOnlyList<string>> ResolveCategoryNamesAsync(IReadOnlyList<int> categoryIds, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        foreach (var categoryId in categoryIds)
        {
            var result = await categoryReader.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                names.Add(result.Value.Name);
            }
        }

        return names;
    }

    private static string BuildSearchText(
        ProductDetailDto product,
        IReadOnlyList<string> categoryNames,
        IReadOnlyDictionary<string, string> attributes)
    {
        var builder = new StringBuilder();
        builder.Append(product.Name).Append(' ');
        builder.Append(product.Sku).Append(' ');
        if (!string.IsNullOrWhiteSpace(product.Slug)) builder.Append(product.Slug).Append(' ');
        if (!string.IsNullOrWhiteSpace(product.ShortDescription)) builder.Append(product.ShortDescription).Append(' ');
        if (!string.IsNullOrWhiteSpace(product.Description)) builder.Append(product.Description).Append(' ');
        foreach (var name in categoryNames) builder.Append(name).Append(' ');
        foreach (var pair in attributes) builder.Append(pair.Key).Append(' ').Append(pair.Value).Append(' ');
        return builder.ToString().Trim();
    }

    public static SearchIndexEntry ToEntry(SearchDocument document) =>
        Search.Domain.Entities.SearchIndexEntry.Create(
            document.ProductId,
            document.StoreId,
            document.LanguageId,
            document.Name,
            document.Sku,
            document.Slug,
            document.Description,
            document.ShortDescription,
            document.Manufacturer,
            document.ProductType,
            document.Price,
            document.Published,
            document.IsVisible,
            document.IsAvailable,
            document.PopularityScore,
            document.Rating,
            document.IsDeleted,
            document.SearchText,
            JsonSerializer.Serialize(document.CategoryIds),
            JsonSerializer.Serialize(document.CategoryNames),
            JsonSerializer.Serialize(document.Tags),
            JsonSerializer.Serialize(document.Attributes),
            document.CreatedAtUtc,
            document.UpdatedAtUtc);
}
