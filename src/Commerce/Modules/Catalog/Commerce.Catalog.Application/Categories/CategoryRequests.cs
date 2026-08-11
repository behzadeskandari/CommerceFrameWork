namespace Commerce.Catalog.Application.Categories;

public sealed record CreateCategoryRequest(
    string Name,
    int? ParentCategoryId = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    int DisplayOrder = 0);

public sealed record UpdateCategoryRequest(
    string Name,
    int? ParentCategoryId = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    int DisplayOrder = 0);
