using Commerce.Framework.Core.Entities;

namespace Commerce.Tax.Domain.Entities;

public sealed class TaxCategory : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SystemNameMaxLength = 128;

    private TaxCategory()
    {
    }

    public int StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string SystemName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsExempt { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TaxCategory Create(
        int storeId,
        string name,
        string systemName,
        string? description,
        bool isExempt,
        bool isActive,
        int displayOrder)
    {
        ValidateStore(storeId);
        ValidateName(name);
        ValidateSystemName(systemName);

        var utcNow = DateTime.UtcNow;
        return new TaxCategory
        {
            StoreId = storeId,
            Name = name.Trim(),
            SystemName = systemName.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsExempt = isExempt,
            IsActive = isActive,
            DisplayOrder = displayOrder,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(
        string name,
        string? description,
        bool isExempt,
        bool isActive,
        int displayOrder)
    {
        EnsureNotDeleted();
        ValidateName(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsExempt = isExempt;
        IsActive = isActive;
        DisplayOrder = displayOrder;
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Tax category has been deleted.");
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
}

public sealed class TaxRate : AggregateRoot
{
    private TaxRate()
    {
    }

    public int StoreId { get; private set; }

    public int TaxCategoryId { get; private set; }

    public int? TaxZoneId { get; private set; }

    public Commerce.Tax.Domain.Enums.TaxRateType RateType { get; private set; }

    public decimal Percentage { get; private set; }

    public decimal? FixedAmount { get; private set; }

    public bool TaxShipping { get; private set; }

    public int Priority { get; private set; }

    public DateTime? EffectiveFromUtc { get; private set; }

    public DateTime? EffectiveToUtc { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TaxRate CreatePercentage(
        int storeId,
        int taxCategoryId,
        int? taxZoneId,
        decimal percentage,
        bool taxShipping,
        int priority,
        DateTime? effectiveFromUtc,
        DateTime? effectiveToUtc)
    {
        ValidateCore(storeId, taxCategoryId, percentage);

        var utcNow = DateTime.UtcNow;
        return new TaxRate
        {
            StoreId = storeId,
            TaxCategoryId = taxCategoryId,
            TaxZoneId = taxZoneId,
            RateType = Commerce.Tax.Domain.Enums.TaxRateType.Percentage,
            Percentage = percentage,
            FixedAmount = null,
            TaxShipping = taxShipping,
            Priority = priority,
            EffectiveFromUtc = effectiveFromUtc,
            EffectiveToUtc = effectiveToUtc,
            IsActive = true,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(
        decimal percentage,
        bool taxShipping,
        int priority,
        DateTime? effectiveFromUtc,
        DateTime? effectiveToUtc,
        bool isActive)
    {
        EnsureNotDeleted();
        if (percentage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage));
        }

        Percentage = percentage;
        TaxShipping = taxShipping;
        Priority = priority;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        IsActive = isActive;
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public bool IsEffective(DateTime utcNow)
    {
        if (!IsActive || IsDeleted)
        {
            return false;
        }

        if (EffectiveFromUtc.HasValue && utcNow < EffectiveFromUtc.Value)
        {
            return false;
        }

        if (EffectiveToUtc.HasValue && utcNow > EffectiveToUtc.Value)
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
            throw new InvalidOperationException("Tax rate has been deleted.");
        }
    }

    private static void ValidateCore(int storeId, int taxCategoryId, decimal percentage)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (taxCategoryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taxCategoryId));
        }

        if (percentage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage));
        }
    }
}
