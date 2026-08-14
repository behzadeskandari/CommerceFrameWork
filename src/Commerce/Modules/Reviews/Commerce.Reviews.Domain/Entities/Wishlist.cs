using Commerce.Framework.Core.Entities;

namespace Commerce.Reviews.Domain.Entities;

public sealed class Wishlist : AggregateRoot
{
    private readonly List<WishlistItem> _items = [];

    public int CustomerId { get; private set; }

    public int StoreId { get; private set; }

    public IReadOnlyCollection<WishlistItem> Items => _items;

    private Wishlist()
    {
    }

    public static Wishlist Create(int customerId, int storeId)
    {
        if (customerId <= 0 || storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }

        return new Wishlist
        {
            CustomerId = customerId,
            StoreId = storeId
        };
    }

    public bool IsOwnedBy(int customerId) => CustomerId == customerId;

    public bool ContainsProduct(int productId) => _items.Any(x => x.ProductId == productId);

    public WishlistItem AddProduct(int productId, DateTime utcNow)
    {
        if (ContainsProduct(productId))
        {
            throw new InvalidOperationException("Product is already in the wishlist.");
        }

        var item = WishlistItem.Create(productId, utcNow);
        _items.Add(item);
        return item;
    }

    public bool RemoveProduct(int productId)
    {
        var item = _items.FirstOrDefault(x => x.ProductId == productId);
        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        return true;
    }
}
