namespace Commerce.Pricing.Contracts.Pricing;

public sealed record AppliedDiscountDto(
    int DiscountId,
    string Name,
    decimal Amount,
    string CurrencyCode,
    string Scope,
    int? OfferId = null,
    string? CouponCode = null);

public sealed record PriceCalculationResult(
    decimal BasePrice,
    decimal DiscountAmount,
    decimal FinalPrice,
    string CurrencyCode,
    IReadOnlyList<AppliedDiscountDto> AppliedDiscounts,
    decimal? DiscountPercentage = null);

public sealed record PriceCalculationContext(
    int StoreId,
    string CurrencyCode,
    int? CustomerId,
    bool IsGuest,
    int? CustomerGroupId,
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal BaseUnitPrice,
    decimal CartSubtotal,
    string? CouponCode,
    DateTime CurrentTimeUtc);

public sealed record CartDiscountCalculationContext(
    int StoreId,
    int CartId,
    string CurrencyCode,
    int? CustomerId,
    bool IsGuest,
    int? CustomerGroupId,
    IReadOnlyList<CartDiscountLineContext> Lines,
    string? CouponCode,
    DateTime CurrentTimeUtc);

public sealed record CartDiscountLineContext(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice);

public sealed record CartDiscountCalculationResult(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    string CurrencyCode,
    IReadOnlyList<AppliedDiscountDto> AppliedDiscounts,
    IReadOnlyList<CartLineDiscountResult> LineResults);

public sealed record CartLineDiscountResult(
    int OfferId,
    decimal BaseUnitPrice,
    decimal FinalUnitPrice,
    decimal LineSubtotal,
    decimal LineDiscountTotal,
    IReadOnlyList<AppliedDiscountDto> AppliedDiscounts);

public interface IPriceCalculationService
{
    Task<PriceCalculationResult> CalculateOfferPriceAsync(
        PriceCalculationContext context,
        CancellationToken cancellationToken = default);

    Task<CartDiscountCalculationResult> CalculateCartAsync(
        CartDiscountCalculationContext context,
        CancellationToken cancellationToken = default);
}

public interface ICouponValidationService
{
    Task<CouponValidationResult> ValidateAsync(
        CouponValidationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CouponValidationRequest(
    string Code,
    int StoreId,
    string CurrencyCode,
    int? CustomerId,
    bool IsGuest,
    int? CustomerGroupId,
    decimal CartSubtotal,
    DateTime CurrentTimeUtc);

public sealed record CouponValidationResult(
    bool IsValid,
    string? NormalizedCode,
    int? CouponId,
    int? DiscountId,
    IReadOnlyList<string> Errors);

public interface ICouponUsageService
{
    Task<CouponUsageResult> TryConsumeAsync(
        CouponUsageRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CouponUsageRequest(
    string Code,
    int StoreId,
    int OrderId,
    int? CustomerId);

public sealed record CouponUsageResult(
    bool Success,
    string? ErrorMessage,
    int? CouponId = null);
