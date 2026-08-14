using Commerce.Cart.Domain.Entities;
using Commerce.Cart.Domain.Enums;
using Commerce.Pricing.Contracts.Pricing;

namespace Commerce.Cart.Application.Abstractions;

public interface ICartRepository
{
    Task<ShoppingCart?> GetByIdWithItemsAsync(int cartId, CancellationToken cancellationToken = default);

    Task<ShoppingCart?> GetActiveCustomerCartAsync(
        int storeId,
        int customerId,
        int currencyId,
        CancellationToken cancellationToken = default);

    Task<ShoppingCart?> GetActiveGuestCartAsync(
        int storeId,
        string guestToken,
        int currencyId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken = default);

    Task SaveAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
}

public interface IGuestCartCookieManager
{
    string? GetGuestToken();

    void SetGuestToken(string token, DateTime expiresAtUtc);

    void ClearGuestToken();
}

public interface ICartGuestTokenGenerator
{
    string GenerateToken();
}

public sealed record OfferValidationResult(
    bool IsValid,
    IReadOnlyList<string> Messages,
    int ProductId,
    int? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    decimal UnitPrice,
    string CurrencyCode);

public interface ICartOfferValidator
{
    Task<OfferValidationResult> ValidateAsync(
        int offerId,
        int storeId,
        int currencyId,
        string currencyCode,
        int quantity = 1,
        CancellationToken cancellationToken = default);
}

public interface ICartTotalsCalculator
{
    CartLineTotals CalculateLine(decimal unitPrice, int quantity, string currencyCode);

    CartAggregateTotals CalculateCart(IReadOnlyList<CartLineTotals> lines, string currencyCode);

    CartAggregateTotals CalculateFromDiscountResult(CartDiscountCalculationResult result);
}

public sealed record CartLineTotals(decimal UnitPrice, int Quantity, decimal LineSubtotal, string CurrencyCode);

public sealed record CartAggregateTotals(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string CurrencyCode);

public interface ICartItemDisplayEnricher
{
    Task<IReadOnlyDictionary<int, CartItemImageInfo>> GetPrimaryImagesByOfferAsync(
        IReadOnlyCollection<(int OfferId, int ProductId, int? VariantId)> items,
        CancellationToken cancellationToken = default);
}

public sealed record CartItemImageInfo(string Url, string? ThumbnailUrl, string? AltText);
