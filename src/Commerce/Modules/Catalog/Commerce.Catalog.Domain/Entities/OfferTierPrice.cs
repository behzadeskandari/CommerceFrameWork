using Commerce.Framework.Core.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Catalog.Domain.Entities;

public sealed class OfferTierPrice : Entity
{
    public int OfferId { get; private set; }

    public int MinQuantity { get; private set; }

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static OfferTierPrice Create(int offerId, int minQuantity, Money price, bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (offerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offerId));
        }

        if (minQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minQuantity));
        }

        return new OfferTierPrice
        {
            OfferId = offerId,
            MinQuantity = minQuantity,
            Price = price.Amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(int minQuantity, Money price, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (minQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minQuantity));
        }

        MinQuantity = minQuantity;
        Price = price.Amount;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
