using Commerce.Pricing.Domain.Enums;
using Commerce.Pricing.Domain.Events;
using Commerce.Framework.Core.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Pricing.Domain.Entities;

public sealed class Discount : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SystemNameMaxLength = 128;
    public const int DescriptionMaxLength = 2000;
    public const int CurrencyCodeMaxLength = 8;

    private readonly List<DiscountTarget> _targets = [];

    private Discount()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string SystemName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DiscountType DiscountType { get; private set; }

    public decimal Value { get; private set; }

    public string? CurrencyCode { get; private set; }

    public int Priority { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? StartsAtUtc { get; private set; }

    public DateTime? EndsAtUtc { get; private set; }

    public int? StoreId { get; private set; }

    public StackingMode StackingMode { get; private set; }

    public decimal? MaximumDiscountAmount { get; private set; }

    public decimal? MinimumCartSubtotal { get; private set; }

    public int? MinimumQuantity { get; private set; }

    public CustomerEligibility CustomerEligibility { get; private set; }

    public int? SpecificCustomerId { get; private set; }

    public int? CustomerGroupId { get; private set; }

    public DiscountApplicationScope ApplicationScope { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<DiscountTarget> Targets => _targets;

    public static Discount Create(
        string name,
        string systemName,
        string? description,
        DiscountType discountType,
        decimal value,
        string? currencyCode,
        int priority,
        bool isActive,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? storeId,
        StackingMode stackingMode,
        decimal? maximumDiscountAmount,
        decimal? minimumCartSubtotal,
        int? minimumQuantity,
        CustomerEligibility customerEligibility,
        int? specificCustomerId,
        int? customerGroupId,
        DiscountApplicationScope applicationScope,
        IEnumerable<DiscountTarget> targets)
    {
        ValidateName(name);
        ValidateSystemName(systemName);
        ValidateValue(discountType, value);
        ValidateCurrency(discountType, currencyCode);
        ValidateDateRange(startsAtUtc, endsAtUtc);
        ValidateCustomerEligibility(customerEligibility, specificCustomerId, customerGroupId);

        var utcNow = DateTime.UtcNow;
        var discount = new Discount
        {
            Name = name.Trim(),
            SystemName = systemName.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            DiscountType = discountType,
            Value = value,
            CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim().ToUpperInvariant(),
            Priority = priority,
            IsActive = isActive,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            StoreId = storeId,
            StackingMode = stackingMode,
            MaximumDiscountAmount = maximumDiscountAmount,
            MinimumCartSubtotal = minimumCartSubtotal,
            MinimumQuantity = minimumQuantity,
            CustomerEligibility = customerEligibility,
            SpecificCustomerId = specificCustomerId,
            CustomerGroupId = customerGroupId,
            ApplicationScope = applicationScope,
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        discount.SetTargets(targets);
        discount.RaiseDomainEvent(new DiscountCreatedEvent(0, discount.Name));
        return discount;
    }

    public void Update(
        string name,
        string? description,
        DiscountType discountType,
        decimal value,
        string? currencyCode,
        int priority,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? storeId,
        StackingMode stackingMode,
        decimal? maximumDiscountAmount,
        decimal? minimumCartSubtotal,
        int? minimumQuantity,
        CustomerEligibility customerEligibility,
        int? specificCustomerId,
        int? customerGroupId,
        DiscountApplicationScope applicationScope,
        IEnumerable<DiscountTarget> targets)
    {
        EnsureNotDeleted();
        ValidateName(name);
        ValidateValue(discountType, value);
        ValidateCurrency(discountType, currencyCode);
        ValidateDateRange(startsAtUtc, endsAtUtc);
        ValidateCustomerEligibility(customerEligibility, specificCustomerId, customerGroupId);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DiscountType = discountType;
        Value = value;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim().ToUpperInvariant();
        Priority = priority;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        StoreId = storeId;
        StackingMode = stackingMode;
        MaximumDiscountAmount = maximumDiscountAmount;
        MinimumCartSubtotal = minimumCartSubtotal;
        MinimumQuantity = minimumQuantity;
        CustomerEligibility = customerEligibility;
        SpecificCustomerId = specificCustomerId;
        CustomerGroupId = customerGroupId;
        ApplicationScope = applicationScope;
        SetTargets(targets);
        Touch();
        RaiseDomainEvent(new DiscountUpdatedEvent(Id, Name));
    }

    public void Activate()
    {
        EnsureNotDeleted();
        if (!IsActive)
        {
            IsActive = true;
            Touch();
            RaiseDomainEvent(new DiscountActivatedEvent(Id));
        }
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        if (IsActive)
        {
            IsActive = false;
            Touch();
            RaiseDomainEvent(new DiscountDeactivatedEvent(Id));
        }
    }

    public void SoftDelete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        IsActive = false;
        Touch();
    }

    public bool IsCurrentlyValid(DateTime utcNow) =>
        !IsDeleted &&
        IsActive &&
        (!StartsAtUtc.HasValue || utcNow >= StartsAtUtc.Value) &&
        (!EndsAtUtc.HasValue || utcNow <= EndsAtUtc.Value);

    public bool AppliesToStore(int storeId) => !StoreId.HasValue || StoreId.Value == storeId;

    public bool IsEligibleForCustomer(int? customerId, bool isGuest, int? customerGroupId)
    {
        return CustomerEligibility switch
        {
            CustomerEligibility.All => true,
            CustomerEligibility.Authenticated => !isGuest && customerId.HasValue,
            CustomerEligibility.Guest => isGuest,
            CustomerEligibility.SpecificCustomer => customerId.HasValue && SpecificCustomerId == customerId.Value,
            CustomerEligibility.CustomerGroup => customerGroupId.HasValue && CustomerGroupId.HasValue && customerGroupId.Value == CustomerGroupId.Value,
            _ => false
        };
    }

    public void LoadTargets(IEnumerable<DiscountTarget> targets)
    {
        _targets.Clear();
        _targets.AddRange(targets);
    }

    private void SetTargets(IEnumerable<DiscountTarget> targets)
    {
        _targets.Clear();
        foreach (var target in targets)
        {
            _targets.Add(DiscountTarget.Create(Id > 0 ? Id : 0, target.TargetType, target.TargetId));
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Discount has been deleted.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name));
        }
    }

    private static void ValidateSystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }

        if (systemName.Length > SystemNameMaxLength)
        {
            throw new ArgumentException($"System name cannot exceed {SystemNameMaxLength} characters.", nameof(systemName));
        }
    }

    private static void ValidateValue(DiscountType discountType, decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Discount value must be greater than zero.");
        }

        if (discountType is DiscountType.Percentage && value > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage discount cannot exceed 100.");
        }
    }

    private static void ValidateCurrency(DiscountType discountType, string? currencyCode)
    {
        if (discountType is DiscountType.FixedAmount && string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required for fixed amount discounts.", nameof(currencyCode));
        }
    }

    private static void ValidateDateRange(DateTime? startsAtUtc, DateTime? endsAtUtc)
    {
        if (startsAtUtc.HasValue && endsAtUtc.HasValue && startsAtUtc.Value > endsAtUtc.Value)
        {
            throw new ArgumentException("Start date must be before end date.");
        }
    }

    private static void ValidateCustomerEligibility(CustomerEligibility eligibility, int? specificCustomerId, int? customerGroupId)
    {
        if (eligibility is CustomerEligibility.SpecificCustomer && (!specificCustomerId.HasValue || specificCustomerId.Value <= 0))
        {
            throw new ArgumentException("Specific customer id is required for specific customer eligibility.");
        }

        if (eligibility is CustomerEligibility.CustomerGroup && (!customerGroupId.HasValue || customerGroupId.Value <= 0))
        {
            throw new ArgumentException("Customer group id is required for customer group eligibility.");
        }
    }
}
