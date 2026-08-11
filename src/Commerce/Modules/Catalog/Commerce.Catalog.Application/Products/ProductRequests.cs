using Commerce.Catalog.Domain.Enums;

namespace Commerce.Catalog.Application.Products;

public sealed record CreateProductRequest(
    string Name,
    string Sku,
    ProductType ProductType,
    string? ShortDescription = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    bool IsVisible = true,
    bool IsAvailable = true,
    int DisplayOrder = 0,
    IReadOnlyList<int>? CategoryIds = null);

public sealed record UpdateProductRequest(
    string Name,
    ProductType ProductType,
    string? ShortDescription = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    bool IsVisible = true,
    bool IsAvailable = true,
    int DisplayOrder = 0,
    IReadOnlyList<int>? CategoryIds = null);

public sealed record AssignProductCategoryRequest(int ProductId, int CategoryId, int DisplayOrder = 0);
