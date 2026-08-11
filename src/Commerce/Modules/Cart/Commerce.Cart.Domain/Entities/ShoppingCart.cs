using Commerce.Cart.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Cart.Domain.Entities;

public sealed class ShoppingCart : AggregateRoot
{
    public const int GuestTokenMaxLength = 128;
    public const int CurrencyCodeMaxLength = 8;

    private readonly List<CartItem> _items = [];

    private ShoppingCart()
    {
    }

    public int StoreId { get; private set; }

    public int? CustomerId { get; private set; }

    public string? GuestToken { get; private set; }

    public int CurrencyId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public CartStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items;

    public static ShoppingCart CreateForCustomer(
        int storeId,
        int customerId,
        int currencyId,
        string currencyCode,
        DateTime expiresAtUtc)
    {
        ValidateStore(storeId);
        ValidateCustomer(customerId);
        ValidateCurrency(currencyId, currencyCode);
        ValidateExpiration(expiresAtUtc);

        var utcNow = DateTime.UtcNow;
        return new ShoppingCart
        {
            StoreId = storeId,
            CustomerId = customerId,
            GuestToken = null,
            CurrencyId = currencyId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = CartStatus.Active,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public static ShoppingCart CreateForGuest(
        int storeId,
        string guestToken,
        int currencyId,
        string currencyCode,
        DateTime expiresAtUtc)
    {
        ValidateStore(storeId);
        ValidateGuestToken(guestToken);
        ValidateCurrency(currencyId, currencyCode);
        ValidateExpiration(expiresAtUtc);

        var utcNow = DateTime.UtcNow;
        return new ShoppingCart
        {
            StoreId = storeId,
            CustomerId = null,
            GuestToken = guestToken.Trim(),
            CurrencyId = currencyId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = CartStatus.Active,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    public void EnsureModifiable(DateTime utcNow)
    {
        if (Status is CartStatus.Converted)
        {
            throw new InvalidOperationException("Cart has already been converted.");
        }

        if (Status is CartStatus.Expired || utcNow >= ExpiresAtUtc)
        {
            throw new InvalidOperationException("Cart has expired.");
        }

        if (Status is not CartStatus.Active)
        {
            throw new InvalidOperationException("Cart is not active.");
        }
    }

    public CartItem AddOrIncreaseItem(int offerId, int quantity, int maxItemQuantity, int maxDistinctItems)
    {
        EnsureModifiable(DateTime.UtcNow);
        ValidateQuantity(quantity, maxItemQuantity);

        var existing = _items.FirstOrDefault(x => x.OfferId == offerId);
        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity, maxItemQuantity);
            Touch();
            return existing;
        }

        if (_items.Count >= maxDistinctItems)
        {
            throw new InvalidOperationException($"Cart cannot contain more than {maxDistinctItems} distinct items.");
        }

        var item = CartItem.Create(Id, offerId, quantity, maxItemQuantity);
        _items.Add(item);
        Touch();
        return item;
    }

    public CartItem UpdateItemQuantity(int cartItemId, int quantity, int maxItemQuantity)
    {
        EnsureModifiable(DateTime.UtcNow);
        ValidateQuantity(quantity, maxItemQuantity);

        var item = GetItem(cartItemId);
        item.SetQuantity(quantity, maxItemQuantity);
        Touch();
        return item;
    }

    public void RemoveItem(int cartItemId)
    {
        EnsureModifiable(DateTime.UtcNow);
        var item = GetItem(cartItemId);
        _items.Remove(item);
        Touch();
    }

    public void ClearItems()
    {
        EnsureModifiable(DateTime.UtcNow);
        _items.Clear();
        Touch();
    }

    public void MarkConverted()
    {
        Status = CartStatus.Converted;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        Status = CartStatus.Expired;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAbandoned()
    {
        if (Status is CartStatus.Active)
        {
            Status = CartStatus.Abandoned;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public void ExtendExpiration(DateTime expiresAtUtc)
    {
        ValidateExpiration(expiresAtUtc);
        ExpiresAtUtc = expiresAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void LoadItems(IEnumerable<CartItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    private CartItem GetItem(int cartItemId)
    {
        var item = _items.FirstOrDefault(x => x.Id == cartItemId);
        if (item is null)
        {
            throw new InvalidOperationException($"Cart item '{cartItemId}' was not found.");
        }

        return item;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId), "Store id is required.");
        }
    }

    private static void ValidateCustomer(int customerId)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId), "Customer id is required.");
        }
    }

    private static void ValidateGuestToken(string guestToken)
    {
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            throw new ArgumentException("Guest token is required.", nameof(guestToken));
        }

        if (guestToken.Length > GuestTokenMaxLength)
        {
            throw new ArgumentException($"Guest token cannot exceed {GuestTokenMaxLength} characters.", nameof(guestToken));
        }
    }

    private static void ValidateCurrency(int currencyId, string currencyCode)
    {
        if (currencyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currencyId), "Currency id is required.");
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }
    }

    private static void ValidateExpiration(DateTime expiresAtUtc)
    {
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Expiration must be in the future.");
        }
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
