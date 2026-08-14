namespace Commerce.Checkout.Contracts.Checkout;

public sealed record ShippingRateRequest(
    int StoreId,
    int CartId,
    string CurrencyCode,
    CheckoutAddressDto? ShippingAddress,
    IReadOnlyList<ShippingRateLineItem> Items);

public sealed record ShippingRateLineItem(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice,
    string ProductType,
    decimal WeightGrams = 0m,
    decimal LineSubtotal = 0m);

public sealed record ShippingOption(
    string Id,
    string Name,
    string ProviderSystemName,
    decimal Price,
    string Currency,
    string? EstimatedDelivery,
    bool RequiresAddress = true);

public sealed record TaxCalculationRequest(
    int StoreId,
    int CartId,
    string CurrencyCode,
    int? CustomerId,
    CheckoutAddressDto? BillingAddress,
    CheckoutAddressDto? ShippingAddress,
    decimal Subtotal,
    IReadOnlyList<ShippingRateLineItem> Items,
    decimal ShippingTotal = 0m,
    IReadOnlyList<string> CouponCodes = null!,
    bool IsGuest = false,
    bool RequiresShipping = true)
{
    public IReadOnlyList<string> CouponCodes { get; init; } = CouponCodes ?? [];
}

public sealed record TaxCalculationResult(
    decimal TaxTotal,
    decimal ProductTaxTotal,
    decimal ShippingTaxTotal,
    string CurrencyCode,
    IReadOnlyList<TaxLine> Lines,
    IReadOnlyList<TaxLineItemResult> LineItems,
    bool PricesIncludeTax);

public sealed record TaxLine(
    string Name,
    decimal Amount,
    string CurrencyCode,
    decimal? RatePercentage = null,
    bool IsShippingTax = false,
    decimal TaxableAmount = 0m);

public sealed record TaxLineItemResult(
    int OfferId,
    decimal TaxableAmount,
    decimal TaxAmount,
    int? TaxCategoryId,
    string? TaxCategoryName,
    decimal? RatePercentage);

public sealed record DiscountCalculationRequest(
    int StoreId,
    int? CustomerId,
    int CartId,
    string CurrencyCode,
    decimal Subtotal,
    IReadOnlyList<string> CouponCodes,
    bool IsGuest = false,
    IReadOnlyList<DiscountLineItem>? Items = null);

public sealed record DiscountLineItem(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice);

public sealed record DiscountCalculationResult(
    decimal DiscountTotal,
    string CurrencyCode,
    IReadOnlyList<DiscountLine> Lines);

public sealed record DiscountLine(
    string Name,
    decimal Amount,
    string CurrencyCode,
    int? DiscountId = null,
    int? OfferId = null,
    string? CouponCode = null,
    string Scope = "Cart");

public interface IShippingRateProvider
{
    string ProviderSystemName { get; }

    Task<IReadOnlyList<ShippingOption>> GetRatesAsync(
        ShippingRateRequest request,
        CancellationToken cancellationToken = default);
}

public interface ITaxCalculator
{
    Task<TaxCalculationResult> CalculateAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDiscountCalculator
{
    Task<DiscountCalculationResult> CalculateAsync(
        DiscountCalculationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IPaymentMethodProvider
{
    string ProviderSystemName { get; }

    Task<IReadOnlyList<PaymentMethodDto>> GetMethodsAsync(
        int storeId,
        string currencyCode,
        bool isGuest,
        CancellationToken cancellationToken = default);
}

public sealed record CheckoutTotalContext(
    int StoreId,
    int CartId,
    string CurrencyCode,
    int? CustomerId,
    decimal Subtotal,
    decimal ShippingTotal,
    CheckoutAddressDto? BillingAddress,
    CheckoutAddressDto? ShippingAddress,
    IReadOnlyList<ShippingRateLineItem> Items,
    IReadOnlyList<string> CouponCodes,
    bool IsGuest = false,
    bool RequiresShipping = true,
    string? AppliedGiftCardCode = null,
    decimal AppliedStoreCreditAmount = 0m);

public sealed record CheckoutTotalResult(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string CurrencyCode,
    decimal ProductTaxTotal = 0m,
    decimal ShippingTaxTotal = 0m,
    bool PricesIncludeTax = false,
    IReadOnlyList<TaxLine>? TaxLines = null,
    IReadOnlyList<TaxLineItemResult>? TaxLineItems = null,
    decimal GiftCardApplied = 0m,
    decimal StoreCreditApplied = 0m);

public interface ICheckoutTotalsCalculator
{
    Task<CheckoutTotalResult> CalculateAsync(
        CheckoutTotalContext context,
        CancellationToken cancellationToken = default);
}
