using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Contracts.Variants;

public sealed record VariantSummaryDto(
    int Id,
    int ProductId,
    string Sku,
    string Name,
    bool IsActive,
    bool IsDefault,
    int DisplayOrder,
    string AttributeCombinationKey);

public sealed record VariantDetailDto(
    int Id,
    int ProductId,
    string Sku,
    string Name,
    bool IsActive,
    bool IsDefault,
    int DisplayOrder,
    string AttributeCombinationKey,
    IReadOnlyList<VariantAttributeDto> Attributes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record VariantAttributeDto(
    int AttributeOptionId,
    int AttributeDefinitionId,
    string AttributeCode,
    string AttributeName,
    string OptionValue);

public interface IProductVariantReader
{
    Task<Result<VariantDetailDto>> GetByIdAsync(int variantId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<VariantSummaryDto>>> ListForProductAsync(
        int productId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
