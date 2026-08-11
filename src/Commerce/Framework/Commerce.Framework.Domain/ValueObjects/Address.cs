namespace Commerce.Framework.Domain.ValueObjects;

public sealed record Address
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Country { get; }
    public string? StateProvince { get; }
    public string City { get; }
    public string Address1 { get; }
    public string? Address2 { get; }
    public string PostalCode { get; }
    public string? PhoneNumber { get; }

    private Address(
        string firstName,
        string lastName,
        string country,
        string? stateProvince,
        string city,
        string address1,
        string? address2,
        string postalCode,
        string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        Country = country;
        StateProvince = stateProvince;
        City = city;
        Address1 = address1;
        Address2 = address2;
        PostalCode = postalCode;
        PhoneNumber = phoneNumber;
    }

    public static Address Create(
        string firstName,
        string lastName,
        string country,
        string city,
        string address1,
        string postalCode,
        string? stateProvince = null,
        string? address2 = null,
        string? phoneNumber = null)
    {
        var normalizedFirstName = RequireValue(firstName, nameof(firstName));
        var normalizedLastName = RequireValue(lastName, nameof(lastName));
        var normalizedCountry = RequireValue(country, nameof(country));
        var normalizedCity = RequireValue(city, nameof(city));
        var normalizedAddress1 = RequireValue(address1, nameof(address1));
        var normalizedPostalCode = RequireValue(postalCode, nameof(postalCode));

        return new Address(
            normalizedFirstName,
            normalizedLastName,
            normalizedCountry,
            NormalizeOptional(stateProvince),
            normalizedCity,
            normalizedAddress1,
            NormalizeOptional(address2),
            normalizedPostalCode,
            NormalizeOptional(phoneNumber));
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private static string RequireValue(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
