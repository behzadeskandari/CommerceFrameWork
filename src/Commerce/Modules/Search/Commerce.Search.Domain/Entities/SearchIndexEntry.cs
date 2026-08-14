using Commerce.Framework.Core.Entities;

namespace Commerce.Search.Domain.Entities;

public sealed class SearchIndexEntry : AggregateRoot
{
    public const int TextMaxLength = 4000;
    public const int JsonMaxLength = 8000;

    public int ProductId { get; private set; }
    public int StoreId { get; private set; }
    public int LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string? Slug { get; private set; }
    public string? Description { get; private set; }
    public string? ShortDescription { get; private set; }
    public string? Manufacturer { get; private set; }
    public string ProductType { get; private set; } = string.Empty;
    public decimal? Price { get; private set; }
    public bool Published { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsAvailable { get; private set; }
    public double PopularityScore { get; private set; }
    public double? Rating { get; private set; }
    public bool IsDeleted { get; private set; }
    public string SearchText { get; private set; } = string.Empty;
    public string CategoryIdsJson { get; private set; } = "[]";
    public string CategoryNamesJson { get; private set; } = "[]";
    public string TagsJson { get; private set; } = "[]";
    public string AttributesJson { get; private set; } = "{}";
    public DateTime ProductCreatedAtUtc { get; private set; }
    public DateTime ProductUpdatedAtUtc { get; private set; }
    public DateTime IndexedAtUtc { get; private set; }

    public static SearchIndexEntry Create(
        int productId,
        int storeId,
        int languageId,
        string name,
        string sku,
        string? slug,
        string? description,
        string? shortDescription,
        string? manufacturer,
        string productType,
        decimal? price,
        bool published,
        bool isVisible,
        bool isAvailable,
        double popularityScore,
        double? rating,
        bool isDeleted,
        string searchText,
        string categoryIdsJson,
        string categoryNamesJson,
        string tagsJson,
        string attributesJson,
        DateTime productCreatedAtUtc,
        DateTime productUpdatedAtUtc)
    {
        return new SearchIndexEntry
        {
            ProductId = productId,
            StoreId = storeId,
            LanguageId = languageId,
            Name = name.Trim(),
            Sku = sku.Trim(),
            Slug = slug?.Trim(),
            Description = description,
            ShortDescription = shortDescription,
            Manufacturer = manufacturer,
            ProductType = productType,
            Price = price,
            Published = published,
            IsVisible = isVisible,
            IsAvailable = isAvailable,
            PopularityScore = popularityScore,
            Rating = rating,
            IsDeleted = isDeleted,
            SearchText = Truncate(searchText, TextMaxLength),
            CategoryIdsJson = Truncate(categoryIdsJson, JsonMaxLength),
            CategoryNamesJson = Truncate(categoryNamesJson, JsonMaxLength),
            TagsJson = Truncate(tagsJson, JsonMaxLength),
            AttributesJson = Truncate(attributesJson, JsonMaxLength),
            ProductCreatedAtUtc = productCreatedAtUtc,
            ProductUpdatedAtUtc = productUpdatedAtUtc,
            IndexedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateFrom(SearchIndexEntry source)
    {
        Name = source.Name;
        Sku = source.Sku;
        Slug = source.Slug;
        Description = source.Description;
        ShortDescription = source.ShortDescription;
        Manufacturer = source.Manufacturer;
        ProductType = source.ProductType;
        Price = source.Price;
        Published = source.Published;
        IsVisible = source.IsVisible;
        IsAvailable = source.IsAvailable;
        PopularityScore = source.PopularityScore;
        Rating = source.Rating;
        IsDeleted = source.IsDeleted;
        SearchText = source.SearchText;
        CategoryIdsJson = source.CategoryIdsJson;
        CategoryNamesJson = source.CategoryNamesJson;
        TagsJson = source.TagsJson;
        AttributesJson = source.AttributesJson;
        ProductCreatedAtUtc = source.ProductCreatedAtUtc;
        ProductUpdatedAtUtc = source.ProductUpdatedAtUtc;
        IndexedAtUtc = DateTime.UtcNow;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
