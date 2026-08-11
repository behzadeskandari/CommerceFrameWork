namespace Commerce.Catalog.Contracts.Categories;

public sealed record CategorySummaryDto(
    int Id,
    string Name,
    int? ParentCategoryId,
    bool Published,
    int DisplayOrder,
    string? Slug);

public sealed record CategoryDetailDto(
    int Id,
    string Name,
    string? Description,
    int? ParentCategoryId,
    bool Published,
    int DisplayOrder,
    string? Slug,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<int> ChildCategoryIds,
    IReadOnlyList<int> ProductIds);
