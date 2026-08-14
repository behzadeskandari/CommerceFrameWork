namespace Commerce.Pricing.Contracts.AdvancedPricing;

public sealed record ProductPricingContext(
    int StoreId,
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    string CurrencyCode,
    int CurrencyId,
    decimal BaseUnitPrice,
    decimal? CompareAtPrice,
    int? CustomerId,
    int? CustomerGroupId,
    DateTime CurrentTimeUtc);

public sealed record ProductPricingResult(
    decimal BaseUnitPrice,
    decimal AdjustedUnitPrice,
    decimal? CompareAtPrice,
    string CurrencyCode,
    bool TierPriceApplied,
    bool CustomerGroupPriceApplied,
    bool CurrencyConverted,
    decimal? ExchangeRate);

public interface IProductPricingPipeline
{
    Task<ProductPricingResult> ResolveUnitPriceAsync(
        ProductPricingContext context,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerGroupDto(
    int Id,
    int StoreId,
    string Name,
    string Code,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CustomerGroupPriceDto(
    int Id,
    int CustomerGroupId,
    int StoreId,
    int ProductId,
    int? VariantId,
    int CurrencyId,
    string CurrencyCode,
    decimal Price,
    bool IsActive);

public sealed record CreateCustomerGroupRequest(
    int StoreId,
    string Name,
    string Code,
    bool IsActive = true,
    int DisplayOrder = 0);

public sealed record UpdateCustomerGroupRequest(
    string Name,
    string Code,
    bool IsActive,
    int DisplayOrder);

public sealed record CreateCustomerGroupPriceRequest(
    int CustomerGroupId,
    int StoreId,
    int ProductId,
    int? VariantId,
    int CurrencyId,
    string CurrencyCode,
    decimal Price,
    bool IsActive = true);

public sealed record UpdateCustomerGroupPriceRequest(
    decimal Price,
    bool IsActive);

public sealed record PricePreviewRequest(
    int OfferId,
    int Quantity,
    int? CustomerId,
    int? CustomerGroupId,
    string CurrencyCode);

public sealed record PricePreviewResult(
    decimal BaseUnitPrice,
    decimal AdjustedUnitPrice,
    decimal? CompareAtPrice,
    decimal? FinalUnitPrice,
    decimal? DiscountAmount,
    string CurrencyCode,
    bool TierPriceApplied,
    bool CustomerGroupPriceApplied,
    bool CurrencyConverted);

public interface ICustomerGroupAdminService
{
    Task<IReadOnlyList<CustomerGroupDto>> ListGroupsAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<CustomerGroupDto?> GetGroupAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerGroupDto> CreateGroupAsync(CreateCustomerGroupRequest request, CancellationToken cancellationToken = default);
    Task<CustomerGroupDto> UpdateGroupAsync(int id, UpdateCustomerGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteGroupAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerGroupPriceDto>> ListGroupPricesAsync(int groupId, CancellationToken cancellationToken = default);
    Task<CustomerGroupPriceDto> AddGroupPriceAsync(CreateCustomerGroupPriceRequest request, CancellationToken cancellationToken = default);
    Task<CustomerGroupPriceDto> UpdateGroupPriceAsync(int id, UpdateCustomerGroupPriceRequest request, CancellationToken cancellationToken = default);
    Task DeleteGroupPriceAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAdvancedPricingService
{
    Task<PricePreviewResult> PreviewAsync(PricePreviewRequest request, CancellationToken cancellationToken = default);
}
