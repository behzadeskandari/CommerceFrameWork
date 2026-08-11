using Commerce.Checkout.Domain.Enums;
using Commerce.Checkout.Domain.ValueObjects;
using Commerce.Framework.Core.Entities;

namespace Commerce.Checkout.Domain.Entities;

public sealed class CheckoutSession : AggregateRoot
{
    public const int GuestTokenMaxLength = 128;
    public const int CurrencyCodeMaxLength = 8;
    public const int EmailMaxLength = 500;
    public const int MethodIdMaxLength = 128;
    public const int ProviderSystemNameMaxLength = 128;

    private readonly List<CheckoutSessionItem> _items = [];

    private CheckoutSession()
    {
    }

    public int StoreId { get; private set; }

    public int CartId { get; private set; }

    public int? CustomerId { get; private set; }

    public string? GuestToken { get; private set; }

    public string? GuestEmail { get; private set; }

    public int CurrencyId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public CheckoutStatus Status { get; private set; }

    public bool RequiresShipping { get; private set; }

    public bool UseShippingAsBilling { get; private set; }

    public CheckoutAddressSnapshot? BillingAddress { get; private set; }

    public CheckoutAddressSnapshot? ShippingAddress { get; private set; }

    public string? SelectedShippingMethodId { get; private set; }

    public string? SelectedShippingProviderSystemName { get; private set; }

    public decimal SelectedShippingPrice { get; private set; }

    public string? SelectedPaymentMethodId { get; private set; }

    public string? SelectedPaymentMethodSystemName { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal DiscountTotal { get; private set; }

    public decimal ShippingTotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal GrandTotal { get; private set; }

    public DateTime CartUpdatedAtUtc { get; private set; }

    public bool PriceChangeDetected { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<CheckoutSessionItem> Items => _items;

    public static CheckoutSession Create(
        int storeId,
        int cartId,
        int? customerId,
        string? guestToken,
        int currencyId,
        string currencyCode,
        bool requiresShipping,
        DateTime cartUpdatedAtUtc,
        DateTime expiresAtUtc,
        IEnumerable<CheckoutSessionItem> items)
    {
        ValidateStore(storeId);
        ValidateCart(cartId);
        ValidateCurrency(currencyId, currencyCode);
        ValidateExpiration(expiresAtUtc);
        ValidateOwnership(customerId, guestToken);

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new InvalidOperationException("Checkout cannot start with an empty cart.");
        }

        var utcNow = DateTime.UtcNow;
        var session = new CheckoutSession
        {
            StoreId = storeId,
            CartId = cartId,
            CustomerId = customerId,
            GuestToken = guestToken?.Trim(),
            CurrencyId = currencyId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = CheckoutStatus.Active,
            RequiresShipping = requiresShipping,
            CartUpdatedAtUtc = cartUpdatedAtUtc,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            ExpiresAtUtc = expiresAtUtc
        };

        session._items.AddRange(itemList);
        session.RecalculateSubtotalFromItems();
        session.RecalculateGrandTotal();
        return session;
    }

    public void EnsureModifiable(DateTime utcNow)
    {
        if (Status is CheckoutStatus.Expired or CheckoutStatus.Completed or CheckoutStatus.Cancelled)
        {
            throw new InvalidOperationException($"Checkout is {Status.ToString().ToLowerInvariant()}.");
        }

        if (utcNow >= ExpiresAtUtc)
        {
            throw new InvalidOperationException("Checkout has expired.");
        }

        if (Status is CheckoutStatus.ReadyForOrder)
        {
            throw new InvalidOperationException("Checkout is ready for order and cannot be modified without refresh.");
        }
    }

    public bool IsOwnedBy(int storeId, int? customerId, string? guestToken)
    {
        if (StoreId != storeId)
        {
            return false;
        }

        if (CustomerId.HasValue)
        {
            return customerId.HasValue && CustomerId.Value == customerId.Value;
        }

        return !string.IsNullOrWhiteSpace(GuestToken) &&
               !string.IsNullOrWhiteSpace(guestToken) &&
               string.Equals(GuestToken, guestToken, StringComparison.Ordinal);
    }

    public void SetGuestEmail(string email)
    {
        if (CustomerId.HasValue)
        {
            throw new InvalidOperationException("Guest email cannot be set for authenticated checkout.");
        }

        GuestEmail = RequireEmail(email);
        Touch();
    }

    public void SetBillingAddress(CheckoutAddressSnapshot address, bool useShippingAsBilling)
    {
        ArgumentNullException.ThrowIfNull(address);
        BillingAddress = address;
        UseShippingAsBilling = useShippingAsBilling;
        if (useShippingAsBilling)
        {
            ShippingAddress = address;
        }

        MarkRequiresReviewIfReady();
        Touch();
    }

    public void SetShippingAddress(CheckoutAddressSnapshot address)
    {
        ArgumentNullException.ThrowIfNull(address);
        ShippingAddress = address;
        if (UseShippingAsBilling)
        {
            BillingAddress = address;
        }

        MarkRequiresReviewIfReady();
        Touch();
    }

    public void SelectShippingMethod(string methodId, string providerSystemName, decimal price)
    {
        SelectedShippingMethodId = RequireMethodId(methodId);
        SelectedShippingProviderSystemName = RequireProvider(providerSystemName);
        SelectedShippingPrice = price;
        ShippingTotal = price;
        MarkRequiresReviewIfReady();
        RecalculateGrandTotal();
        Touch();
    }

    public void ClearShippingSelection()
    {
        SelectedShippingMethodId = null;
        SelectedShippingProviderSystemName = null;
        SelectedShippingPrice = 0m;
        ShippingTotal = 0m;
        MarkRequiresReviewIfReady();
        RecalculateGrandTotal();
        Touch();
    }

    public void SelectPaymentMethod(string methodId, string systemName)
    {
        SelectedPaymentMethodId = RequireMethodId(methodId);
        SelectedPaymentMethodSystemName = RequireProvider(systemName);
        MarkRequiresReviewIfReady();
        Touch();
    }

    public void ClearPaymentSelection()
    {
        SelectedPaymentMethodId = null;
        SelectedPaymentMethodSystemName = null;
        MarkRequiresReviewIfReady();
        Touch();
    }

    public void ReplaceItems(IEnumerable<CheckoutSessionItem> items, DateTime cartUpdatedAtUtc, bool priceChangeDetected)
    {
        _items.Clear();
        _items.AddRange(items);
        CartUpdatedAtUtc = cartUpdatedAtUtc;
        PriceChangeDetected = priceChangeDetected;
        Status = priceChangeDetected || Status is CheckoutStatus.ReadyForOrder
            ? CheckoutStatus.RequiresReview
            : CheckoutStatus.Active;
        RecalculateSubtotalFromItems();
        RecalculateGrandTotal();
        Touch();
    }

    public void MarkCartStale(DateTime cartUpdatedAtUtc)
    {
        if (cartUpdatedAtUtc > CartUpdatedAtUtc)
        {
            CartUpdatedAtUtc = cartUpdatedAtUtc;
            Status = CheckoutStatus.RequiresReview;
            Touch();
        }
    }

    public void ApplyTotals(decimal discountTotal, decimal shippingTotal, decimal taxTotal)
    {
        DiscountTotal = discountTotal;
        ShippingTotal = shippingTotal;
        TaxTotal = taxTotal;
        RecalculateGrandTotal();
        Touch();
    }

    public void MarkReadyForOrder()
    {
        if (Status is CheckoutStatus.Expired or CheckoutStatus.Completed or CheckoutStatus.Cancelled)
        {
            throw new InvalidOperationException($"Checkout cannot become ready from status {Status}.");
        }

        Status = CheckoutStatus.ReadyForOrder;
        PriceChangeDetected = false;
        Touch();
    }

    public void MarkRequiresReview()
    {
        if (Status is CheckoutStatus.ReadyForOrder or CheckoutStatus.Active)
        {
            Status = CheckoutStatus.RequiresReview;
            Touch();
        }
    }

    public void MarkExpired()
    {
        Status = CheckoutStatus.Expired;
        Touch();
    }

    public void MarkCancelled()
    {
        Status = CheckoutStatus.Cancelled;
        Touch();
    }

    public void MarkCompleted()
    {
        if (Status is not CheckoutStatus.ReadyForOrder)
        {
            throw new InvalidOperationException($"Checkout cannot be completed from status {Status}.");
        }

        Status = CheckoutStatus.Completed;
        Touch();
    }

    private void MarkRequiresReviewIfReady()
    {
        if (Status is CheckoutStatus.ReadyForOrder)
        {
            Status = CheckoutStatus.RequiresReview;
        }
    }

    private void RecalculateSubtotalFromItems() =>
        Subtotal = _items.Sum(x => x.LineSubtotal);

    private void RecalculateGrandTotal()
    {
        GrandTotal = Subtotal + ShippingTotal + TaxTotal - DiscountTotal;
        if (GrandTotal < 0)
        {
            GrandTotal = 0m;
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }
    }

    private static void ValidateCart(int cartId)
    {
        if (cartId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cartId));
        }
    }

    private static void ValidateCurrency(int currencyId, string currencyCode)
    {
        if (currencyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currencyId));
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
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));
        }
    }

    private static void ValidateOwnership(int? customerId, string? guestToken)
    {
        if (customerId.HasValue && customerId.Value > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(guestToken))
        {
            throw new ArgumentException("Either customer or guest token is required.");
        }
    }

    private static string RequireEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("A valid email is required.");
        }

        var trimmed = email.Trim();
        return trimmed.Length > EmailMaxLength ? trimmed[..EmailMaxLength] : trimmed;
    }

    private static string RequireMethodId(string methodId)
    {
        if (string.IsNullOrWhiteSpace(methodId))
        {
            throw new ArgumentException("Method id is required.");
        }

        var trimmed = methodId.Trim();
        return trimmed.Length > MethodIdMaxLength ? trimmed[..MethodIdMaxLength] : trimmed;
    }

    private static string RequireProvider(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("Provider system name is required.");
        }

        var trimmed = systemName.Trim();
        return trimmed.Length > ProviderSystemNameMaxLength ? trimmed[..ProviderSystemNameMaxLength] : trimmed;
    }
}
