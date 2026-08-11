namespace Commerce.Framework.Contracts.Configuration;

public enum SettingValueType
{
    String = 0,
    Boolean = 1,
    Integer = 2,
    Decimal = 3,
    DateTime = 4
}

public sealed record SettingDefinition(
    string Key,
    string Description,
    SettingValueType ValueType,
    string DefaultValue,
    string ModuleSystemName);

public interface ISettingDefinitionProvider
{
    IReadOnlyList<SettingDefinition> GetDefinitions();
}

public interface ISettingService
{
    Task<string?> GetRawAsync(string key, int? storeId = null, CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(string key, int? storeId = null, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, int? storeId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SettingEntryDto>> ListAsync(int? storeId = null, CancellationToken cancellationToken = default);
}

public sealed record SettingEntryDto(
    string Key,
    string Value,
    SettingValueType ValueType,
    string Description,
    int StoreId,
    string ModuleSystemName);
