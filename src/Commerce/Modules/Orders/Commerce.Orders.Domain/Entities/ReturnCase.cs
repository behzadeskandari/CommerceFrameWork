using Commerce.Framework.Core.Entities;
using Commerce.Orders.Domain.Enums;

namespace Commerce.Orders.Domain.Entities;

public sealed class ReturnCase : AggregateRoot
{
    public const int ReasonMaxLength = 2000;
    public const int NotesMaxLength = 4000;
    public const int TrackingNumberMaxLength = 128;

    private readonly List<ReturnCaseItem> _items = [];

    private ReturnCase()
    {
    }

    public int OrderId { get; private set; }

    public int StoreId { get; private set; }

    public int? CustomerId { get; private set; }

    public ReturnStatus Status { get; private set; }

    public ReturnResolutionType ResolutionType { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string? CustomerNotes { get; private set; }

    public string? AdminNotes { get; private set; }

    public string? ReturnTrackingNumber { get; private set; }

    public decimal RefundAmount { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public int? RefundId { get; private set; }

    public int? ReplacementOrderId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ReturnCaseItem> Items => _items;

    public static ReturnCase Create(
        int orderId,
        int storeId,
        int? customerId,
        ReturnResolutionType resolutionType,
        string reason,
        string currencyCode,
        string? customerNotes,
        IEnumerable<ReturnCaseItem> items)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Return reason is required.", nameof(reason));
        }

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new InvalidOperationException("Return must include at least one item.");
        }

        var utcNow = DateTime.UtcNow;
        var returnCase = new ReturnCase
        {
            OrderId = orderId,
            StoreId = storeId,
            CustomerId = customerId,
            Status = ReturnStatus.Requested,
            ResolutionType = resolutionType,
            Reason = reason.Trim(),
            CustomerNotes = string.IsNullOrWhiteSpace(customerNotes) ? null : customerNotes.Trim(),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        returnCase._items.AddRange(itemList);
        return returnCase;
    }

    public void Approve(string? adminNotes)
    {
        EnsureStatus(ReturnStatus.Requested);
        Status = ReturnStatus.Approved;
        AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? AdminNotes : adminNotes.Trim();
        Touch();
    }

    public void Reject(string reason)
    {
        EnsureStatus(ReturnStatus.Requested, ReturnStatus.Approved);
        Status = ReturnStatus.Rejected;
        AdminNotes = reason.Trim();
        Touch();
    }

    public void SetReturnShipment(string? trackingNumber)
    {
        EnsureStatus(ReturnStatus.Approved);
        Status = ReturnStatus.ShipmentPending;
        ReturnTrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        Touch();
    }

    public void MarkReceived()
    {
        EnsureStatus(ReturnStatus.Approved, ReturnStatus.ShipmentPending);
        Status = ReturnStatus.Received;
        Touch();
    }

    public void MarkRestocked()
    {
        EnsureStatus(ReturnStatus.Received);
        Status = ReturnStatus.Restocked;
        Touch();
    }

    public void RecordRefund(decimal amount, int refundId)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        RefundAmount = amount;
        RefundId = refundId;
        Status = ReturnStatus.Refunded;
        Touch();
    }

    public void Complete(int? replacementOrderId = null)
    {
        EnsureStatus(ReturnStatus.Restocked, ReturnStatus.Refunded, ReturnStatus.Received);
        Status = ReturnStatus.Completed;
        ReplacementOrderId = replacementOrderId;
        Touch();
    }

    public void Cancel(string reason)
    {
        if (Status is ReturnStatus.Completed or ReturnStatus.Refunded or ReturnStatus.Rejected)
        {
            throw new InvalidOperationException($"Return cannot be cancelled from status {Status}.");
        }

        Status = ReturnStatus.Cancelled;
        AdminNotes = reason.Trim();
        Touch();
    }

    private void EnsureStatus(params ReturnStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidOperationException($"Invalid return status transition from {Status}.");
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

public sealed class ReturnCaseItem : Entity
{
    private ReturnCaseItem()
    {
    }

    public int ReturnCaseId { get; private set; }

    public int OrderItemId { get; private set; }

    public int OfferId { get; private set; }

    public int ProductId { get; private set; }

    public int Quantity { get; private set; }

    public decimal RefundAmount { get; private set; }

    public static ReturnCaseItem Create(int orderItemId, int offerId, int productId, int quantity)
    {
        if (orderItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderItemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return new ReturnCaseItem
        {
            OrderItemId = orderItemId,
            OfferId = offerId,
            ProductId = productId,
            Quantity = quantity
        };
    }

    public void SetRefundAmount(decimal amount) => RefundAmount = amount;
}
