using Commerce.Framework.Core.Results;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Contracts.Admin;

public sealed record PromotionConditionDto(
    int Id,
    PromotionConditionType ConditionType,
    string ParametersJson);

public sealed record PromotionActionDto(
    int Id,
    PromotionActionType ActionType,
    PromotionTargetScope TargetScope,
    string ParametersJson);

public sealed record PromotionSummaryDto(
    int Id,
    string Name,
    string SystemName,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    int Priority,
    PromotionCombinationRule CombinationRule,
    int UsageCount,
    int? GlobalUsageLimit);

public sealed record PromotionDetailDto(
    int Id,
    string Name,
    string SystemName,
    string? Description,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    int Priority,
    PromotionCombinationRule CombinationRule,
    string? CombinationGroup,
    int? GlobalUsageLimit,
    int? PerCustomerUsageLimit,
    int UsageCount,
    bool RequiresCouponCode,
    string? CouponCode,
    IReadOnlyList<PromotionConditionDto> Conditions,
    IReadOnlyList<PromotionActionDto> Actions,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record PromotionConditionRequest(
    PromotionConditionType ConditionType,
    string ParametersJson);

public sealed record PromotionActionRequest(
    PromotionActionType ActionType,
    PromotionTargetScope TargetScope,
    string ParametersJson);

public sealed record CreatePromotionRequest(
    string Name,
    string SystemName,
    string? Description,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    int Priority,
    PromotionCombinationRule CombinationRule,
    string? CombinationGroup,
    int? GlobalUsageLimit,
    int? PerCustomerUsageLimit,
    bool RequiresCouponCode,
    string? CouponCode,
    IReadOnlyList<PromotionConditionRequest> Conditions,
    IReadOnlyList<PromotionActionRequest> Actions);

public sealed record UpdatePromotionRequest(
    string Name,
    string? Description,
    bool IsActive,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? StoreId,
    int Priority,
    PromotionCombinationRule CombinationRule,
    string? CombinationGroup,
    int? GlobalUsageLimit,
    int? PerCustomerUsageLimit,
    bool RequiresCouponCode,
    string? CouponCode,
    IReadOnlyList<PromotionConditionRequest> Conditions,
    IReadOnlyList<PromotionActionRequest> Actions);

public interface IPromotionAdminService
{
    Task<Result<IReadOnlyList<PromotionSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default);

    Task<Result<PromotionDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<PromotionDetailDto>> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<Result<PromotionDetailDto>> UpdateAsync(int id, UpdatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
