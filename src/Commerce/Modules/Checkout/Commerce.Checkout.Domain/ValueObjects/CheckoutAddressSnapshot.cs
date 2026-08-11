namespace Commerce.Checkout.Domain.ValueObjects;

public sealed class CheckoutAddressSnapshot
{
    public const int NameMaxLength = 200;
    public const int CountryMaxLength = 100;
    public const int CityMaxLength = 100;
    public const int AddressMaxLength = 500;
    public const int PostalCodeMaxLength = 32;
    public const int StateProvinceMaxLength = 100;
    public const int PhoneMaxLength = 32;

    private CheckoutAddressSnapshot()
    {
    }

    public int? SourceCustomerAddressId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public string? StateProvince { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string Address1 { get; private set; } = string.Empty;

    public string? Address2 { get; private set; }

    public string PostalCode { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public static CheckoutAddressSnapshot FromAddress(
        Commerce.Framework.Domain.ValueObjects.Address address,
        int? sourceCustomerAddressId = null) =>
        Create(
            address.FirstName,
            address.LastName,
            address.Country,
            address.City,
            address.Address1,
            address.PostalCode,
            address.StateProvince,
            address.Address2,
            address.PhoneNumber,
            sourceCustomerAddressId);

    public static CheckoutAddressSnapshot Create(
        string firstName,
        string lastName,
        string country,
        string city,
        string address1,
        string postalCode,
        string? stateProvince = null,
        string? address2 = null,
        string? phoneNumber = null,
        int? sourceCustomerAddressId = null)
    {
        return new CheckoutAddressSnapshot
        {
            SourceCustomerAddressId = sourceCustomerAddressId,
            FirstName = Require(firstName, nameof(firstName), NameMaxLength),
            LastName = Require(lastName, nameof(lastName), NameMaxLength),
            Country = Require(country, nameof(country), CountryMaxLength),
            StateProvince = NormalizeOptional(stateProvince, StateProvinceMaxLength),
            City = Require(city, nameof(city), CityMaxLength),
            Address1 = Require(address1, nameof(address1), AddressMaxLength),
            Address2 = NormalizeOptional(address2, AddressMaxLength),
            PostalCode = Require(postalCode, nameof(postalCode), PostalCodeMaxLength),
            PhoneNumber = NormalizeOptional(phoneNumber, PhoneMaxLength)
        };
    }

    public Commerce.Framework.Domain.ValueObjects.Address ToAddress() =>
        Commerce.Framework.Domain.ValueObjects.Address.Create(
            FirstName,
            LastName,
            Country,
            City,
            Address1,
            PostalCode,
            StateProvince,
            Address2,
            PhoneNumber);

    private static string Require(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{paramName} cannot exceed {maxLength} characters.", paramName);
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
