using Commerce.Checkout.Domain.Enums;
using Commerce.Checkout.Domain.ValueObjects;
using Commerce.Framework.Core.Entities;

namespace Commerce.Checkout.Domain.Entities;

public sealed class CheckoutSessionItem : Entity
{
    private CheckoutSessionItem()
    {
    }

    public int CheckoutSessionId { get; private set; }

    public int CartItemId { get; private set; }

    public int OfferId { get; private set; }

    public int ProductId { get; private set; }

    public int? VariantId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineSubtotal { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal PreviousUnitPrice { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static CheckoutSessionItem Create(
        int checkoutSessionId,
        int cartItemId,
        int offerId,
        int productId,
        int? variantId,
        int quantity,
        decimal unitPrice,
        decimal lineSubtotal,
        string currencyCode,
        decimal? previousUnitPrice = null)
    {
        if (checkoutSessionId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(checkoutSessionId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        var utcNow = DateTime.UtcNow;
        return new CheckoutSessionItem
        {
            CheckoutSessionId = checkoutSessionId,
            CartItemId = cartItemId,
            OfferId = offerId,
            ProductId = productId,
            VariantId = variantId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineSubtotal = lineSubtotal,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            PreviousUnitPrice = previousUnitPrice ?? unitPrice,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void UpdatePricing(decimal unitPrice, decimal lineSubtotal, decimal? previousUnitPrice = null)
    {
        PreviousUnitPrice = previousUnitPrice ?? UnitPrice;
        UnitPrice = unitPrice;
        LineSubtotal = lineSubtotal;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateQuantity(int quantity, decimal lineSubtotal)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        Quantity = quantity;
        LineSubtotal = lineSubtotal;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
