namespace Commerce.Orders.Domain.Entities;

public sealed class OrderCreationIdempotency : Commerce.Framework.Core.Entities.Entity
{
    public const int IdempotencyKeyMaxLength = 128;

    private OrderCreationIdempotency()
    {
    }

    public int StoreId { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public int CheckoutId { get; private set; }

    public int OrderId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static OrderCreationIdempotency CreatePending(
        int storeId,
        string idempotencyKey,
        int checkoutId) =>
        CreateInternal(storeId, idempotencyKey, checkoutId, 0);

    public static OrderCreationIdempotency Create(
        int storeId,
        string idempotencyKey,
        int checkoutId,
        int orderId) =>
        CreateInternal(storeId, idempotencyKey, checkoutId, orderId);

    public void AssignOrderId(int orderId)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        OrderId = orderId;
    }

    private static OrderCreationIdempotency CreateInternal(
        int storeId,
        string idempotencyKey,
        int checkoutId,
        int orderId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        if (checkoutId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(checkoutId));
        }

        if (orderId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        var trimmed = idempotencyKey.Trim();
        return new OrderCreationIdempotency
        {
            StoreId = storeId,
            IdempotencyKey = trimmed.Length > IdempotencyKeyMaxLength ? trimmed[..IdempotencyKeyMaxLength] : trimmed,
            CheckoutId = checkoutId,
            OrderId = orderId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
