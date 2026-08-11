using Commerce.Framework.Core.Results;

namespace Commerce.Catalog.Contracts.Attributes;

public sealed record AttributeDefinitionDto(
    int Id,
    string Name,
    string Code,
    string AttributeType,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<AttributeOptionDto> Options);

public sealed record AttributeOptionDto(
    int Id,
    int AttributeDefinitionId,
    string Value,
    bool IsActive,
    int DisplayOrder);

public sealed record ProductAttributeAssignmentDto(
    int AttributeDefinitionId,
    string AttributeCode,
    string AttributeName,
    string AttributeType,
    IReadOnlyList<AttributeOptionDto> Options);

public interface IProductAttributeReader
{
    Task<Result<AttributeDefinitionDto>> GetByIdAsync(int attributeId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AttributeDefinitionDto>>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProductAttributeAssignmentDto>>> GetForProductAsync(
        int productId,
        CancellationToken cancellationToken = default);
}
