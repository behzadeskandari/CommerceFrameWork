using Commerce.Customers.Domain.Events;
using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class Customer : AggregateRoot
{
    public const int EmailMaxLength = 500;
    public const int NameMaxLength = 200;
    public const int PhoneMaxLength = 50;

    private Customer()
    {
    }

    public string IdentityUserId { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public bool Active { get; private set; }

    public bool Deleted { get; private set; }

    public bool IsTaxExempt { get; private set; }

    public string? TaxRegistrationNumber { get; private set; }

    public int? CustomerGroupId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Customer Create(
        string identityUserId,
        string email,
        string firstName,
        string lastName,
        string? phoneNumber = null,
        bool active = true)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new ArgumentException("Identity user id is required.", nameof(identityUserId));
        }

        var normalizedEmail = NormalizeEmail(email);
        var customer = new Customer
        {
            IdentityUserId = identityUserId.Trim(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            FirstName = NormalizeName(firstName, nameof(firstName)),
            LastName = NormalizeName(lastName, nameof(lastName)),
            PhoneNumber = NormalizePhone(phoneNumber),
            Active = active,
            Deleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        customer.RaiseDomainEvent(new CustomerRegisteredEvent(customer.Id, customer.Email));
        return customer;
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        EnsureNotDeleted();

        FirstName = NormalizeName(firstName, nameof(firstName));
        LastName = NormalizeName(lastName, nameof(lastName));
        PhoneNumber = NormalizePhone(phoneNumber);
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CustomerUpdatedEvent(Id, Email));
    }

    public void UpdateTaxProfile(bool isTaxExempt, string? taxRegistrationNumber)
    {
        EnsureNotDeleted();
        IsTaxExempt = isTaxExempt;
        TaxRegistrationNumber = string.IsNullOrWhiteSpace(taxRegistrationNumber)
            ? null
            : taxRegistrationNumber.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignCustomerGroup(int? customerGroupId)
    {
        EnsureNotDeleted();
        CustomerGroupId = customerGroupId is > 0 ? customerGroupId : null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        Active = false;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerDeactivatedEvent(Id, Email));
    }

    public void MarkDeleted()
    {
        if (Deleted)
        {
            return;
        }

        Deleted = true;
        Active = false;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CustomerDeactivatedEvent(Id, Email));
    }

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    internal static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaxLength)
        {
            throw new ArgumentException($"Email cannot exceed {EmailMaxLength} characters.", nameof(email));
        }

        return trimmed.ToUpperInvariant();
    }

    private static string NormalizeName(string name, string paramName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"{paramName} cannot exceed {NameMaxLength} characters.", paramName);
        }

        return trimmed;
    }

    private static string? NormalizePhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var trimmed = phoneNumber.Trim();
        if (trimmed.Length > PhoneMaxLength)
        {
            throw new ArgumentException($"Phone number cannot exceed {PhoneMaxLength} characters.", nameof(phoneNumber));
        }

        return trimmed;
    }

    private void EnsureNotDeleted()
    {
        if (Deleted)
        {
            throw new InvalidOperationException("Deleted customers cannot be modified.");
        }
    }
}
