using Commerce.Orders.Domain.Enums;

namespace Commerce.Orders.Domain.Entities;

public sealed class OrderStatusHistory : Commerce.Framework.Core.Entities.Entity
{
    public const int StatusMaxLength = 64;
    public const int ReasonMaxLength = 500;
    public const int ActorMaxLength = 200;

    private OrderStatusHistory()
    {
    }

    public int OrderId { get; private set; }

    public OrderStatusHistoryType HistoryType { get; private set; }

    public string? FromStatus { get; private set; }

    public string ToStatus { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public string? Actor { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static OrderStatusHistory Create(
        int orderId,
        OrderStatusHistoryType historyType,
        string? fromStatus,
        string toStatus,
        string reason,
        string? actor = null)
    {
        if (orderId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        // OrderId may be 0 before the aggregate is persisted.

        if (string.IsNullOrWhiteSpace(toStatus))
        {
            throw new ArgumentException("To status is required.", nameof(toStatus));
        }

        return new OrderStatusHistory
        {
            OrderId = orderId,
            HistoryType = historyType,
            FromStatus = TrimOptional(fromStatus, StatusMaxLength),
            ToStatus = toStatus.Trim(),
            Reason = string.IsNullOrWhiteSpace(reason) ? "Status changed." : TrimOptional(reason, ReasonMaxLength)!,
            Actor = TrimOptional(actor, ActorMaxLength),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
