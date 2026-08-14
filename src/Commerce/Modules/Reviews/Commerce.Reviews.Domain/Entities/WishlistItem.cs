using Commerce.Framework.Core.Entities;

namespace Commerce.Reviews.Domain.Entities;

public sealed class WishlistItem : Entity
{
    public int WishlistId { get; private set; }

    public int ProductId { get; private set; }

    public DateTime AddedAtUtc { get; private set; }

    private WishlistItem()
    {
    }

    internal static WishlistItem Create(int productId, DateTime utcNow)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        return new WishlistItem
        {
            ProductId = productId,
            AddedAtUtc = utcNow
        };
    }
}
