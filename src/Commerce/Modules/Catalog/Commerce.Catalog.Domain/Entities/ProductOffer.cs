using Commerce.Catalog.Domain.Events;
using Commerce.Framework.Core.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductOffer : AggregateRoot
{
    private ProductOffer()
    {
    }

    public int ProductId { get; private set; }

    public int? VariantId { get; private set; }

    public int StoreId { get; private set; }

    public int CurrencyId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public decimal? CompareAtPrice { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? ValidFromUtc { get; private set; }

    public DateTime? ValidToUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static ProductOffer Create(
        int productId,
        int? variantId,
        int storeId,
        int currencyId,
        string currencyCode,
        Money price,
        Money? compareAtPrice = null,
        bool isActive = true,
        DateTime? validFromUtc = null,
        DateTime? validToUtc = null)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (currencyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currencyId));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        if (!price.Currency.Code.Equals(currencyCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Offer currency code must match the price currency.", nameof(currencyCode));
        }

        if (compareAtPrice is not null && compareAtPrice.Currency.Code != price.Currency.Code)
        {
            throw new ArgumentException("Compare-at price currency must match the offer price currency.");
        }

        if (compareAtPrice is not null && compareAtPrice.Amount < price.Amount)
        {
            throw new ArgumentException("Compare-at price cannot be less than price.");
        }

        ValidateDateRange(validFromUtc, validToUtc);

        var now = DateTime.UtcNow;
        var offer = new ProductOffer
        {
            ProductId = productId,
            VariantId = variantId,
            StoreId = storeId,
            CurrencyId = currencyId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Price = price.Amount,
            CompareAtPrice = compareAtPrice?.Amount,
            IsActive = isActive,
            ValidFromUtc = validFromUtc,
            ValidToUtc = validToUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        offer.RaiseDomainEvent(new OfferCreatedEvent(offer.Id, offer.ProductId, offer.VariantId, offer.StoreId));
        return offer;
    }

    public void Update(
        Money price,
        Money? compareAtPrice,
        bool isActive,
        DateTime? validFromUtc,
        DateTime? validToUtc)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (compareAtPrice is not null && compareAtPrice.Currency.Code != price.Currency.Code)
        {
            throw new ArgumentException("Compare-at price currency must match the offer price currency.");
        }

        if (compareAtPrice is not null && compareAtPrice.Amount < price.Amount)
        {
            throw new ArgumentException("Compare-at price cannot be less than price.");
        }

        ValidateDateRange(validFromUtc, validToUtc);

        Price = price.Amount;
        CompareAtPrice = compareAtPrice?.Amount;
        IsActive = isActive;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new OfferUpdatedEvent(Id, ProductId, VariantId, StoreId));
    }

    public bool IsCurrentlyValid(DateTime utcNow)
    {
        if (!IsActive)
        {
            return false;
        }

        if (ValidFromUtc.HasValue && utcNow < ValidFromUtc.Value)
        {
            return false;
        }

        if (ValidToUtc.HasValue && utcNow > ValidToUtc.Value)
        {
            return false;
        }

        return true;
    }

    public Money ToMoney() => Money.Create(Price, Currency.FromCode(CurrencyCode));

    public Money? ToCompareAtMoney() =>
        CompareAtPrice.HasValue ? Money.Create(CompareAtPrice.Value, Currency.FromCode(CurrencyCode)) : null;

    private static void ValidateDateRange(DateTime? validFromUtc, DateTime? validToUtc)
    {
        if (validFromUtc.HasValue && validToUtc.HasValue && validFromUtc.Value > validToUtc.Value)
        {
            throw new ArgumentException("ValidFrom cannot be after ValidTo.");
        }
    }
}
