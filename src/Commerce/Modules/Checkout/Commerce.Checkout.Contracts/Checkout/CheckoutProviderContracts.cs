namespace Commerce.Checkout.Contracts.Checkout;

public sealed record ShippingRateRequest(
    int StoreId,
    int CartId,
    string CurrencyCode,
    CheckoutAddressDto ShippingAddress,
    IReadOnlyList<ShippingRateLineItem> Items);

public sealed record ShippingRateLineItem(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice,
    string ProductType);

public sealed record ShippingOption(
    string Id,
    string Name,
    string ProviderSystemName,
    decimal Price,
    string Currency,
    string? EstimatedDelivery);

public sealed record TaxCalculationRequest(
    int StoreId,
    string CurrencyCode,
    int? CustomerId,
    CheckoutAddressDto? BillingAddress,
    CheckoutAddressDto? ShippingAddress,
    decimal Subtotal,
    IReadOnlyList<ShippingRateLineItem> Items);

public sealed record TaxCalculationResult(
    decimal TaxTotal,
    string CurrencyCode,
    IReadOnlyList<TaxLine> Lines);

public sealed record TaxLine(string Name, decimal Amount, string CurrencyCode);

public sealed record DiscountCalculationRequest(
    int StoreId,
    int? CustomerId,
    int CartId,
    string CurrencyCode,
    decimal Subtotal,
    IReadOnlyList<string> CouponCodes);

public sealed record DiscountCalculationResult(
    decimal DiscountTotal,
    string CurrencyCode,
    IReadOnlyList<DiscountLine> Lines);

public sealed record DiscountLine(string Name, decimal Amount, string CurrencyCode);

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
    string CurrencyCode,
    int? CustomerId,
    decimal Subtotal,
    decimal ShippingTotal,
    CheckoutAddressDto? BillingAddress,
    CheckoutAddressDto? ShippingAddress,
    IReadOnlyList<ShippingRateLineItem> Items,
    IReadOnlyList<string> CouponCodes);

public sealed record CheckoutTotalResult(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string CurrencyCode);

public interface ICheckoutTotalsCalculator
{
    Task<CheckoutTotalResult> CalculateAsync(
        CheckoutTotalContext context,
        CancellationToken cancellationToken = default);
}
