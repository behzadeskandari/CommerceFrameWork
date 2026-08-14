namespace Commerce.Payments.Contracts.Payments;

public interface IPaymentProviderSettingsReader
{
    Task<string?> GetAsync(string key, int storeId, CancellationToken cancellationToken = default);

    Task<bool> GetBoolAsync(
        string key,
        int storeId,
        bool defaultValue = false,
        CancellationToken cancellationToken = default);
}
