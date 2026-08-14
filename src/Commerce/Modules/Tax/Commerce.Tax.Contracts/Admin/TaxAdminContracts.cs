using Commerce.Framework.Core.Results;
using Commerce.Tax.Domain.Enums;

namespace Commerce.Tax.Contracts.Admin;

public sealed record TaxCategorySummaryDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    bool IsExempt,
    bool IsActive,
    int DisplayOrder);

public sealed record TaxCategoryDetailDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    string? Description,
    bool IsExempt,
    bool IsActive,
    int DisplayOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateTaxCategoryRequest(
    int StoreId,
    string Name,
    string SystemName,
    string? Description,
    bool IsExempt,
    bool IsActive,
    int DisplayOrder);

public sealed record UpdateTaxCategoryRequest(
    string Name,
    string? Description,
    bool IsExempt,
    bool IsActive,
    int DisplayOrder);

public sealed record TaxZoneCountryDto(string CountryCode);

public sealed record TaxZoneStateDto(string CountryCode, string StateProvince);

public sealed record TaxZonePostalRuleDto(
    string CountryCode,
    PostalRuleType RuleType,
    string PostalFrom,
    string? PostalTo);

public sealed record TaxZoneSummaryDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder);

public sealed record TaxZoneDetailDto(
    int Id,
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<TaxZoneCountryDto> Countries,
    IReadOnlyList<TaxZoneStateDto> States,
    IReadOnlyList<TaxZonePostalRuleDto> PostalRules,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateTaxZoneRequest(
    int StoreId,
    string Name,
    string SystemName,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<TaxZoneCountryDto> Countries,
    IReadOnlyList<TaxZoneStateDto> States,
    IReadOnlyList<TaxZonePostalRuleDto> PostalRules);

public sealed record UpdateTaxZoneRequest(
    string Name,
    bool IsDefault,
    bool IsActive,
    int DisplayOrder,
    IReadOnlyList<TaxZoneCountryDto> Countries,
    IReadOnlyList<TaxZoneStateDto> States,
    IReadOnlyList<TaxZonePostalRuleDto> PostalRules);

public sealed record TaxRateSummaryDto(
    int Id,
    int StoreId,
    int TaxCategoryId,
    int? TaxZoneId,
    TaxRateType RateType,
    decimal Percentage,
    bool TaxShipping,
    int Priority,
    bool IsActive);

public sealed record TaxRateDetailDto(
    int Id,
    int StoreId,
    int TaxCategoryId,
    int? TaxZoneId,
    TaxRateType RateType,
    decimal Percentage,
    decimal? FixedAmount,
    bool TaxShipping,
    int Priority,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateTaxRateRequest(
    int StoreId,
    int TaxCategoryId,
    int? TaxZoneId,
    TaxRateType RateType,
    decimal Percentage,
    bool TaxShipping,
    int Priority,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc);

public sealed record UpdateTaxRateRequest(
    decimal Percentage,
    bool TaxShipping,
    int Priority,
    DateTime? EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    bool IsActive);

public interface ITaxAdminService
{
    Task<Result<IReadOnlyList<TaxCategorySummaryDto>>> ListCategoriesAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Result<TaxCategoryDetailDto>> GetCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<TaxCategoryDetailDto>> CreateCategoryAsync(CreateTaxCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<TaxCategoryDetailDto>> UpdateCategoryAsync(int id, UpdateTaxCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TaxZoneSummaryDto>>> ListZonesAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<Result<TaxZoneDetailDto>> GetZoneAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<TaxZoneDetailDto>> CreateZoneAsync(CreateTaxZoneRequest request, CancellationToken cancellationToken = default);
    Task<Result<TaxZoneDetailDto>> UpdateZoneAsync(int id, UpdateTaxZoneRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteZoneAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TaxRateSummaryDto>>> ListRatesAsync(int? storeId, int? categoryId, CancellationToken cancellationToken = default);
    Task<Result<TaxRateDetailDto>> GetRateAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<TaxRateDetailDto>> CreateRateAsync(CreateTaxRateRequest request, CancellationToken cancellationToken = default);
    Task<Result<TaxRateDetailDto>> UpdateRateAsync(int id, UpdateTaxRateRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRateAsync(int id, CancellationToken cancellationToken = default);
}
