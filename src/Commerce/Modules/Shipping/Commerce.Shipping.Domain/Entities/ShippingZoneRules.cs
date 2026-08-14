using Commerce.Framework.Core.Entities;

namespace Commerce.Shipping.Domain.Entities;

public sealed class ShippingZoneCountry : Entity
{
    private ShippingZoneCountry()
    {
    }

    public int ShippingZoneId { get; private set; }

    public string CountryCode { get; private set; } = string.Empty;

    public static ShippingZoneCountry Create(int zoneId, string countryCode)
    {
        if (zoneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
        {
            throw new ArgumentException("Country code must be ISO 3166-1 alpha-2.", nameof(countryCode));
        }

        return new ShippingZoneCountry
        {
            ShippingZoneId = zoneId,
            CountryCode = countryCode.Trim().ToUpperInvariant()
        };
    }
}

public sealed class ShippingZoneState : Entity
{
    private ShippingZoneState()
    {
    }

    public int ShippingZoneId { get; private set; }

    public string CountryCode { get; private set; } = string.Empty;

    public string StateProvince { get; private set; } = string.Empty;

    public static ShippingZoneState Create(int zoneId, string countryCode, string stateProvince)
    {
        if (zoneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException("Country code is required.", nameof(countryCode));
        }

        if (string.IsNullOrWhiteSpace(stateProvince))
        {
            throw new ArgumentException("State/province is required.", nameof(stateProvince));
        }

        return new ShippingZoneState
        {
            ShippingZoneId = zoneId,
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            StateProvince = stateProvince.Trim()
        };
    }
}

public sealed class ShippingZonePostalRule : Entity
{
    private ShippingZonePostalRule()
    {
    }

    public int ShippingZoneId { get; private set; }

    public string CountryCode { get; private set; } = string.Empty;

    public Domain.Enums.PostalRuleType RuleType { get; private set; }

    public string PostalFrom { get; private set; } = string.Empty;

    public string? PostalTo { get; private set; }

    public static ShippingZonePostalRule CreateExact(int zoneId, string countryCode, string postalCode) =>
        Create(zoneId, countryCode, Domain.Enums.PostalRuleType.Exact, postalCode, null);

    public static ShippingZonePostalRule CreatePrefix(int zoneId, string countryCode, string prefix) =>
        Create(zoneId, countryCode, Domain.Enums.PostalRuleType.Prefix, prefix, null);

    public static ShippingZonePostalRule CreateRange(int zoneId, string countryCode, string from, string to) =>
        Create(zoneId, countryCode, Domain.Enums.PostalRuleType.Range, from, to);

    private static ShippingZonePostalRule Create(
        int zoneId,
        string countryCode,
        Domain.Enums.PostalRuleType ruleType,
        string postalFrom,
        string? postalTo)
    {
        if (zoneId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zoneId));
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new ArgumentException("Country code is required.", nameof(countryCode));
        }

        if (string.IsNullOrWhiteSpace(postalFrom))
        {
            throw new ArgumentException("Postal code is required.", nameof(postalFrom));
        }

        return new ShippingZonePostalRule
        {
            ShippingZoneId = zoneId,
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            RuleType = ruleType,
            PostalFrom = postalFrom.Trim().ToUpperInvariant(),
            PostalTo = string.IsNullOrWhiteSpace(postalTo) ? null : postalTo.Trim().ToUpperInvariant()
        };
    }

    public bool Matches(string postalCode)
    {
        var normalized = postalCode.Trim().ToUpperInvariant();
        return RuleType switch
        {
            Domain.Enums.PostalRuleType.Exact => normalized == PostalFrom,
            Domain.Enums.PostalRuleType.Prefix => normalized.StartsWith(PostalFrom, StringComparison.Ordinal),
            Domain.Enums.PostalRuleType.Range => PostalTo is not null &&
                string.Compare(normalized, PostalFrom, StringComparison.Ordinal) >= 0 &&
                string.Compare(normalized, PostalTo, StringComparison.Ordinal) <= 0,
            _ => false
        };
    }
}
