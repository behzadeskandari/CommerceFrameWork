using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Domain.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Application.Rules;

public static class PromotionCombinationSelector
{
    public static IReadOnlyList<PromotionDiscountEffect> ApplyCombinationRules(IReadOnlyList<PromotionDiscountEffect> candidates)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var ordered = candidates
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.Amount)
            .ToList();

        if (ordered.All(x => x.CombinationRule is PromotionCombinationRule.Stackable))
        {
            return ordered;
        }

        var result = new List<PromotionDiscountEffect>();
        var usedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var effect in ordered)
        {
            switch (effect.CombinationRule)
            {
                case PromotionCombinationRule.Exclusive:
                    return [effect];

                case PromotionCombinationRule.SameGroupExclusive:
                    if (!string.IsNullOrWhiteSpace(effect.CombinationGroup))
                    {
                        if (usedGroups.Contains(effect.CombinationGroup))
                        {
                            continue;
                        }

                        usedGroups.Add(effect.CombinationGroup);
                    }

                    result.Add(effect);
                    break;

                case PromotionCombinationRule.Stackable:
                    result.Add(effect);
                    break;
            }
        }

        return result;
    }
}

public sealed class PromotionRuleEngine(
    IEnumerable<IPromotionConditionEvaluator> conditionEvaluators,
    IEnumerable<IPromotionActionExecutor> actionExecutors)
{
    private readonly IReadOnlyDictionary<PromotionConditionType, IPromotionConditionEvaluator> _conditions =
        conditionEvaluators.ToDictionary(x => x.ConditionType);

    private readonly IReadOnlyDictionary<PromotionActionType, IPromotionActionExecutor> _actions =
        actionExecutors.ToDictionary(x => x.ActionType);

    public IReadOnlyList<PromotionDiscountEffect> Evaluate(
        Promotion promotion,
        PromotionEvaluationState state,
        PromotionTargetScope scope)
    {
        if (!promotion.IsCurrentlyValid(state.CurrentTimeUtc) ||
            !promotion.AppliesToStore(state.StoreId) ||
            !promotion.HasGlobalUsageRemaining() ||
            !promotion.MatchesCouponCode(state.CouponCode))
        {
            return [];
        }

        if (promotion.PerCustomerUsageLimit.HasValue &&
            state.CustomerId.HasValue &&
            state.CustomerPromotionUsageCount >= promotion.PerCustomerUsageLimit.Value)
        {
            return [];
        }

        foreach (var condition in promotion.Conditions)
        {
            if (!_conditions.TryGetValue(condition.ConditionType, out var evaluator))
            {
                continue;
            }

            if (!evaluator.Evaluate(condition, state))
            {
                return [];
            }
        }

        var effects = new List<PromotionDiscountEffect>();
        foreach (var action in promotion.Actions.Where(x => x.TargetScope == scope))
        {
            if (!_actions.TryGetValue(action.ActionType, out var executor))
            {
                continue;
            }

            var effect = executor.Execute(action, promotion, state);
            if (effect is not null)
            {
                effects.Add(effect);
            }
        }

        return PromotionCombinationSelector.ApplyCombinationRules(effects);
    }
}
