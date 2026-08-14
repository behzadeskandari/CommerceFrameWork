using Commerce.Framework.Core.Entities;
using Commerce.Promotions.Domain.Enums;

namespace Commerce.Promotions.Domain.Entities;

public sealed class Promotion : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int SystemNameMaxLength = 128;
    public const int DescriptionMaxLength = 2000;
    public const int CombinationGroupMaxLength = 64;
    public const int CouponCodeMaxLength = 64;

    private readonly List<PromotionCondition> _conditions = [];
    private readonly List<PromotionAction> _actions = [];

    private Promotion()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string SystemName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? StartsAtUtc { get; private set; }

    public DateTime? EndsAtUtc { get; private set; }

    public int? StoreId { get; private set; }

    public int Priority { get; private set; }

    public PromotionCombinationRule CombinationRule { get; private set; }

    public string? CombinationGroup { get; private set; }

    public int? GlobalUsageLimit { get; private set; }

    public int? PerCustomerUsageLimit { get; private set; }

    public int UsageCount { get; private set; }

    public bool RequiresCouponCode { get; private set; }

    public string? CouponCode { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<PromotionCondition> Conditions => _conditions;

    public IReadOnlyCollection<PromotionAction> Actions => _actions;

    public static Promotion Create(
        string name,
        string systemName,
        string? description,
        bool isActive,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? storeId,
        int priority,
        PromotionCombinationRule combinationRule,
        string? combinationGroup,
        int? globalUsageLimit,
        int? perCustomerUsageLimit,
        bool requiresCouponCode,
        string? couponCode,
        IEnumerable<PromotionCondition> conditions,
        IEnumerable<PromotionAction> actions)
    {
        ValidateName(name);
        ValidateSystemName(systemName);
        ValidateDateRange(startsAtUtc, endsAtUtc);
        ValidateUsageLimits(globalUsageLimit, perCustomerUsageLimit);
        ValidateCoupon(requiresCouponCode, couponCode);

        var utcNow = DateTime.UtcNow;
        var promotion = new Promotion
        {
            Name = name.Trim(),
            SystemName = systemName.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = isActive,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            StoreId = storeId,
            Priority = priority,
            CombinationRule = combinationRule,
            CombinationGroup = NormalizeCombinationGroup(combinationGroup),
            GlobalUsageLimit = globalUsageLimit,
            PerCustomerUsageLimit = perCustomerUsageLimit,
            UsageCount = 0,
            RequiresCouponCode = requiresCouponCode,
            CouponCode = NormalizeCouponCode(couponCode),
            IsDeleted = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        promotion.SetConditions(conditions);
        promotion.SetActions(actions);
        return promotion;
    }

    public void Update(
        string name,
        string? description,
        bool isActive,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? storeId,
        int priority,
        PromotionCombinationRule combinationRule,
        string? combinationGroup,
        int? globalUsageLimit,
        int? perCustomerUsageLimit,
        bool requiresCouponCode,
        string? couponCode,
        IEnumerable<PromotionCondition> conditions,
        IEnumerable<PromotionAction> actions)
    {
        EnsureNotDeleted();
        ValidateName(name);
        ValidateDateRange(startsAtUtc, endsAtUtc);
        ValidateUsageLimits(globalUsageLimit, perCustomerUsageLimit);
        ValidateCoupon(requiresCouponCode, couponCode);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        StoreId = storeId;
        Priority = priority;
        CombinationRule = combinationRule;
        CombinationGroup = NormalizeCombinationGroup(combinationGroup);
        GlobalUsageLimit = globalUsageLimit;
        PerCustomerUsageLimit = perCustomerUsageLimit;
        RequiresCouponCode = requiresCouponCode;
        CouponCode = NormalizeCouponCode(couponCode);
        SetConditions(conditions);
        SetActions(actions);
        Touch();
    }

    public void Activate()
    {
        EnsureNotDeleted();
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        IsActive = false;
        Touch();
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

    public bool HasGlobalUsageRemaining() =>
        !GlobalUsageLimit.HasValue || UsageCount < GlobalUsageLimit.Value;

    public bool MatchesCouponCode(string? couponCode)
    {
        if (!RequiresCouponCode)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(couponCode) || string.IsNullOrWhiteSpace(CouponCode))
        {
            return false;
        }

        return string.Equals(NormalizeCouponCode(couponCode), CouponCode, StringComparison.Ordinal);
    }

    public void RecordUsage()
    {
        EnsureNotDeleted();
        if (!HasGlobalUsageRemaining())
        {
            throw new InvalidOperationException("Promotion global usage limit reached.");
        }

        UsageCount++;
        Touch();
    }

    internal void LoadConditions(IEnumerable<PromotionCondition> conditions)
    {
        _conditions.Clear();
        _conditions.AddRange(conditions);
    }

    internal void LoadActions(IEnumerable<PromotionAction> actions)
    {
        _actions.Clear();
        _actions.AddRange(actions);
    }

    private void SetConditions(IEnumerable<PromotionCondition> conditions)
    {
        _conditions.Clear();
        foreach (var condition in conditions)
        {
            _conditions.Add(PromotionCondition.Create(Id > 0 ? Id : 0, condition.ConditionType, condition.ParametersJson));
        }
    }

    private void SetActions(IEnumerable<PromotionAction> actions)
    {
        _actions.Clear();
        foreach (var action in actions)
        {
            _actions.Add(PromotionAction.Create(Id > 0 ? Id : 0, action.ActionType, action.TargetScope, action.ParametersJson));
        }
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Promotion has been deleted.");
        }
    }

    private static string? NormalizeCombinationGroup(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCouponCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
        {
            throw new ArgumentException("Name is required and must be within length limits.", nameof(name));
        }
    }

    private static void ValidateSystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName) || systemName.Length > SystemNameMaxLength)
        {
            throw new ArgumentException("System name is required.", nameof(systemName));
        }
    }

    private static void ValidateDateRange(DateTime? startsAtUtc, DateTime? endsAtUtc)
    {
        if (startsAtUtc.HasValue && endsAtUtc.HasValue && startsAtUtc.Value > endsAtUtc.Value)
        {
            throw new ArgumentException("Start date must be before end date.");
        }
    }

    private static void ValidateUsageLimits(int? global, int? perCustomer)
    {
        if (global.HasValue && global.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(global));
        }

        if (perCustomer.HasValue && perCustomer.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(perCustomer));
        }
    }

    private static void ValidateCoupon(bool requiresCouponCode, string? couponCode)
    {
        if (requiresCouponCode && string.IsNullOrWhiteSpace(couponCode))
        {
            throw new ArgumentException("Coupon code is required when promotion requires a code.");
        }
    }
}
