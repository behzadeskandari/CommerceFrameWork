using Commerce.Framework.Contracts.Configuration;
using Commerce.Tax.Application;
using Commerce.Tax.Contracts.Admin;

namespace Commerce.Tax.Application.Admin;

public sealed class TaxSettingsAdminService(
    TaxSettings taxSettings,
    ISettingService settingService) : ITaxSettingsAdminService
{
    public async Task<TaxSettingsDto> GetAsync(int? storeId, CancellationToken cancellationToken = default) =>
        new(
            await taxSettings.IsEnabledAsync(storeId, cancellationToken).ConfigureAwait(false),
            await taxSettings.GetPricesIncludeTaxAsync(storeId, cancellationToken).ConfigureAwait(false),
            await taxSettings.GetDefaultCategoryIdAsync(storeId, cancellationToken).ConfigureAwait(false),
            await taxSettings.GetShippingTaxableByDefaultAsync(storeId, cancellationToken).ConfigureAwait(false));

    public async Task<TaxSettingsDto> UpdateAsync(
        int? storeId,
        UpdateTaxSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        await settingService.SetAsync(TaxSettingKeys.Enabled, request.Enabled.ToString(), storeId, cancellationToken).ConfigureAwait(false);
        await settingService.SetAsync(TaxSettingKeys.PricesIncludeTax, request.PricesIncludeTax.ToString(), storeId, cancellationToken).ConfigureAwait(false);
        await settingService.SetAsync(
            TaxSettingKeys.DefaultCategoryId,
            request.DefaultCategoryId?.ToString() ?? string.Empty,
            storeId,
            cancellationToken).ConfigureAwait(false);
        await settingService.SetAsync(TaxSettingKeys.ShippingTaxableByDefault, request.ShippingTaxableByDefault.ToString(), storeId, cancellationToken).ConfigureAwait(false);
        return await GetAsync(storeId, cancellationToken).ConfigureAwait(false);
    }
}
