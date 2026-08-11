namespace Commerce.Customers.Application.Customers;

public sealed record RegisterCustomerRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber = null);

public sealed record CreateCustomerRequest(
    string IdentityUserId,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber = null);

public sealed record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber = null);

public sealed record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false);

public sealed record AddCustomerAddressRequest(
    string Label,
    string FirstName,
    string LastName,
    string Country,
    string City,
    string Address1,
    string PostalCode,
    string? StateProvince = null,
    string? Address2 = null,
    string? PhoneNumber = null,
    bool IsDefaultBilling = false,
    bool IsDefaultShipping = false);

public sealed record UpdateCustomerAddressRequest(
    string Label,
    string FirstName,
    string LastName,
    string Country,
    string City,
    string Address1,
    string PostalCode,
    string? StateProvince = null,
    string? Address2 = null,
    string? PhoneNumber = null,
    bool IsDefaultBilling = false,
    bool IsDefaultShipping = false);
