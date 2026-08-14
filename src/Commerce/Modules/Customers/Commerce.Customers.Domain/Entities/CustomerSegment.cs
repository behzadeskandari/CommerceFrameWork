using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class CustomerSegment : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    private readonly List<CustomerSegmentRule> _rules = [];

    private CustomerSegment()
    {
    }

    public int StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<CustomerSegmentRule> Rules => _rules;

    public static CustomerSegment Create(
        int storeId,
        string name,
        string? description,
        IEnumerable<CustomerSegmentRule> rules)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Segment name is required.", nameof(name));
        }

        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
        {
            throw new InvalidOperationException("Segment must include at least one rule.");
        }

        var utcNow = DateTime.UtcNow;
        var segment = new CustomerSegment
        {
            StoreId = storeId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        segment._rules.AddRange(ruleList);
        return segment;
    }

    public void Update(string name, string? description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Segment name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceRules(IEnumerable<CustomerSegmentRule> rules)
    {
        var ruleList = rules.ToList();
        if (ruleList.Count == 0)
        {
            throw new InvalidOperationException("Segment must include at least one rule.");
        }

        _rules.Clear();
        _rules.AddRange(ruleList);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class CustomerSegmentRule : Entity
{
    private CustomerSegmentRule()
    {
    }

    public int CustomerSegmentId { get; private set; }

    public CustomerSegmentRuleType RuleType { get; private set; }

    public int? CustomerGroupId { get; private set; }

    public int? MinOrderCount { get; private set; }

    public decimal? MinLifetimeSpend { get; private set; }

    public static CustomerSegmentRule ForCustomerGroup(int customerGroupId)
    {
        if (customerGroupId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerGroupId));
        }

        return new CustomerSegmentRule
        {
            RuleType = CustomerSegmentRuleType.CustomerGroup,
            CustomerGroupId = customerGroupId
        };
    }

    public static CustomerSegmentRule ForMinOrderCount(int minOrderCount)
    {
        if (minOrderCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minOrderCount));
        }

        return new CustomerSegmentRule
        {
            RuleType = CustomerSegmentRuleType.MinOrderCount,
            MinOrderCount = minOrderCount
        };
    }

    public static CustomerSegmentRule ForMinLifetimeSpend(decimal minLifetimeSpend)
    {
        if (minLifetimeSpend <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minLifetimeSpend));
        }

        return new CustomerSegmentRule
        {
            RuleType = CustomerSegmentRuleType.MinLifetimeSpend,
            MinLifetimeSpend = minLifetimeSpend
        };
    }
}

public sealed class CustomerSegmentMembership : Entity
{
    private CustomerSegmentMembership()
    {
    }

    public int CustomerSegmentId { get; private set; }

    public int CustomerId { get; private set; }

    public int StoreId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    public static CustomerSegmentMembership Create(int segmentId, int customerId, int storeId, DateTime utcNow) =>
        new()
        {
            CustomerSegmentId = segmentId,
            CustomerId = customerId,
            StoreId = storeId,
            AssignedAtUtc = utcNow
        };
}
