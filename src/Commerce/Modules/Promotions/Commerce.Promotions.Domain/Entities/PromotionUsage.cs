using Commerce.Framework.Core.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Domain.Entities;

public sealed class PromotionUsage : Entity
{
    public int PromotionId { get; private set; }

    public int? CustomerId { get; private set; }

    public int? OrderId { get; private set; }

    public DateTime UsedAtUtc { get; private set; }

    private PromotionUsage()
    {
    }

    public static PromotionUsage Record(int promotionId, int? customerId, int? orderId, DateTime utcNow)
    {
        if (promotionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(promotionId));
        }

        return new PromotionUsage
        {
            PromotionId = promotionId,
            CustomerId = customerId,
            OrderId = orderId,
            UsedAtUtc = utcNow
        };
    }
}
