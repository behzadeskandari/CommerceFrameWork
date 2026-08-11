using Commerce.Orders.Domain.Enums;
using Commerce.Orders.Domain.Events;
using Commerce.Orders.Domain.ValueObjects;
using Commerce.Framework.Core.Entities;

namespace Commerce.Orders.Domain.Entities;

public sealed class Order : AggregateRoot
{
    public const int OrderNumberMaxLength = 32;
    public const int CurrencyCodeMaxLength = 8;
    public const int EmailMaxLength = 500;
    public const int DisplayNameMaxLength = 400;
    public const int AccessTokenMaxLength = 128;
    public const int MethodIdMaxLength = 128;
    public const int ProviderSystemNameMaxLength = 128;

    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];

    private Order()
    {
    }

    public string OrderNumber { get; private set; } = string.Empty;

    public int StoreId { get; private set; }

    public int CheckoutId { get; private set; }

    public int CartId { get; private set; }

    public int? CustomerId { get; private set; }

    public string? GuestEmail { get; private set; }

    public string? CustomerEmail { get; private set; }

    public string? CustomerDisplayName { get; private set; }

    public string? GuestAccessToken { get; private set; }

    public int CurrencyId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; }

    public FulfillmentStatus FulfillmentStatus { get; private set; }

    public bool RequiresShipping { get; private set; }

    public OrderAddressSnapshot? BillingAddress { get; private set; }

    public OrderAddressSnapshot? ShippingAddress { get; private set; }

    public string? SelectedShippingMethodId { get; private set; }

    public string? SelectedShippingProviderSystemName { get; private set; }

    public string? SelectedPaymentMethodId { get; private set; }

    public string? SelectedPaymentMethodSystemName { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal DiscountTotal { get; private set; }

    public decimal ShippingTotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal GrandTotal { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<OrderItem> Items => _items;

    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory;

    public static Order CreateFromCheckout(
        string orderNumber,
        int storeId,
        int checkoutId,
        int cartId,
        int? customerId,
        string? guestEmail,
        string? customerEmail,
        string? customerDisplayName,
        string? guestAccessToken,
        int currencyId,
        string currencyCode,
        bool requiresShipping,
        OrderAddressSnapshot? billingAddress,
        OrderAddressSnapshot? shippingAddress,
        string? selectedShippingMethodId,
        string? selectedShippingProviderSystemName,
        string? selectedPaymentMethodId,
        string? selectedPaymentMethodSystemName,
        decimal subtotal,
        decimal discountTotal,
        decimal shippingTotal,
        decimal taxTotal,
        decimal grandTotal,
        IEnumerable<OrderItem> items)
    {
        ValidateOrderNumber(orderNumber);
        ValidateStore(storeId);
        ValidateCheckout(checkoutId);
        ValidateCart(cartId);
        ValidateCurrency(currencyId, currencyCode);
        ValidateTotals(subtotal, discountTotal, shippingTotal, taxTotal, grandTotal);

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        var utcNow = DateTime.UtcNow;
        var order = new Order
        {
            OrderNumber = orderNumber,
            StoreId = storeId,
            CheckoutId = checkoutId,
            CartId = cartId,
            CustomerId = customerId,
            GuestEmail = NormalizeEmail(guestEmail),
            CustomerEmail = NormalizeEmail(customerEmail ?? guestEmail),
            CustomerDisplayName = NormalizeOptional(customerDisplayName, DisplayNameMaxLength),
            GuestAccessToken = guestAccessToken,
            CurrencyId = currencyId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            FulfillmentStatus = FulfillmentStatus.Unfulfilled,
            RequiresShipping = requiresShipping,
            BillingAddress = billingAddress,
            ShippingAddress = shippingAddress,
            SelectedShippingMethodId = NormalizeOptional(selectedShippingMethodId, MethodIdMaxLength),
            SelectedShippingProviderSystemName = NormalizeOptional(selectedShippingProviderSystemName, ProviderSystemNameMaxLength),
            SelectedPaymentMethodId = NormalizeOptional(selectedPaymentMethodId, MethodIdMaxLength),
            SelectedPaymentMethodSystemName = NormalizeOptional(selectedPaymentMethodSystemName, ProviderSystemNameMaxLength),
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            ShippingTotal = shippingTotal,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        order._items.AddRange(itemList);
        order.AddHistory(OrderStatusHistoryType.Order, null, OrderStatus.Pending.ToString(), "Order created.");
        order.RaiseDomainEvent(new OrderCreatedEvent(order.OrderNumber, order.StoreId, order.CustomerId));
        return order;
    }

    public void Cancel(string reason, string? actor = null)
    {
        if (Status is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            throw new InvalidOperationException($"Order cannot be cancelled from status {Status}.");
        }

        var previous = Status;
        Status = OrderStatus.Cancelled;
        if (FulfillmentStatus is not FulfillmentStatus.Cancelled)
        {
            AddHistory(OrderStatusHistoryType.Fulfillment, FulfillmentStatus.ToString(), FulfillmentStatus.Cancelled.ToString(), reason, actor);
            FulfillmentStatus = FulfillmentStatus.Cancelled;
        }

        AddHistory(OrderStatusHistoryType.Order, previous.ToString(), Status.ToString(), reason, actor);
        RaiseDomainEvent(new OrderCancelledEvent(Id, OrderNumber, reason));
        Touch();
    }

    public bool IsOwnedByCustomer(int customerId) =>
        CustomerId.HasValue && CustomerId.Value == customerId;

    public bool IsAccessibleByGuest(string accessToken) =>
        !CustomerId.HasValue &&
        !string.IsNullOrWhiteSpace(GuestAccessToken) &&
        !string.IsNullOrWhiteSpace(accessToken) &&
        string.Equals(GuestAccessToken, accessToken, StringComparison.Ordinal);

    private void AddHistory(
        OrderStatusHistoryType historyType,
        string? fromStatus,
        string toStatus,
        string reason,
        string? actor = null)
    {
        _statusHistory.Add(OrderStatusHistory.Create(
            Id,
            historyType,
            fromStatus,
            toStatus,
            reason,
            actor));
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private static void ValidateOrderNumber(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new ArgumentException("Order number is required.", nameof(orderNumber));
        }
    }

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }
    }

    private static void ValidateCheckout(int checkoutId)
    {
        if (checkoutId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(checkoutId));
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

    private static void ValidateTotals(
        decimal subtotal,
        decimal discountTotal,
        decimal shippingTotal,
        decimal taxTotal,
        decimal grandTotal)
    {
        var calculated = subtotal - discountTotal + shippingTotal + taxTotal;
        if (calculated != grandTotal)
        {
            throw new InvalidOperationException("Order totals are inconsistent.");
        }
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        return trimmed.Length > EmailMaxLength ? trimmed[..EmailMaxLength] : trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
