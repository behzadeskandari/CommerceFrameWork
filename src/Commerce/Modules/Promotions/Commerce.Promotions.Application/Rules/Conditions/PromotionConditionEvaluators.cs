using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Application.Rules;
using Commerce.Promotions.Domain.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Application.Rules.Conditions;

public sealed class MinimumCartSubtotalConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.MinimumCartSubtotal;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        var minimum = PromotionParameterReader.ReadDecimal(condition.ParametersJson, "minimum");
        return state.CartSubtotal >= minimum;
    }
}

public sealed class MinimumQuantityConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.MinimumQuantity;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        var minimum = PromotionParameterReader.ReadInt(condition.ParametersJson, "minimum");
        return state.TotalQuantity >= minimum;
    }
}

public sealed class CustomerGroupConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.CustomerGroup;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        var groupId = PromotionParameterReader.ReadInt(condition.ParametersJson, "customerGroupId");
        return state.CustomerGroupId.HasValue && state.CustomerGroupId.Value == groupId;
    }
}

public sealed class ProductInCartConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.ProductInCart;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        var productIds = PromotionParameterReader.ReadIntList(condition.ParametersJson, "productIds");
        if (productIds.Count == 0)
        {
            return true;
        }

        return state.Lines.Any(line => productIds.Contains(line.ProductId));
    }
}

public sealed class CategoryInCartConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.CategoryInCart;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        var categoryIds = PromotionParameterReader.ReadIntList(condition.ParametersJson, "categoryIds");
        if (categoryIds.Count == 0)
        {
            return true;
        }

        return state.Lines.Any(line => line.CategoryIds.Any(categoryIds.Contains));
    }
}

public sealed class ProductRestrictionConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.ProductRestriction;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        if (!state.ProductId.HasValue)
        {
            return true;
        }

        var allowed = PromotionParameterReader.ReadIntList(condition.ParametersJson, "allowedProductIds");
        var blocked = PromotionParameterReader.ReadIntList(condition.ParametersJson, "blockedProductIds");

        if (allowed.Count > 0 && !allowed.Contains(state.ProductId.Value))
        {
            return false;
        }

        return blocked.Count == 0 || !blocked.Contains(state.ProductId.Value);
    }
}

public sealed class CategoryRestrictionConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.CategoryRestriction;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        var allowed = PromotionParameterReader.ReadIntList(condition.ParametersJson, "allowedCategoryIds");
        var blocked = PromotionParameterReader.ReadIntList(condition.ParametersJson, "blockedCategoryIds");

        if (allowed.Count > 0 && !state.LineCategoryIds.Any(allowed.Contains))
        {
            return false;
        }

        return blocked.Count == 0 || !state.LineCategoryIds.Any(blocked.Contains);
    }
}

public sealed class StoreRestrictionConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.StoreRestriction;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        var storeId = PromotionParameterReader.ReadInt(condition.ParametersJson, "storeId");
        return storeId <= 0 || state.StoreId == storeId;
    }
}

public sealed class UsageLimitRemainingConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.UsageLimitRemaining;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        if (!state.GlobalUsageLimit.HasValue)
        {
            return true;
        }

        return state.PromotionUsageCount < state.GlobalUsageLimit.Value;
    }
}

public sealed class PerCustomerUsageRemainingConditionEvaluator : IPromotionConditionEvaluator
{
    public PromotionConditionType ConditionType => PromotionConditionType.PerCustomerUsageRemaining;

    public bool Evaluate(PromotionCondition condition, PromotionEvaluationState state)
    {
        if (!state.PerCustomerUsageLimit.HasValue || !state.CustomerId.HasValue)
        {
            return true;
        }

        return state.CustomerPromotionUsageCount < state.PerCustomerUsageLimit.Value;
    }
}
