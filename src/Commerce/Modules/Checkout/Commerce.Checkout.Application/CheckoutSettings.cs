using Commerce.Framework.Contracts.Configuration;

namespace Commerce.Checkout.Application;

public static class CheckoutSettingKeys
{
    public const string ExpirationMinutes = "Checkout.ExpirationMinutes";
}

public sealed class CheckoutSettings(ISettingService settingService)
{
    public async Task<int> GetExpirationMinutesAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int>(CheckoutSettingKeys.ExpirationMinutes, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 60;
    }
}
