using Commerce.Framework.Core.Results;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Contracts.Pricing;

public sealed record PromotionCartLineContext(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice,
    IReadOnlyList<int> CategoryIds);

public sealed record PromotionEvaluationContext(
    int StoreId,
    string CurrencyCode,
    int? CustomerId,
    int? CustomerGroupId,
    bool IsGuest,
    decimal CartSubtotal,
    int TotalQuantity,
    IReadOnlyList<PromotionCartLineContext> Lines,
    int? OfferId,
    int? ProductId,
    int? VariantId,
    int LineQuantity,
    decimal LineSubtotal,
    IReadOnlyList<int> LineCategoryIds,
    string? CouponCode,
    DateTime CurrentTimeUtc);

public sealed record PromotionDiscountEffect(
    int PromotionId,
    string Name,
    decimal Amount,
    PromotionTargetScope Scope,
    PromotionCombinationRule CombinationRule,
    string? CombinationGroup,
    int Priority,
    int? OfferId = null);

public interface IPromotionEvaluationService
{
    Task<IReadOnlyList<PromotionDiscountEffect>> EvaluateLinePromotionsAsync(
        PromotionEvaluationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionDiscountEffect>> EvaluateCartPromotionsAsync(
        PromotionEvaluationContext context,
        CancellationToken cancellationToken = default);
}
