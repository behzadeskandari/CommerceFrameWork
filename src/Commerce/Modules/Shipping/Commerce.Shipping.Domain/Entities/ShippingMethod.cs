using Commerce.Shipping.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Shipping.Domain.Entities;

public sealed class ShippingMethod : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SystemNameMaxLength = 128;
    public const int DescriptionMaxLength = 2000;
    public const int ProviderSystemNameMaxLength = 128;

    private ShippingMethod()
    {
    }

    public int StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string SystemName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string ProviderSystemName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool RequiresAddress { get; private set; }

    public bool SupportsTracking { get; private set; }

    public int? EstimatedDeliveryDaysMin { get; private set; }

    public int? EstimatedDeliveryDaysMax { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static ShippingMethod Create(
        int storeId,
        string name,
        string systemName,
        string? description,
        string providerSystemName,
        bool isActive,
        int displayOrder,
        bool requiresAddress,
        bool supportsTracking,
        int? estimatedDeliveryDaysMin,
        int? estimatedDeliveryDaysMax)
    {
        ValidateStore(storeId);
        ValidateName(name);
        ValidateSystemName(systemName);
        ValidateProvider(providerSystemName);

        var utcNow = DateTime.UtcNow;
        return new ShippingMethod
        {
            StoreId = storeId,
            Name = name.Trim(),
            SystemName = systemName.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ProviderSystemName = providerSystemName.Trim(),
            IsActive = isActive,
            DisplayOrder = displayOrder,
            RequiresAddress = requiresAddress,
            SupportsTracking = supportsTracking,
            EstimatedDeliveryDaysMin = estimatedDeliveryDaysMin,
            EstimatedDeliveryDaysMax = estimatedDeliveryDaysMax,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(
        string name,
        string? description,
        bool isActive,
        int displayOrder,
        bool requiresAddress,
        bool supportsTracking,
        int? estimatedDeliveryDaysMin,
        int? estimatedDeliveryDaysMax)
    {
        EnsureNotDeleted();
        ValidateName(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;
        DisplayOrder = displayOrder;
        RequiresAddress = requiresAddress;
        SupportsTracking = supportsTracking;
        EstimatedDeliveryDaysMin = estimatedDeliveryDaysMin;
        EstimatedDeliveryDaysMax = estimatedDeliveryDaysMax;
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public string? FormatEstimatedDelivery()
    {
        if (!EstimatedDeliveryDaysMin.HasValue && !EstimatedDeliveryDaysMax.HasValue)
        {
            return null;
        }

        if (EstimatedDeliveryDaysMin == EstimatedDeliveryDaysMax)
        {
            return EstimatedDeliveryDaysMin == 1 ? "1 business day" : $"{EstimatedDeliveryDaysMin} business days";
        }

        return $"{EstimatedDeliveryDaysMin}-{EstimatedDeliveryDaysMax} business days";
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Shipping method has been deleted.");
        }
    }

    private static void ValidateStore(int storeId)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
    }

    private static void ValidateSystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }
    }

    private static void ValidateProvider(string providerSystemName)
    {
        if (string.IsNullOrWhiteSpace(providerSystemName))
        {
            throw new ArgumentException("Provider system name is required.", nameof(providerSystemName));
        }
    }
}

public sealed class ShippingRate : AggregateRoot
{
    private ShippingRate()
    {
    }

    public int StoreId { get; private set; }

    public int ShippingMethodId { get; private set; }

    public int? ShippingZoneId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public ShippingRateType RateType { get; private set; }

    public decimal BasePrice { get; private set; }

    public decimal? PricePerWeightUnit { get; private set; }

    public decimal? PricePerQuantityUnit { get; private set; }

    public decimal? OrderSubtotalPercentage { get; private set; }

    public decimal? FreeShippingThreshold { get; private set; }

    public decimal? MinOrderSubtotal { get; private set; }

    public decimal? MaxOrderSubtotal { get; private set; }

    public decimal? MinWeightGrams { get; private set; }

    public decimal? MaxWeightGrams { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static ShippingRate CreateFlat(
        int storeId,
        int shippingMethodId,
        int? shippingZoneId,
        string currencyCode,
        decimal basePrice,
        decimal? freeShippingThreshold,
        decimal? minOrderSubtotal,
        decimal? maxOrderSubtotal,
        decimal? pricePerWeightUnit = null)
    {
        ValidateCore(storeId, shippingMethodId, currencyCode, basePrice);

        var utcNow = DateTime.UtcNow;
        return new ShippingRate
        {
            StoreId = storeId,
            ShippingMethodId = shippingMethodId,
            ShippingZoneId = shippingZoneId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            RateType = ShippingRateType.Flat,
            BasePrice = basePrice,
            PricePerWeightUnit = pricePerWeightUnit,
            FreeShippingThreshold = freeShippingThreshold,
            MinOrderSubtotal = minOrderSubtotal,
            MaxOrderSubtotal = maxOrderSubtotal,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public static ShippingRate CreateWeightBased(
        int storeId,
        int shippingMethodId,
        int? shippingZoneId,
        string currencyCode,
        decimal basePrice,
        decimal pricePerWeightUnit,
        decimal? minWeightGrams,
        decimal? maxWeightGrams,
        decimal? freeShippingThreshold = null)
    {
        ValidateCore(storeId, shippingMethodId, currencyCode, basePrice);
        if (pricePerWeightUnit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pricePerWeightUnit));
        }

        var utcNow = DateTime.UtcNow;
        return new ShippingRate
        {
            StoreId = storeId,
            ShippingMethodId = shippingMethodId,
            ShippingZoneId = shippingZoneId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            RateType = ShippingRateType.WeightBased,
            BasePrice = basePrice,
            PricePerWeightUnit = pricePerWeightUnit,
            MinWeightGrams = minWeightGrams,
            MaxWeightGrams = maxWeightGrams,
            FreeShippingThreshold = freeShippingThreshold,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public static ShippingRate CreateOrderSubtotalBased(
        int storeId,
        int shippingMethodId,
        int? shippingZoneId,
        string currencyCode,
        decimal basePrice,
        decimal orderSubtotalPercentage,
        decimal? minOrderSubtotal,
        decimal? maxOrderSubtotal,
        decimal? freeShippingThreshold = null)
    {
        ValidateCore(storeId, shippingMethodId, currencyCode, basePrice);
        if (orderSubtotalPercentage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderSubtotalPercentage));
        }

        var utcNow = DateTime.UtcNow;
        return new ShippingRate
        {
            StoreId = storeId,
            ShippingMethodId = shippingMethodId,
            ShippingZoneId = shippingZoneId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            RateType = ShippingRateType.OrderSubtotalBased,
            BasePrice = basePrice,
            OrderSubtotalPercentage = orderSubtotalPercentage,
            MinOrderSubtotal = minOrderSubtotal,
            MaxOrderSubtotal = maxOrderSubtotal,
            FreeShippingThreshold = freeShippingThreshold,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public static ShippingRate CreateQuantityBased(
        int storeId,
        int shippingMethodId,
        int? shippingZoneId,
        string currencyCode,
        decimal basePrice,
        decimal pricePerQuantityUnit,
        decimal? freeShippingThreshold = null)
    {
        ValidateCore(storeId, shippingMethodId, currencyCode, basePrice);
        if (pricePerQuantityUnit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pricePerQuantityUnit));
        }

        var utcNow = DateTime.UtcNow;
        return new ShippingRate
        {
            StoreId = storeId,
            ShippingMethodId = shippingMethodId,
            ShippingZoneId = shippingZoneId,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            RateType = ShippingRateType.QuantityBased,
            BasePrice = basePrice,
            PricePerQuantityUnit = pricePerQuantityUnit,
            FreeShippingThreshold = freeShippingThreshold,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(
        decimal basePrice,
        decimal? pricePerWeightUnit,
        decimal? pricePerQuantityUnit,
        decimal? orderSubtotalPercentage,
        decimal? freeShippingThreshold,
        decimal? minOrderSubtotal,
        decimal? maxOrderSubtotal,
        decimal? minWeightGrams,
        decimal? maxWeightGrams,
        bool isActive)
    {
        EnsureNotDeleted();
        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice));
        }

        BasePrice = basePrice;
        PricePerWeightUnit = pricePerWeightUnit;
        PricePerQuantityUnit = pricePerQuantityUnit;
        OrderSubtotalPercentage = orderSubtotalPercentage;
        FreeShippingThreshold = freeShippingThreshold;
        MinOrderSubtotal = minOrderSubtotal;
        MaxOrderSubtotal = maxOrderSubtotal;
        MinWeightGrams = minWeightGrams;
        MaxWeightGrams = maxWeightGrams;
        IsActive = isActive;
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public bool MatchesOrderSubtotal(decimal subtotal)
    {
        if (MinOrderSubtotal.HasValue && subtotal < MinOrderSubtotal.Value)
        {
            return false;
        }

        if (MaxOrderSubtotal.HasValue && subtotal > MaxOrderSubtotal.Value)
        {
            return false;
        }

        return true;
    }

    public bool MatchesWeight(decimal weightGrams)
    {
        if (MinWeightGrams.HasValue && weightGrams < MinWeightGrams.Value)
        {
            return false;
        }

        if (MaxWeightGrams.HasValue && weightGrams > MaxWeightGrams.Value)
        {
            return false;
        }

        return true;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Shipping rate has been deleted.");
        }
    }

    private static void ValidateCore(int storeId, int shippingMethodId, string currencyCode, decimal basePrice)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (shippingMethodId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shippingMethodId));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        if (basePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basePrice));
        }
    }
}
