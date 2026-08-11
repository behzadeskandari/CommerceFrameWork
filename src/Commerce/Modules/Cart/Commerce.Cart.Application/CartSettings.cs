using Commerce.Framework.Contracts.Configuration;

namespace Commerce.Cart.Application;

public static class CartSettingKeys
{
    public const string MaxItemQuantity = "Cart.MaxItemQuantity";
    public const string MaxDistinctItems = "Cart.MaxDistinctItems";
    public const string GuestExpirationHours = "Cart.GuestExpirationHours";
    public const string CustomerExpirationDays = "Cart.CustomerExpirationDays";
}

public sealed class CartSettings(ISettingService settingService)
{
    public async Task<int> GetMaxItemQuantityAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int>(CartSettingKeys.MaxItemQuantity, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 999;
    }

    public async Task<int> GetMaxDistinctItemsAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int>(CartSettingKeys.MaxDistinctItems, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 100;
    }

    public async Task<int> GetGuestExpirationHoursAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int>(CartSettingKeys.GuestExpirationHours, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 720;
    }

    public async Task<int> GetCustomerExpirationDaysAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int>(CartSettingKeys.CustomerExpirationDays, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 30;
    }
}
