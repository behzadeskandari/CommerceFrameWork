namespace Commerce.Customers.Contracts.Customers;

public sealed record CustomerSummaryDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool Active,
    bool Deleted,
    DateTime CreatedAtUtc);

public sealed record CustomerDetailDto(
    int Id,
    string IdentityUserId,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    bool Active,
    bool Deleted,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<CustomerAddressDto> Addresses);

public sealed record CustomerAddressDto(
    int Id,
    int CustomerId,
    string Label,
    string FirstName,
    string LastName,
    string Country,
    string? StateProvince,
    string City,
    string Address1,
    string? Address2,
    string PostalCode,
    string? PhoneNumber,
    bool IsDefaultBilling,
    bool IsDefaultShipping,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
