using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Application.Rules;
using Commerce.Promotions.Domain.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Application.Rules.Actions;

public sealed class PercentageDiscountActionExecutor : IPromotionActionExecutor
{
    public PromotionActionType ActionType => PromotionActionType.PercentageDiscount;

    public PromotionDiscountEffect? Execute(PromotionAction action, Promotion promotion, PromotionEvaluationState state)
    {
        var percent = PromotionParameterReader.ReadDecimal(action.ParametersJson, "percent");
        if (percent <= 0)
        {
            return null;
        }

        var baseAmount = action.TargetScope is PromotionTargetScope.Cart ? state.CartSubtotal : state.LineSubtotal;
        if (baseAmount <= 0)
        {
            return null;
        }

        var amount = Math.Round(baseAmount * percent / 100m, 4, MidpointRounding.ToEven);
        var max = PromotionParameterReader.ReadDecimal(action.ParametersJson, "maximumDiscountAmount", 0);
        if (max > 0)
        {
            amount = Math.Min(amount, max);
        }

        amount = Math.Min(amount, baseAmount);
        if (amount <= 0)
        {
            return null;
        }

        return new PromotionDiscountEffect(
            promotion.Id,
            promotion.Name,
            amount,
            action.TargetScope,
            promotion.CombinationRule,
            promotion.CombinationGroup,
            promotion.Priority,
            state.OfferId);
    }
}

public sealed class FixedAmountDiscountActionExecutor : IPromotionActionExecutor
{
    public PromotionActionType ActionType => PromotionActionType.FixedAmountDiscount;

    public PromotionDiscountEffect? Execute(PromotionAction action, Promotion promotion, PromotionEvaluationState state)
    {
        var amountValue = PromotionParameterReader.ReadDecimal(action.ParametersJson, "amount");
        if (amountValue <= 0)
        {
            return null;
        }

        var baseAmount = action.TargetScope is PromotionTargetScope.Cart ? state.CartSubtotal : state.LineSubtotal;
        if (baseAmount <= 0)
        {
            return null;
        }

        var amount = Math.Min(amountValue, baseAmount);
        return new PromotionDiscountEffect(
            promotion.Id,
            promotion.Name,
            amount,
            action.TargetScope,
            promotion.CombinationRule,
            promotion.CombinationGroup,
            promotion.Priority,
            state.OfferId);
    }
}

public sealed class BuyXGetYActionExecutor : IPromotionActionExecutor
{
    public PromotionActionType ActionType => PromotionActionType.BuyXGetY;

    public PromotionDiscountEffect? Execute(PromotionAction action, Promotion promotion, PromotionEvaluationState state)
    {
        var buyQuantity = PromotionParameterReader.ReadInt(action.ParametersJson, "buyQuantity");
        var getQuantity = PromotionParameterReader.ReadInt(action.ParametersJson, "getQuantity");
        var getDiscountPercent = PromotionParameterReader.ReadDecimal(action.ParametersJson, "getDiscountPercent", 100m);
        var productIds = PromotionParameterReader.ReadIntList(action.ParametersJson, "productIds");

        if (buyQuantity <= 0 || getQuantity <= 0 || state.LineQuantity <= 0)
        {
            return null;
        }

        if (productIds.Count > 0 && (!state.ProductId.HasValue || !productIds.Contains(state.ProductId.Value)))
        {
            return null;
        }

        var sets = state.LineQuantity / (buyQuantity + getQuantity);
        if (sets <= 0)
        {
            return null;
        }

        var freeUnits = sets * getQuantity;
        var unitPrice = state.LineQuantity > 0 ? state.LineSubtotal / state.LineQuantity : 0m;
        var amount = unitPrice * freeUnits * (getDiscountPercent / 100m);

        if (amount <= 0)
        {
            return null;
        }

        return new PromotionDiscountEffect(
            promotion.Id,
            promotion.Name,
            amount,
            PromotionTargetScope.Line,
            promotion.CombinationRule,
            promotion.CombinationGroup,
            promotion.Priority,
            state.OfferId);
    }
}

public sealed class ApplyLinkedDiscountActionExecutor : IPromotionActionExecutor
{
    public PromotionActionType ActionType => PromotionActionType.ApplyLinkedDiscount;

    public PromotionDiscountEffect? Execute(PromotionAction action, Promotion promotion, PromotionEvaluationState state) =>
        null;
}
