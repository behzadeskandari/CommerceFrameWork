using Commerce.Pricing.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Pricing.Domain.Entities;

public sealed class DiscountTarget : Entity
{
    private DiscountTarget()
    {
    }

    public int DiscountId { get; private set; }

    public DiscountTargetType TargetType { get; private set; }

    public int TargetId { get; private set; }

    public static DiscountTarget Create(int discountId, DiscountTargetType targetType, int targetId)
    {
        if (discountId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountId));
        }

        if (targetId <= 0 && targetType is not DiscountTargetType.Cart)
        {
            throw new ArgumentOutOfRangeException(nameof(targetId));
        }

        return new DiscountTarget
        {
            DiscountId = discountId,
            TargetType = targetType,
            TargetId = targetId
        };
    }
}
