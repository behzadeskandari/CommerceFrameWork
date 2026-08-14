using Commerce.Pricing.Domain.Enums;
using Commerce.Framework.Core.Results;

namespace Commerce.Pricing.Contracts.Discounts;

public sealed record DiscountTargetDto(
    DiscountTargetType TargetType,
    int TargetId);

public sealed record DiscountSummaryDto(
    int Id,
    string Name,
    string SystemName,
    DiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    int Priority,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    DiscountApplicationScope ApplicationScope);

public sealed record DiscountDetailDto(
    int Id,
    string Name,
    string SystemName,
    string? Description,
    DiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    int Priority,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    StackingMode StackingMode,
    decimal? MaximumDiscountAmount,
    decimal? MinimumCartSubtotal,
    int? MinimumQuantity,
    CustomerEligibility CustomerEligibility,
    int? SpecificCustomerId,
    int? CustomerGroupId,
    DiscountApplicationScope ApplicationScope,
    IReadOnlyList<DiscountTargetDto> Targets,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateDiscountRequest(
    string Name,
    string SystemName,
    string? Description,
    DiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    int Priority,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    StackingMode StackingMode,
    decimal? MaximumDiscountAmount,
    decimal? MinimumCartSubtotal,
    int? MinimumQuantity,
    CustomerEligibility CustomerEligibility,
    int? SpecificCustomerId,
    int? CustomerGroupId,
    DiscountApplicationScope ApplicationScope,
    IReadOnlyList<DiscountTargetDto> Targets);

public sealed record UpdateDiscountRequest(
    string Name,
    string? Description,
    DiscountType DiscountType,
    decimal Value,
    string? CurrencyCode,
    int Priority,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    StackingMode StackingMode,
    decimal? MaximumDiscountAmount,
    decimal? MinimumCartSubtotal,
    int? MinimumQuantity,
    CustomerEligibility CustomerEligibility,
    int? SpecificCustomerId,
    int? CustomerGroupId,
    DiscountApplicationScope ApplicationScope,
    IReadOnlyList<DiscountTargetDto> Targets);

public sealed record CouponSummaryDto(
    int Id,
    string Code,
    int DiscountId,
    string DiscountName,
    bool IsActive,
    int UsageCount,
    int? GlobalUsageLimit,
    int? PerCustomerUsageLimit,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId);

public sealed record CouponDetailDto(
    int Id,
    string Code,
    int DiscountId,
    string DiscountName,
    bool IsActive,
    int UsageCount,
    int? GlobalUsageLimit,
    int? PerCustomerUsageLimit,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateCouponRequest(
    string Code,
    int DiscountId,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    int? GlobalUsageLimit,
    int? PerCustomerUsageLimit);

public sealed record UpdateCouponRequest(
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    int? GlobalUsageLimit,
    int? PerCustomerUsageLimit);

public interface IDiscountAdminService
{
    Task<Result<IReadOnlyList<DiscountSummaryDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<DiscountDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<DiscountDetailDto>> CreateAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default);

    Task<Result<DiscountDetailDto>> UpdateAsync(int id, UpdateDiscountRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public interface ICouponAdminService
{
    Task<Result<IReadOnlyList<CouponSummaryDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<CouponDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<CouponDetailDto>> CreateAsync(CreateCouponRequest request, CancellationToken cancellationToken = default);

    Task<Result<CouponDetailDto>> UpdateAsync(int id, UpdateCouponRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
