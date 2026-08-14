using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Application.Pricing;
using Commerce.Pricing.Domain.Enums;
using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Application.Rules;
using Commerce.Promotions.Contracts.Pricing;
using ContractEffect = Commerce.Promotions.Contracts.Pricing.PromotionDiscountEffect;
using ContractContext = Commerce.Promotions.Contracts.Pricing.PromotionEvaluationContext;
using InternalEffect = Commerce.Promotions.Application.Abstractions.PromotionDiscountEffect;

namespace Commerce.Promotions.Application.Pricing;

public sealed class PromotionEvaluationService(
    IPromotionsRepository promotionsRepository,
    IPricingRepository pricingRepository,
    PromotionRuleEngine ruleEngine) : IPromotionEvaluationService
{
    public async Task<IReadOnlyList<ContractEffect>> EvaluateLinePromotionsAsync(
        ContractContext context,
        CancellationToken cancellationToken = default)
    {
        var state = await BuildStateAsync(context, cancellationToken).ConfigureAwait(false);
        var promotions = await promotionsRepository
            .GetActivePromotionsAsync(context.StoreId, context.CurrentTimeUtc, cancellationToken)
            .ConfigureAwait(false);

        var effects = new List<InternalEffect>();
        foreach (var promotion in promotions.OrderByDescending(x => x.Priority))
        {
            var promotionState = await BuildPromotionStateAsync(state, promotion, cancellationToken).ConfigureAwait(false);
            effects.AddRange(ruleEngine.Evaluate(promotion, promotionState, Domain.Enums.PromotionTargetScope.Line));
        }

        await AppendLinkedDiscountEffectsAsync(effects, promotions, state, Domain.Enums.PromotionTargetScope.Line, cancellationToken)
            .ConfigureAwait(false);

        return MapEffects(PromotionCombinationSelector.ApplyCombinationRules(effects));
    }

    public async Task<IReadOnlyList<ContractEffect>> EvaluateCartPromotionsAsync(
        ContractContext context,
        CancellationToken cancellationToken = default)
    {
        var state = await BuildStateAsync(context, cancellationToken).ConfigureAwait(false);
        var promotions = await promotionsRepository
            .GetActivePromotionsAsync(context.StoreId, context.CurrentTimeUtc, cancellationToken)
            .ConfigureAwait(false);

        var effects = new List<InternalEffect>();
        foreach (var promotion in promotions.OrderByDescending(x => x.Priority))
        {
            var promotionState = await BuildPromotionStateAsync(state, promotion, cancellationToken).ConfigureAwait(false);
            effects.AddRange(ruleEngine.Evaluate(promotion, promotionState, Domain.Enums.PromotionTargetScope.Cart));
        }

        await AppendLinkedDiscountEffectsAsync(effects, promotions, state, Domain.Enums.PromotionTargetScope.Cart, cancellationToken)
            .ConfigureAwait(false);

        return MapEffects(PromotionCombinationSelector.ApplyCombinationRules(effects));
    }

    private async Task AppendLinkedDiscountEffectsAsync(
        List<InternalEffect> effects,
        IReadOnlyList<Domain.Entities.Promotion> promotions,
        PromotionEvaluationState state,
        Domain.Enums.PromotionTargetScope scope,
        CancellationToken cancellationToken)
    {
        foreach (var promotion in promotions)
        {
            foreach (var action in promotion.Actions.Where(x =>
                         x.ActionType is Domain.Enums.PromotionActionType.ApplyLinkedDiscount &&
                         x.TargetScope == scope))
            {
                var discountId = PromotionParameterReader.ReadInt(action.ParametersJson, "discountId");
                if (discountId <= 0)
                {
                    continue;
                }

                var discount = await pricingRepository.GetDiscountByIdAsync(discountId, cancellationToken).ConfigureAwait(false);
                if (discount is null || !discount.IsCurrentlyValid(state.CurrentTimeUtc))
                {
                    continue;
                }

                var baseAmount = scope is Domain.Enums.PromotionTargetScope.Cart ? state.CartSubtotal : state.LineSubtotal;
                var amount = DiscountCalculationEngine.CalculateDiscountAmount(discount, baseAmount, state.CurrencyCode);
                if (amount <= 0)
                {
                    continue;
                }

                effects.Add(new InternalEffect(
                    promotion.Id,
                    promotion.Name,
                    amount,
                    scope,
                    promotion.CombinationRule,
                    promotion.CombinationGroup,
                    promotion.Priority,
                    state.OfferId));
            }
        }
    }

    private async Task<PromotionEvaluationState> BuildStateAsync(ContractContext context, CancellationToken cancellationToken)
    {
        return new PromotionEvaluationState
        {
            StoreId = context.StoreId,
            CurrencyCode = context.CurrencyCode,
            CustomerId = context.CustomerId,
            CustomerGroupId = context.CustomerGroupId,
            IsGuest = context.IsGuest,
            CartSubtotal = context.CartSubtotal,
            TotalQuantity = context.TotalQuantity,
            Lines = context.Lines.Select(x => new PromotionCartLineState(
                x.OfferId, x.ProductId, x.VariantId, x.Quantity, x.UnitPrice, x.CategoryIds)).ToList(),
            OfferId = context.OfferId,
            ProductId = context.ProductId,
            VariantId = context.VariantId,
            LineQuantity = context.LineQuantity,
            LineSubtotal = context.LineSubtotal,
            LineCategoryIds = context.LineCategoryIds,
            CouponCode = context.CouponCode,
            CurrentTimeUtc = context.CurrentTimeUtc,
            PromotionUsageCount = 0,
            CustomerPromotionUsageCount = 0
        };
    }

    private async Task<PromotionEvaluationState> BuildPromotionStateAsync(
        PromotionEvaluationState baseState,
        Domain.Entities.Promotion promotion,
        CancellationToken cancellationToken)
    {
        var customerUsage = baseState.CustomerId.HasValue
            ? await promotionsRepository.GetCustomerUsageCountAsync(promotion.Id, baseState.CustomerId.Value, cancellationToken).ConfigureAwait(false)
            : 0;

        return baseState with
        {
            PromotionUsageCount = promotion.UsageCount,
            CustomerPromotionUsageCount = customerUsage,
            GlobalUsageLimit = promotion.GlobalUsageLimit,
            PerCustomerUsageLimit = promotion.PerCustomerUsageLimit
        };
    }

    private static IReadOnlyList<ContractEffect> MapEffects(IReadOnlyList<InternalEffect> effects) =>
        effects.Select(x => new ContractEffect(
            x.PromotionId,
            x.Name,
            x.Amount,
            x.Scope,
            x.CombinationRule,
            x.CombinationGroup,
            x.Priority,
            x.OfferId)).ToList();
}
