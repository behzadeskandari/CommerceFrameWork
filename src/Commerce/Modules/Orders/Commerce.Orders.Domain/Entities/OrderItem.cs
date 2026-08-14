namespace Commerce.Orders.Domain.Entities;

public sealed class OrderItem : Commerce.Framework.Core.Entities.Entity
{
    private OrderItem()
    {
    }

    public int OrderId { get; private set; }

    public int CheckoutId { get; private set; }

    public int CartItemId { get; private set; }

    public int OfferId { get; private set; }

    public int ProductId { get; private set; }

    public int? VariantId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string? VariantName { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public int CancelledQuantity { get; private set; }

    public int ReturnedQuantity { get; private set; }

    public int ActiveQuantity => Quantity - CancelledQuantity;

    public int ReturnableQuantity => ActiveQuantity - ReturnedQuantity;

    public decimal UnitPrice { get; private set; }

    public decimal LineSubtotal { get; private set; }

    public decimal DiscountTotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal LineTotal { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public string? PrimaryImageUrl { get; private set; }

    public string? PrimaryImageThumbnailUrl { get; private set; }

    public static OrderItem Create(
        int orderId,
        int checkoutId,
        int cartItemId,
        int offerId,
        int productId,
        int? variantId,
        string productName,
        string? variantName,
        string sku,
        int quantity,
        decimal unitPrice,
        decimal lineSubtotal,
        decimal discountTotal,
        decimal taxTotal,
        decimal lineTotal,
        string currencyCode,
        string? primaryImageUrl = null,
        string? primaryImageThumbnailUrl = null)
    {
        if (orderId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return new OrderItem
        {
            OrderId = orderId,
            CheckoutId = checkoutId,
            CartItemId = cartItemId,
            OfferId = offerId,
            ProductId = productId,
            VariantId = variantId,
            ProductName = productName.Trim(),
            VariantName = string.IsNullOrWhiteSpace(variantName) ? null : variantName.Trim(),
            Sku = sku.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineSubtotal = lineSubtotal,
            DiscountTotal = discountTotal,
            TaxTotal = taxTotal,
            LineTotal = lineTotal,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            PrimaryImageUrl = primaryImageUrl,
            PrimaryImageThumbnailUrl = primaryImageThumbnailUrl
        };
    }

    public void RecordCancellation(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity > ActiveQuantity)
        {
            throw new InvalidOperationException("Cancellation quantity exceeds active quantity.");
        }

        CancelledQuantity += quantity;
    }

    public void RecordReturn(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (quantity > ReturnableQuantity)
        {
            throw new InvalidOperationException("Return quantity exceeds returnable quantity.");
        }

        ReturnedQuantity += quantity;
    }

    public decimal CalculateLineRefundAmount(int quantity)
    {
        if (quantity <= 0 || quantity > ReturnableQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (Quantity == 0)
        {
            return 0m;
        }

        return Math.Round(LineTotal * quantity / Quantity, 4, MidpointRounding.AwayFromZero);
    }
}
