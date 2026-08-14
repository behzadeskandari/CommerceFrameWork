using Commerce.Framework.Contracts.Configuration;

namespace Commerce.Shipping.Application;

public static class ShippingSettingKeys
{
    public const string Enabled = "Shipping.Enabled";
    public const string DefaultEstimatedDeliveryDays = "Shipping.DefaultEstimatedDeliveryDays";
    public const string AllowFreeShipping = "Shipping.AllowFreeShipping";
    public const string RequireShippingAddress = "Shipping.RequireShippingAddress";
}

public sealed class ShippingSettings(ISettingService settingService)
{
    public async Task<bool> IsEnabledAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<bool?>(ShippingSettingKeys.Enabled, storeId, cancellationToken).ConfigureAwait(false);
        return value ?? true;
    }

    public async Task<int> GetDefaultEstimatedDeliveryDaysAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int?>(ShippingSettingKeys.DefaultEstimatedDeliveryDays, storeId, cancellationToken).ConfigureAwait(false);
        return value is > 0 ? value.Value : 5;
    }
}
