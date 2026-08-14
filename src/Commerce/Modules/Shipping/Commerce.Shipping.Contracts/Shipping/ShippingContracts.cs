namespace Commerce.Shipping.Contracts.Shipping;

public sealed record ShippingCalculationContext(
    int StoreId,
    string CurrencyCode,
    string CountryCode,
    string? StateProvince,
    string PostalCode,
    decimal OrderSubtotal,
    decimal TotalWeightGrams,
    int TotalQuantity,
    IReadOnlyList<ShippingCalculationLineContext> Lines);

public sealed record ShippingCalculationLineContext(
    int OfferId,
    int ProductId,
    int? VariantId,
    int Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    string ProductType,
    decimal WeightGrams);

public sealed record CalculatedShippingOption(
    string Id,
    int ShippingMethodId,
    string Name,
    string? Description,
    string ProviderSystemName,
    decimal Cost,
    string CurrencyCode,
    string? EstimatedDelivery,
    int DisplayOrder,
    bool RequiresAddress = true);

public interface IShippingCalculationService
{
    Task<IReadOnlyList<CalculatedShippingOption>> CalculateOptionsAsync(
        ShippingCalculationContext context,
        CancellationToken cancellationToken = default);

    Task<CalculatedShippingOption?> ValidateSelectionAsync(
        ShippingCalculationContext context,
        string methodId,
        string providerSystemName,
        CancellationToken cancellationToken = default);
}

public interface IShippingProvider
{
    string ProviderSystemName { get; }

    Task<IReadOnlyList<CalculatedShippingOption>> GetOptionsAsync(
        ShippingCalculationContext context,
        CancellationToken cancellationToken = default);
}

public static class ShippingProviderNames
{
    public const string FlatRate = "Shipping.FlatRate";
    public const string Pickup = "Shipping.Pickup";
}

public sealed record ShippingProviderDescriptorDto(
    string SystemName,
    string DisplayName,
    bool IsPlugin);
