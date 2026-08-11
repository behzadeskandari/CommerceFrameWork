using Commerce.Customers.Domain.Events;
using Commerce.Framework.Core.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Customers.Domain.Entities;

public sealed class CustomerAddress : Entity
{
    public const int LabelMaxLength = 100;

    private CustomerAddress()
    {
    }

    public int CustomerId { get; private set; }

    public string Label { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public string? StateProvince { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string Address1 { get; private set; } = string.Empty;

    public string? Address2 { get; private set; }

    public string PostalCode { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public bool IsDefaultBilling { get; private set; }

    public bool IsDefaultShipping { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static CustomerAddress Create(
        int customerId,
        string label,
        Address address,
        bool isDefaultBilling = false,
        bool isDefaultShipping = false)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Address label is required.", nameof(label));
        }

        var trimmedLabel = label.Trim();
        if (trimmedLabel.Length > LabelMaxLength)
        {
            throw new ArgumentException($"Address label cannot exceed {LabelMaxLength} characters.", nameof(label));
        }

        var entity = new CustomerAddress
        {
            CustomerId = customerId,
            Label = trimmedLabel,
            FirstName = address.FirstName,
            LastName = address.LastName,
            Country = address.Country,
            StateProvince = address.StateProvince,
            City = address.City,
            Address1 = address.Address1,
            Address2 = address.Address2,
            PostalCode = address.PostalCode,
            PhoneNumber = address.PhoneNumber,
            IsDefaultBilling = isDefaultBilling,
            IsDefaultShipping = isDefaultShipping,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        entity.AddDomainEvent(new CustomerAddressAddedEvent(customerId, entity.Id));
        return entity;
    }

    public void UpdateDetails(
        string label,
        Address address,
        bool isDefaultBilling,
        bool isDefaultShipping)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Address label is required.", nameof(label));
        }

        Label = label.Trim();
        FirstName = address.FirstName;
        LastName = address.LastName;
        Country = address.Country;
        StateProvince = address.StateProvince;
        City = address.City;
        Address1 = address.Address1;
        Address2 = address.Address2;
        PostalCode = address.PostalCode;
        PhoneNumber = address.PhoneNumber;
        IsDefaultBilling = isDefaultBilling;
        IsDefaultShipping = isDefaultShipping;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetDefaultBilling(bool value)
    {
        IsDefaultBilling = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetDefaultShipping(bool value)
    {
        IsDefaultShipping = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Address ToAddress() =>
        Address.Create(
            FirstName,
            LastName,
            Country,
            City,
            Address1,
            PostalCode,
            StateProvince,
            Address2,
            PhoneNumber);
}
