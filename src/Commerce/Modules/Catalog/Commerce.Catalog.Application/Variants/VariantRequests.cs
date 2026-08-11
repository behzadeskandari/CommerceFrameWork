namespace Commerce.Catalog.Application.Variants;

public sealed record CreateVariantRequest(
    int ProductId,
    string Sku,
    string Name,
    IReadOnlyList<int> AttributeOptionIds,
    bool IsActive = true,
    bool IsDefault = false,
    int DisplayOrder = 0);

public sealed record UpdateVariantRequest(
    string Name,
    IReadOnlyList<int> AttributeOptionIds,
    bool IsActive = true,
    bool IsDefault = false,
    int DisplayOrder = 0);

public sealed record GenerateVariantsRequest(
    int ProductId,
    string SkuPrefix,
    bool SkipExisting = true);
