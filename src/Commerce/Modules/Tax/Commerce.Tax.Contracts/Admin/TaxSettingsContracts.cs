namespace Commerce.Tax.Contracts.Admin;

public sealed record TaxSettingsDto(
    bool Enabled,
    bool PricesIncludeTax,
    int? DefaultCategoryId,
    bool ShippingTaxableByDefault);

public sealed record UpdateTaxSettingsRequest(
    bool Enabled,
    bool PricesIncludeTax,
    int? DefaultCategoryId,
    bool ShippingTaxableByDefault);

public interface ITaxSettingsAdminService
{
    Task<TaxSettingsDto> GetAsync(int? storeId, CancellationToken cancellationToken = default);
    Task<TaxSettingsDto> UpdateAsync(int? storeId, UpdateTaxSettingsRequest request, CancellationToken cancellationToken = default);
}
