namespace Commerce.Catalog.Contracts.Products;

public static class DigitalProductTypes
{
    public static bool IsDigital(string? productType) =>
        !string.IsNullOrWhiteSpace(productType) && IsDigitalProductTypeName(productType);

    public static bool IsDigitalProductTypeName(string productType) =>
        string.Equals(productType, "Digital", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(productType, "Downloadable", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(productType, "Virtual", StringComparison.OrdinalIgnoreCase);
}
