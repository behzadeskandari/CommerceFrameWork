using Commerce.Cart.Application.Abstractions;
using Commerce.Cart.Contracts.Carts;
using Commerce.Pricing.Contracts.Pricing;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Cart.Application.Carts;

public sealed class CartTotalsCalculator : ICartTotalsCalculator
{
    public CartLineTotals CalculateLine(decimal unitPrice, int quantity, string currencyCode)
    {
        var currency = Currency.FromCode(currencyCode);
        var unit = Money.Create(unitPrice, currency);
        var line = unit.Multiply(quantity);
        return new CartLineTotals(unit.Amount, quantity, line.Amount, currency.Code);
    }

    public CartAggregateTotals CalculateCart(IReadOnlyList<CartLineTotals> lines, string currencyCode)
    {
        var currency = Currency.FromCode(currencyCode);
        var subtotal = lines.Aggregate(Money.Zero(currency), (current, line) =>
        {
            var lineMoney = Money.Create(line.LineSubtotal, currency);
            return current.Add(lineMoney);
        });

        return new CartAggregateTotals(
            subtotal.Amount,
            DiscountTotal: 0m,
            ShippingTotal: 0m,
            TaxTotal: 0m,
            GrandTotal: subtotal.Amount,
            currency.Code);
    }

    public CartAggregateTotals CalculateFromDiscountResult(CartDiscountCalculationResult result) =>
        new(
            result.Subtotal,
            result.DiscountTotal,
            ShippingTotal: 0m,
            TaxTotal: 0m,
            result.GrandTotal,
            result.CurrencyCode);
}
