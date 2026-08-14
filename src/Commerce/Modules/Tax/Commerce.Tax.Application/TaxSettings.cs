using Commerce.Framework.Contracts.Configuration;

namespace Commerce.Tax.Application;

public static class TaxSettingKeys
{
    public const string Enabled = "Tax.Enabled";
    public const string PricesIncludeTax = "Tax.PricesIncludeTax";
    public const string DefaultCategoryId = "Tax.DefaultCategoryId";
    public const string ShippingTaxableByDefault = "Tax.ShippingTaxableByDefault";
}

public sealed class TaxSettings(ISettingService settingService)
{
    public async Task<bool> IsEnabledAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<bool?>(TaxSettingKeys.Enabled, storeId, cancellationToken).ConfigureAwait(false);
        return value ?? true;
    }

    public async Task<bool> GetPricesIncludeTaxAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<bool?>(TaxSettingKeys.PricesIncludeTax, storeId, cancellationToken).ConfigureAwait(false);
        return value ?? false;
    }

    public async Task<int?> GetDefaultCategoryIdAsync(int? storeId, CancellationToken cancellationToken = default) =>
        await settingService.GetAsync<int?>(TaxSettingKeys.DefaultCategoryId, storeId, cancellationToken).ConfigureAwait(false);

    public async Task<bool> GetShippingTaxableByDefaultAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<bool?>(TaxSettingKeys.ShippingTaxableByDefault, storeId, cancellationToken).ConfigureAwait(false);
        return value ?? true;
    }
}
