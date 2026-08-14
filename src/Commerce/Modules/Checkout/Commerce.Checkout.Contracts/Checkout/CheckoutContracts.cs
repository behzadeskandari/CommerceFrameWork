using Commerce.Checkout.Domain.Enums;
using Commerce.Framework.Core.Results;

namespace Commerce.Checkout.Contracts.Checkout;

public sealed record CheckoutAddressDto(
    int? SourceCustomerAddressId,
    string FirstName,
    string LastName,
    string Country,
    string? StateProvince,
    string City,
    string Address1,
    string? Address2,
    string PostalCode,
    string? PhoneNumber);

public sealed record CheckoutItemDto(
    int CartItemId,
    int OfferId,
    int ProductId,
    int? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    string ProductType,
    int Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    string Currency,
    bool PriceChanged,
    decimal WeightGrams,
    CheckoutItemImageDto? PrimaryImage);

public sealed record CheckoutItemImageDto(string Url, string? ThumbnailUrl, string? AltText);

public sealed record CheckoutTotalsDto(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal ProductTaxTotal,
    decimal ShippingTaxTotal,
    decimal GiftCardApplied,
    decimal StoreCreditApplied,
    decimal WalletAdjustmentTotal,
    decimal GrandTotal,
    string Currency,
    bool PricesIncludeTax,
    IReadOnlyList<TaxLineDto> TaxLines,
    IReadOnlyList<TaxLineItemDto> TaxLineItems);

public sealed record TaxLineDto(
    string Name,
    decimal Amount,
    decimal? RatePercentage,
    bool IsShippingTax,
    decimal TaxableAmount = 0m);

public sealed record TaxLineItemDto(
    int OfferId,
    decimal TaxableAmount,
    decimal TaxAmount,
    int? TaxCategoryId,
    string? TaxCategoryName,
    decimal? RatePercentage);

public sealed record ShippingOptionDto(
    string Id,
    string Name,
    string ProviderSystemName,
    decimal Price,
    string Currency,
    string? EstimatedDelivery,
    bool RequiresAddress = true);

public sealed record PaymentMethodDto(
    string Id,
    string Name,
    string SystemName,
    string DisplayName,
    bool RequiresRedirect,
    bool SupportsGuest,
    bool SupportsCurrency);

public sealed record CheckoutCustomerDto(
    int? CustomerId,
    string? Email,
    bool IsGuest);

public sealed record CheckoutDto(
    int Id,
    int CartId,
    int StoreId,
    CheckoutStatus Status,
    string Currency,
    int CurrencyId,
    CheckoutCustomerDto Customer,
    CheckoutAddressDto? BillingAddress,
    CheckoutAddressDto? ShippingAddress,
    bool UseShippingAsBilling,
    bool RequiresShipping,
    bool PriceChangeDetected,
    IReadOnlyList<CheckoutItemDto> Items,
    CheckoutTotalsDto Totals,
    IReadOnlyList<ShippingOptionDto> ShippingOptions,
    IReadOnlyList<PaymentMethodDto> PaymentMethods,
    string? SelectedShippingMethodId,
    string? SelectedPaymentMethodId,
    IReadOnlyList<string> ValidationErrors,
    IReadOnlyList<string> Warnings,
    DateTime ExpiresAtUtc,
    DateTime CartUpdatedAtUtc,
    string? AppliedCouponCode = null,
    string? AppliedGiftCardCode = null,
    decimal AppliedStoreCreditAmount = 0m,
    string? ReferralCode = null,
    int? AffiliateId = null);

public sealed record CheckoutAddressRequest(
    string FirstName,
    string LastName,
    string Country,
    string City,
    string Address1,
    string PostalCode,
    string? StateProvince = null,
    string? Address2 = null,
    string? PhoneNumber = null);

public sealed record SetBillingAddressRequest(
    CheckoutAddressRequest? Address,
    int? CustomerAddressId,
    bool UseShippingAsBilling = false);

public sealed record SetShippingAddressRequest(
    CheckoutAddressRequest? Address,
    int? CustomerAddressId);

public sealed record SetGuestContactRequest(string Email);

public sealed record SelectShippingMethodRequest(string MethodId, string ProviderSystemName);

public sealed record SelectPaymentMethodRequest(string MethodId, string SystemName);

public sealed record ApplyGiftCardRequest(string Code);

public sealed record ApplyStoreCreditRequest(decimal Amount);

public sealed record ApplyReferralCodeRequest(string ReferralCode);

public sealed record CheckoutValidationResultDto(
    CheckoutDto Checkout,
    bool IsValid,
    bool IsReadyForOrder,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record OrderPreparationLineDto(
    int CartItemId,
    int OfferId,
    int ProductId,
    int? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    decimal LineDiscount,
    decimal LineTax,
    decimal LineTotal,
    string CurrencyCode,
    int? TaxCategoryId = null,
    string? PrimaryImageUrl = null,
    string? PrimaryImageThumbnailUrl = null);

public sealed record OrderPreparationResult(
    int CheckoutId,
    int StoreId,
    int CartId,
    int? CustomerId,
    string CurrencyCode,
    int CurrencyId,
    string? GuestEmail,
    CheckoutAddressDto? BillingAddress,
    CheckoutAddressDto? ShippingAddress,
    bool RequiresShipping,
    string? SelectedShippingMethodId,
    string? SelectedShippingProviderSystemName,
    decimal ShippingTotal,
    string? SelectedPaymentMethodId,
    string? SelectedPaymentMethodSystemName,
    IReadOnlyList<OrderPreparationLineDto> Items,
    CheckoutTotalsDto Totals,
    IReadOnlyList<TaxLineDto> OrderTaxLines,
    string? AppliedCouponCode = null,
    string? AppliedGiftCardCode = null,
    decimal GiftCardApplied = 0m,
    decimal StoreCreditApplied = 0m,
    string? ReferralCode = null,
    int? AffiliateId = null);

public interface ICheckoutService
{
    Task<Result<CheckoutDto>> StartAsync(CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> GetAsync(int checkoutId, CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> SetGuestContactAsync(
        int checkoutId,
        SetGuestContactRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> SetBillingAddressAsync(
        int checkoutId,
        SetBillingAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> SetShippingAddressAsync(
        int checkoutId,
        SetShippingAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> SelectShippingMethodAsync(
        int checkoutId,
        SelectShippingMethodRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> SelectPaymentMethodAsync(
        int checkoutId,
        SelectPaymentMethodRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> RefreshAsync(int checkoutId, CancellationToken cancellationToken = default);

    Task<Result<CheckoutValidationResultDto>> ValidateAsync(
        int checkoutId,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> ApplyGiftCardAsync(
        int checkoutId,
        ApplyGiftCardRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> RemoveGiftCardAsync(
        int checkoutId,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> ApplyStoreCreditAsync(
        int checkoutId,
        ApplyStoreCreditRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> RemoveStoreCreditAsync(
        int checkoutId,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> ApplyReferralCodeAsync(
        int checkoutId,
        ApplyReferralCodeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CheckoutDto>> RemoveReferralCodeAsync(
        int checkoutId,
        CancellationToken cancellationToken = default);
}

public interface ICheckoutOrderPreparationService
{
    Task<Result<OrderPreparationResult>> ValidateForOrderCreationAsync(
        int checkoutId,
        CancellationToken cancellationToken = default);
}

public interface ICheckoutCompletionService
{
    Task<Result> MarkCompletedAsync(int checkoutId, CancellationToken cancellationToken = default);
}
