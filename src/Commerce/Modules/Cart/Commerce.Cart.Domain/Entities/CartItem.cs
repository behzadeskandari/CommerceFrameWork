using Commerce.Framework.Core.Entities;

namespace Commerce.Cart.Domain.Entities;

public sealed class CartItem : Entity
{
    private CartItem()
    {
    }

    public int CartId { get; private set; }

    public int OfferId { get; private set; }

    public int Quantity { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static CartItem Create(int cartId, int offerId, int quantity, int maxItemQuantity)
    {
        if (cartId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cartId), "Cart id cannot be negative.");
        }

        if (offerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offerId), "Offer id is required.");
        }

        ValidateQuantity(quantity, maxItemQuantity);

        var utcNow = DateTime.UtcNow;
        return new CartItem
        {
            CartId = cartId,
            OfferId = offerId,
            Quantity = quantity,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void IncreaseQuantity(int quantity, int maxItemQuantity)
    {
        ValidateQuantity(quantity, maxItemQuantity);

        var newQuantity = Quantity + quantity;
        if (newQuantity > maxItemQuantity)
        {
            throw new InvalidOperationException($"Quantity cannot exceed {maxItemQuantity}.");
        }

        Quantity = newQuantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetQuantity(int quantity, int maxItemQuantity)
    {
        ValidateQuantity(quantity, maxItemQuantity);
        Quantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateQuantity(int quantity, int maxItemQuantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (quantity > maxItemQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), $"Quantity cannot exceed {maxItemQuantity}.");
        }
    }
}
