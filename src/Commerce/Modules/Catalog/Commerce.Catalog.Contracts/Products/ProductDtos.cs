namespace Commerce.Catalog.Contracts.Products;

public sealed record ProductSummaryDto(
    int Id,
    string Name,
    string Sku,
    string ProductType,
    bool Published,
    bool Deleted,
    int DisplayOrder,
    string? Slug);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string? ShortDescription,
    string? Description,
    string Sku,
    string ProductType,
    bool Published,
    bool Deleted,
    int DisplayOrder,
    string? Slug,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<int> CategoryIds,
    IReadOnlyList<ProductAttributeValueDto> Attributes);

public sealed record ProductAttributeValueDto(
    int AttributeDefinitionId,
    string AttributeCode,
    string AttributeName,
    string Value);
