using Commerce.Framework.Contracts.Configuration;
using Commerce.Payments.Contracts.Payments;

namespace Commerce.Payments.Application.Payments;

public sealed class PaymentProviderSettingsReader(ISettingService settingService) : IPaymentProviderSettingsReader
{
    public Task<string?> GetAsync(string key, int storeId, CancellationToken cancellationToken = default) =>
        settingService.GetRawAsync(key, storeId, cancellationToken);

    public async Task<bool> GetBoolAsync(
        string key,
        int storeId,
        bool defaultValue = false,
        CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetRawAsync(key, storeId, cancellationToken).ConfigureAwait(false);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
