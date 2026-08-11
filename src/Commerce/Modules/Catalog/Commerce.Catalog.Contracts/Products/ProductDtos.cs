using Commerce.Catalog.Contracts.Media;

namespace Commerce.Catalog.Contracts.Products;

public sealed record ProductSummaryDto(
    int Id,
    string Name,
    string Sku,
    string ProductType,
    bool Published,
    bool IsVisible,
    bool IsAvailable,
    bool Deleted,
    int DisplayOrder,
    string? Slug,
    ProductMediaSummaryDto? PrimaryImage = null);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string? ShortDescription,
    string? Description,
    string Sku,
    string ProductType,
    bool Published,
    bool IsVisible,
    bool IsAvailable,
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

public sealed record StorefrontProductDetailDto(
    int Id,
    string Name,
    string? ShortDescription,
    string? Description,
    string Sku,
    string ProductType,
    string? Slug,
    IReadOnlyList<int> CategoryIds,
    IReadOnlyList<ProductAttributeAssignmentSummaryDto> ConfigurableAttributes,
    IReadOnlyList<StorefrontVariantDto> Variants,
    int? DefaultVariantId,
    ResolvedPriceSummaryDto? Price,
    StorefrontMediaDto? PrimaryImage = null,
    IReadOnlyList<StorefrontMediaDto>? Gallery = null);

public sealed record StorefrontVariantDto(
    int Id,
    string Sku,
    string Name,
    bool IsDefault,
    IReadOnlyList<StorefrontAttributeOptionDto> Options,
    StorefrontMediaDto? Image = null);

public sealed record ProductAttributeAssignmentSummaryDto(
    int AttributeDefinitionId,
    string Code,
    string Name,
    IReadOnlyList<StorefrontAttributeOptionDto> Options);

public sealed record StorefrontAttributeOptionDto(int Id, string Value);

public sealed record StorefrontAvailabilityDto(
    string Status,
    bool CanPurchase,
    bool IsBackorder);

public sealed record ResolvedPriceSummaryDto(
    int OfferId,
    string CurrencyCode,
    decimal UnitPrice,
    decimal? CompareAtPrice,
    StorefrontAvailabilityDto? Availability = null);
