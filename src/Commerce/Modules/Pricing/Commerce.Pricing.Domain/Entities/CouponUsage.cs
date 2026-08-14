using Commerce.Framework.Core.Entities;

namespace Commerce.Pricing.Domain.Entities;

public sealed class CouponUsage : Entity
{
    private CouponUsage()
    {
    }

    public int CouponId { get; private set; }

    public int? CustomerId { get; private set; }

    public int OrderId { get; private set; }

    public DateTime UsedAtUtc { get; private set; }

    public static CouponUsage Create(int couponId, int? customerId, int orderId)
    {
        if (couponId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(couponId));
        }

        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        return new CouponUsage
        {
            CouponId = couponId,
            CustomerId = customerId,
            OrderId = orderId,
            UsedAtUtc = DateTime.UtcNow
        };
    }
}
