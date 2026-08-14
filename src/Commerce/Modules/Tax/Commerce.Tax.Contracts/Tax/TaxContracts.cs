namespace Commerce.Tax.Contracts.Tax;

public sealed record TaxCalculationLineContext(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    decimal LineDiscount,
    decimal TaxableAmount,
    string ProductType,
    int? TaxCategoryId);

public sealed record TaxCalculationContext(
    int StoreId,
    int CartId,
    string CurrencyCode,
    int? CustomerId,
    bool IsGuest,
    bool IsCustomerTaxExempt,
    bool PricesIncludeTax,
    string CountryCode,
    string? StateProvince,
    string PostalCode,
    decimal ShippingTotal,
    bool RequiresShipping,
    IReadOnlyList<TaxCalculationLineContext> Lines);

public sealed record CalculatedTaxLineItem(
    int OfferId,
    int? TaxCategoryId,
    string? TaxCategoryName,
    decimal TaxableAmount,
    decimal TaxAmount,
    decimal? RatePercentage,
    string RateName);

public sealed record CalculatedTaxLine(
    string Name,
    decimal TaxableAmount,
    decimal TaxAmount,
    decimal? RatePercentage,
    bool IsShippingTax,
    int? TaxCategoryId,
    string? TaxCategoryName);

public sealed record CalculatedTaxResult(
    decimal TaxTotal,
    decimal ProductTaxTotal,
    decimal ShippingTaxTotal,
    string CurrencyCode,
    bool PricesIncludeTax,
    IReadOnlyList<CalculatedTaxLine> Lines,
    IReadOnlyList<CalculatedTaxLineItem> LineItems);

public interface ITaxCalculationService
{
    Task<CalculatedTaxResult> CalculateAsync(
        TaxCalculationContext context,
        CancellationToken cancellationToken = default);
}

public interface ITaxProvider
{
    string ProviderSystemName { get; }

    Task<CalculatedTaxResult> CalculateAsync(
        TaxCalculationContext context,
        CancellationToken cancellationToken = default);
}

public static class TaxProviderNames
{
    public const string Internal = "Tax.Internal";
}
