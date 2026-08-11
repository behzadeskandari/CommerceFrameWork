using Commerce.Catalog.Domain.Enums;

namespace Commerce.Catalog.Application.Attributes;

public sealed record CreateAttributeDefinitionRequest(
    string Name,
    string Code,
    AttributeType AttributeType,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record UpdateAttributeDefinitionRequest(
    string Name,
    AttributeType AttributeType,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record CreateAttributeOptionRequest(
    int AttributeDefinitionId,
    string Value,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record UpdateAttributeOptionRequest(
    string Value,
    int DisplayOrder = 0,
    bool IsActive = true);

public sealed record AssignProductAttributeRequest(
    int ProductId,
    int AttributeDefinitionId,
    int DisplayOrder = 0);

public sealed record SetProductAttributeValueRequest(
    int ProductId,
    int AttributeDefinitionId,
    string Value);
