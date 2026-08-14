using Commerce.Framework.Core.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Pricing.Domain.Entities;

public sealed class CustomerGroupPrice : Entity
{
    public int CustomerGroupId { get; private set; }

    public int StoreId { get; private set; }

    public int ProductId { get; private set; }

    public int? VariantId { get; private set; }

    public int CurrencyId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static CustomerGroupPrice Create(
        int customerGroupId,
        int storeId,
        int productId,
        int? variantId,
        int currencyId,
        string currencyCode,
        Money price,
        bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (customerGroupId <= 0 || storeId <= 0 || productId <= 0 || currencyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerGroupId));
        }

        if (!price.Currency.Code.Equals(currencyCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Currency code must match price currency.");
        }

        return new CustomerGroupPrice
        {
            CustomerGroupId = customerGroupId,
            StoreId = storeId,
            ProductId = productId,
            VariantId = variantId,
            CurrencyId = currencyId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            Price = price.Amount,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(Money price, bool isActive)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (!price.Currency.Code.Equals(CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Price currency must match the configured currency.");
        }

        Price = price.Amount;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
