using Commerce.Pricing.Contracts.Pricing;
using Commerce.Pricing.Domain.Entities;
using Commerce.Pricing.Domain.Enums;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Pricing.Application.Pricing;

/// <summary>
/// Pure discount calculation engine.
/// Stacking: stackable discounts apply sequentially to the running price (compound).
/// Non-stackable: only the highest-priority applicable discount applies.
/// Rounding: banker's rounding at Money scale (4 dp) after each discount application.
/// </summary>
public static class DiscountCalculationEngine
{
    public static decimal CalculateDiscountAmount(
        Discount discount,
        decimal baseAmount,
        string currencyCode)
    {
        if (baseAmount <= 0)
        {
            return 0m;
        }

        var currency = Currency.FromCode(currencyCode);
        var baseMoney = Money.Create(baseAmount, currency);

        decimal rawDiscount = discount.DiscountType switch
        {
            DiscountType.Percentage => baseMoney.Multiply(discount.Value / 100m).Amount,
            DiscountType.FixedAmount => ValidateFixedCurrency(discount, currencyCode)
                ? Math.Min(discount.Value, baseAmount)
                : 0m,
            _ => 0m
        };

        if (discount.MaximumDiscountAmount.HasValue)
        {
            rawDiscount = Math.Min(rawDiscount, discount.MaximumDiscountAmount.Value);
        }

        rawDiscount = Math.Min(rawDiscount, baseAmount);
        return Money.Create(rawDiscount, currency).Amount;
    }

    public static IReadOnlyList<Discount> SelectApplicableLineDiscounts(
        IReadOnlyList<Discount> candidates,
        int offerId,
        int productId,
        int? variantId,
        IReadOnlyList<int> productCategoryIds,
        int quantity,
        decimal lineSubtotal,
        decimal cartSubtotal,
        int storeId,
        int? customerId,
        bool isGuest,
        int? customerGroupId,
        string currencyCode,
        DateTime utcNow)
    {
        var applicable = candidates
            .Where(d => d.ApplicationScope is DiscountApplicationScope.Line)
            .Where(d => d.IsCurrentlyValid(utcNow))
            .Where(d => d.AppliesToStore(storeId))
            .Where(d => d.IsEligibleForCustomer(customerId, isGuest, customerGroupId))
            .Where(d => MeetsMinimumRequirements(d, quantity, cartSubtotal))
            .Where(d => MatchesLineTarget(d, offerId, productId, variantId, productCategoryIds))
            .Where(d => d.DiscountType is not DiscountType.FixedAmount || string.Equals(d.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => GetTargetSpecificity(d, offerId, productId, variantId, productCategoryIds))
            .ThenByDescending(d => d.Priority)
            .ToList();

        return ApplyStackingRules(applicable);
    }

    public static IReadOnlyList<Discount> SelectApplicableCartDiscounts(
        IReadOnlyList<Discount> candidates,
        decimal cartSubtotal,
        int storeId,
        int? customerId,
        bool isGuest,
        int? customerGroupId,
        string currencyCode,
        DateTime utcNow,
        int totalQuantity)
    {
        var applicable = candidates
            .Where(d => d.ApplicationScope is DiscountApplicationScope.Cart)
            .Where(d => d.IsCurrentlyValid(utcNow))
            .Where(d => d.AppliesToStore(storeId))
            .Where(d => d.IsEligibleForCustomer(customerId, isGuest, customerGroupId))
            .Where(d => MeetsMinimumRequirements(d, totalQuantity, cartSubtotal))
            .Where(d => d.Targets.Any(t => t.TargetType is DiscountTargetType.Cart) || d.Targets.Count == 0)
            .Where(d => d.DiscountType is not DiscountType.FixedAmount || string.Equals(d.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.Priority)
            .ToList();

        return ApplyStackingRules(applicable);
    }

    public static (decimal FinalAmount, IReadOnlyList<(Discount Discount, decimal Amount)> Applied) ApplyDiscountsToAmount(
        decimal baseAmount,
        IReadOnlyList<Discount> discounts,
        string currencyCode)
    {
        if (baseAmount <= 0 || discounts.Count == 0)
        {
            return (baseAmount, []);
        }

        var currency = Currency.FromCode(currencyCode);
        var running = Money.Create(baseAmount, currency);
        var applied = new List<(Discount, decimal)>();

        foreach (var discount in discounts)
        {
            var discountAmount = CalculateDiscountAmount(discount, running.Amount, currencyCode);
            if (discountAmount <= 0)
            {
                continue;
            }

            running = running.Subtract(Money.Create(discountAmount, currency));
            applied.Add((discount, discountAmount));
        }

        return (running.Amount, applied);
    }

    private static IReadOnlyList<Discount> ApplyStackingRules(IReadOnlyList<Discount> orderedApplicable)
    {
        if (orderedApplicable.Count == 0)
        {
            return [];
        }

        if (orderedApplicable.All(d => d.StackingMode is StackingMode.Stackable))
        {
            return orderedApplicable;
        }

        if (orderedApplicable.All(d => d.StackingMode is StackingMode.NonStackable))
        {
            return [orderedApplicable[0]];
        }

        var result = new List<Discount>();
        var hasNonStackableWinner = false;

        foreach (var discount in orderedApplicable)
        {
            if (hasNonStackableWinner && discount.StackingMode is StackingMode.NonStackable)
            {
                continue;
            }

            result.Add(discount);

            if (discount.StackingMode is StackingMode.NonStackable)
            {
                hasNonStackableWinner = true;
                break;
            }
        }

        return result;
    }

    private static bool MeetsMinimumRequirements(Discount discount, int quantity, decimal cartSubtotal)
    {
        if (discount.MinimumCartSubtotal.HasValue && cartSubtotal < discount.MinimumCartSubtotal.Value)
        {
            return false;
        }

        if (discount.MinimumQuantity.HasValue && quantity < discount.MinimumQuantity.Value)
        {
            return false;
        }

        return true;
    }

    private static bool MatchesLineTarget(
        Discount discount,
        int offerId,
        int productId,
        int? variantId,
        IReadOnlyList<int> productCategoryIds)
    {
        if (discount.Targets.Count == 0)
        {
            return true;
        }

        foreach (var target in discount.Targets)
        {
            var matches = target.TargetType switch
            {
                DiscountTargetType.Offer => target.TargetId == offerId,
                DiscountTargetType.Product => target.TargetId == productId,
                DiscountTargetType.Variant => variantId.HasValue && target.TargetId == variantId.Value,
                DiscountTargetType.Category => productCategoryIds.Contains(target.TargetId),
                _ => false
            };

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    private static int GetTargetSpecificity(
        Discount discount,
        int offerId,
        int productId,
        int? variantId,
        IReadOnlyList<int> productCategoryIds)
    {
        var max = 0;
        foreach (var target in discount.Targets)
        {
            var specificity = target.TargetType switch
            {
                DiscountTargetType.Offer when target.TargetId == offerId => 100,
                DiscountTargetType.Variant when variantId.HasValue && target.TargetId == variantId.Value => 80,
                DiscountTargetType.Product when target.TargetId == productId => 60,
                DiscountTargetType.Category when productCategoryIds.Contains(target.TargetId) => 50,
                _ => 0
            };

            max = Math.Max(max, specificity);
        }

        return max;
    }

    private static bool ValidateFixedCurrency(Discount discount, string currencyCode) =>
        !string.IsNullOrWhiteSpace(discount.CurrencyCode) &&
        string.Equals(discount.CurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase);
}
