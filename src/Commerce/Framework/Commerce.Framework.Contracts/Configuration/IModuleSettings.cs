namespace Commerce.Framework.Contracts.Configuration;

public interface IModuleSettings
{
    Task<string?> GetAsync(
        string moduleSystemName,
        string key,
        int? storeId = null,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string moduleSystemName,
        string key,
        string value,
        int? storeId = null,
        CancellationToken cancellationToken = default);
}
