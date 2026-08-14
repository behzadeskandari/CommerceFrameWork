namespace Commerce.Orders.Domain.Entities;

public sealed class OrderTaxLine : Commerce.Framework.Core.Entities.Entity
{
    private OrderTaxLine()
    {
    }

    public int OrderId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal? RatePercentage { get; private set; }

    public decimal TaxableAmount { get; private set; }

    public decimal TaxAmount { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public bool IsShippingTax { get; private set; }

    public int? TaxCategoryId { get; private set; }

    public string? TaxCategoryName { get; private set; }

    public static OrderTaxLine Create(
        int orderId,
        string name,
        decimal? ratePercentage,
        decimal taxableAmount,
        decimal taxAmount,
        string currencyCode,
        bool isShippingTax,
        int? taxCategoryId,
        string? taxCategoryName)
    {
        return new OrderTaxLine
        {
            OrderId = orderId,
            Name = name.Trim(),
            RatePercentage = ratePercentage,
            TaxableAmount = taxableAmount,
            TaxAmount = taxAmount,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            IsShippingTax = isShippingTax,
            TaxCategoryId = taxCategoryId,
            TaxCategoryName = string.IsNullOrWhiteSpace(taxCategoryName) ? null : taxCategoryName.Trim()
        };
    }
}
