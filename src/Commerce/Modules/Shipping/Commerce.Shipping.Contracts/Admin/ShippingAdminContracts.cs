using Commerce.Framework.Core.Results;
using Commerce.Shipping.Contracts.Shipments;
using Commerce.Shipping.Domain.Enums;

namespace Commerce.Shipping.Contracts.Admin;

public sealed record ShippingMethodSummaryDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    string ProviderSystemName,
    bool IsActive,
    int DisplayOrder);

public sealed record ShippingMethodDetailDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    string? Description,
    string ProviderSystemName,
    bool IsActive,
    int DisplayOrder,
    bool RequiresAddress,
    bool SupportsTracking,
    int? EstimatedDeliveryDaysMin,
    int? EstimatedDeliveryDaysMax,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateShippingMethodRequest(
    int StoreId,
    string Name,
    string SystemName,
    string? Description,
    string ProviderSystemName,
    bool IsActive,
    int DisplayOrder,
    bool RequiresAddress,
    bool SupportsTracking,
    int? EstimatedDeliveryDaysMin,
    int? EstimatedDeliveryDaysMax);

public sealed record UpdateShippingMethodRequest(
    string Name,
    string? Description,
    bool IsActive,
    int DisplayOrder,
    bool RequiresAddress,
    bool SupportsTracking,
    int? EstimatedDeliveryDaysMin,
    int? EstimatedDeliveryDaysMax);

public sealed record ShippingZoneCountryDto(string CountryCode);

public sealed record ShippingZoneStateDto(string CountryCode, string StateProvince);

public sealed record ShippingZonePostalRuleDto(
    string CountryCode,
    PostalRuleType RuleType,
    string PostalFrom,
    string? PostalTo);

public sealed record ShippingZoneSummaryDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder);

public sealed record ShippingZoneDetailDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<ShippingZoneCountryDto> Countries,
    IReadOnlyList<ShippingZoneStateDto> States,
    IReadOnlyList<ShippingZonePostalRuleDto> PostalRules,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateShippingZoneRequest(
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<ShippingZoneCountryDto> Countries,
    IReadOnlyList<ShippingZoneStateDto> States,
    IReadOnlyList<ShippingZonePostalRuleDto> PostalRules);

public sealed record UpdateShippingZoneRequest(
    string Name,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<ShippingZoneCountryDto> Countries,
    IReadOnlyList<ShippingZoneStateDto> States,
    IReadOnlyList<ShippingZonePostalRuleDto> PostalRules);

public sealed record ShippingRateSummaryDto(
    int Id,
    int StoreId,
    int ShippingMethodId,
    int? ShippingZoneId,
    string CurrencyCode,
    ShippingRateType RateType,
    decimal BasePrice,
    bool IsActive);

public sealed record ShippingRateDetailDto(
    int Id,
    int StoreId,
    int ShippingMethodId,
    int? ShippingZoneId,
    string CurrencyCode,
    ShippingRateType RateType,
    decimal BasePrice,
    decimal? PricePerWeightUnit,
    decimal? PricePerQuantityUnit,
    decimal? OrderSubtotalPercentage,
    decimal? FreeShippingThreshold,
    decimal? MinOrderSubtotal,
    decimal? MaxOrderSubtotal,
    decimal? MinWeightGrams,
    decimal? MaxWeightGrams,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateShippingRateRequest(
    int StoreId,
    int ShippingMethodId,
    int? ShippingZoneId,
    string CurrencyCode,
    ShippingRateType RateType,
    decimal BasePrice,
    decimal? PricePerWeightUnit,
    decimal? PricePerQuantityUnit,
    decimal? OrderSubtotalPercentage,
    decimal? FreeShippingThreshold,
    decimal? MinOrderSubtotal,
    decimal? MaxOrderSubtotal,
    decimal? MinWeightGrams,
    decimal? MaxWeightGrams);

public sealed record UpdateShippingRateRequest(
    decimal BasePrice,
    decimal? PricePerWeightUnit,
    decimal? PricePerQuantityUnit,
    decimal? OrderSubtotalPercentage,
    decimal? FreeShippingThreshold,
    decimal? MinOrderSubtotal,
    decimal? MaxOrderSubtotal,
    decimal? MinWeightGrams,
    decimal? MaxWeightGrams,
    bool IsActive);

public interface IShippingAdminService
{
    Task<Result<IReadOnlyList<ShippingMethodSummaryDto>>> ListMethodsAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Result<ShippingMethodDetailDto>> GetMethodAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ShippingMethodDetailDto>> CreateMethodAsync(CreateShippingMethodRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShippingMethodDetailDto>> UpdateMethodAsync(int id, UpdateShippingMethodRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteMethodAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ShippingZoneSummaryDto>>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Result<ShippingZoneDetailDto>> GetZoneAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ShippingZoneDetailDto>> CreateZoneAsync(CreateShippingZoneRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShippingZoneDetailDto>> UpdateZoneAsync(int id, UpdateShippingZoneRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteZoneAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ShippingRateSummaryDto>>> ListRatesAsync(int? storeId, int? methodId, CancellationToken cancellationToken = default);
    Task<Result<ShippingRateDetailDto>> GetRateAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<ShippingRateDetailDto>> CreateRateAsync(CreateShippingRateRequest request, CancellationToken cancellationToken = default);
    Task<Result<ShippingRateDetailDto>> UpdateRateAsync(int id, UpdateShippingRateRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRateAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<ShippingSettingsDto>> GetSettingsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task<Result<ShippingSettingsDto>> UpdateSettingsAsync(int? storeId, UpdateShippingSettingsRequest request, CancellationToken cancellationToken = default);
}
