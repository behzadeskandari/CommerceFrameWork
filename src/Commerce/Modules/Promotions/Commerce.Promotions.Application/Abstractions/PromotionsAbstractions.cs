using Commerce.Promotions.Domain.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Application.Abstractions;

public interface IPromotionsRepository
{
    Task<IReadOnlyList<Promotion>> GetActivePromotionsAsync(int storeId, DateTime utcNow, CancellationToken cancellationToken = default);

    Task<Promotion?> GetPromotionByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Promotion>> ListPromotionsAsync(int? storeId, CancellationToken cancellationToken = default);

    Task AddPromotionAsync(Promotion promotion, CancellationToken cancellationToken = default);

    Task SavePromotionAsync(Promotion promotion, CancellationToken cancellationToken = default);

    Task DeletePromotionAsync(Promotion promotion, CancellationToken cancellationToken = default);

    Task<int> GetCustomerUsageCountAsync(int promotionId, int customerId, CancellationToken cancellationToken = default);

    Task AddUsageAsync(PromotionUsage usage, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IPromotionConditionEvaluator
{
    PromotionConditionType ConditionType { get; }

    bool Evaluate(PromotionCondition condition, PromotionEvaluationState state);
}

public interface IPromotionActionExecutor
{
    PromotionActionType ActionType { get; }

    PromotionDiscountEffect? Execute(PromotionAction action, Promotion promotion, PromotionEvaluationState state);
}

public sealed record PromotionEvaluationState
{
    public required int StoreId { get; init; }

    public required string CurrencyCode { get; init; }

    public int? CustomerId { get; init; }

    public int? CustomerGroupId { get; init; }

    public bool IsGuest { get; init; }

    public decimal CartSubtotal { get; init; }

    public int TotalQuantity { get; init; }

    public IReadOnlyList<PromotionCartLineState> Lines { get; init; } = [];

    public int? OfferId { get; init; }

    public int? ProductId { get; init; }

    public int? VariantId { get; init; }

    public int LineQuantity { get; init; }

    public decimal LineSubtotal { get; init; }

    public IReadOnlyList<int> LineCategoryIds { get; init; } = [];

    public string? CouponCode { get; init; }

    public DateTime CurrentTimeUtc { get; init; }

    public int PromotionUsageCount { get; init; }

    public int CustomerPromotionUsageCount { get; init; }

    public int? GlobalUsageLimit { get; init; }

    public int? PerCustomerUsageLimit { get; init; }
}

public sealed record PromotionCartLineState(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice,
    IReadOnlyList<int> CategoryIds);

public sealed record PromotionDiscountEffect(
    int PromotionId,
    string Name,
    decimal Amount,
    PromotionTargetScope Scope,
    PromotionCombinationRule CombinationRule,
    string? CombinationGroup,
    int Priority,
    int? OfferId = null);

public sealed record PromotionCartLineContext(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice,
    IReadOnlyList<int> CategoryIds);
