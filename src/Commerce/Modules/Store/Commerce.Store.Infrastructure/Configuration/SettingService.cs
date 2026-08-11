using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Store.Infrastructure.Configuration;

public sealed class StoreSettingDefinitionProvider : ISettingDefinitionProvider
{
    public IReadOnlyList<SettingDefinition> GetDefinitions() =>
    [
        new(
            "Store.DefaultLanguage",
            "Default language code for the store.",
            SettingValueType.String,
            "en",
            "Commerce.Store"),
        new(
            "Catalog.ProductsPerPage",
            "Number of products shown per catalog page.",
            SettingValueType.Integer,
            "12",
            "Commerce.Store")
    ];
}

public sealed class SettingService(
    CommerceDbContext dbContext,
    IEnumerable<ISettingDefinitionProvider> definitionProviders) : ISettingService
{
    public async Task<string?> GetRawAsync(
        string key,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (storeId.HasValue && storeId.Value > 0)
        {
            var storeValue = await dbContext.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == key && x.StoreId == storeId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (storeValue is not null)
            {
                return storeValue.Value;
            }
        }

        var globalValue = await dbContext.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == key && x.StoreId == 0, cancellationToken)
            .ConfigureAwait(false);

        if (globalValue is not null)
        {
            return globalValue.Value;
        }

        return GetDefinition(key)?.DefaultValue;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var raw = await GetRawAsync(key, storeId, cancellationToken).ConfigureAwait(false);
        if (raw is null)
        {
            return default;
        }

        return ConvertValue<T>(raw);
    }

    public async Task SetAsync(
        string key,
        string value,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var normalizedStoreId = storeId ?? 0;
        var definition = GetDefinition(key);
        var dataType = definition?.ValueType.ToString().ToLowerInvariant() ?? "string";

        var setting = await dbContext.Settings
            .FirstOrDefaultAsync(x => x.Name == key && x.StoreId == normalizedStoreId, cancellationToken)
            .ConfigureAwait(false);

        if (setting is null)
        {
            dbContext.Settings.Add(new Setting
            {
                Name = key,
                Value = value,
                StoreId = normalizedStoreId,
                DataType = dataType
            });
        }
        else
        {
            setting.Value = value;
            setting.DataType = dataType;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SettingEntryDto>> ListAsync(
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedStoreId = storeId ?? 0;
        var definitions = definitionProviders.SelectMany(p => p.GetDefinitions()).ToList();
        var stored = await dbContext.Settings
            .AsNoTracking()
            .Where(x => x.StoreId == normalizedStoreId || x.StoreId == 0)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var entries = new List<SettingEntryDto>();
        foreach (var definition in definitions)
        {
            var storeSpecific = stored.FirstOrDefault(x => x.Name == definition.Key && x.StoreId == normalizedStoreId);
            var global = stored.FirstOrDefault(x => x.Name == definition.Key && x.StoreId == 0);
            var value = storeSpecific?.Value ?? global?.Value ?? definition.DefaultValue;

            entries.Add(new SettingEntryDto(
                definition.Key,
                value,
                definition.ValueType,
                definition.Description,
                normalizedStoreId,
                definition.ModuleSystemName));
        }

        return entries;
    }

    private SettingDefinition? GetDefinition(string key) =>
        definitionProviders
            .SelectMany(p => p.GetDefinitions())
            .FirstOrDefault(d => d.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    private static T? ConvertValue<T>(string raw)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType == typeof(string))
        {
            return (T?)(object)raw;
        }

        if (targetType == typeof(bool))
        {
            return (T?)(object)bool.Parse(raw);
        }

        if (targetType == typeof(int))
        {
            return (T?)(object)int.Parse(raw);
        }

        if (targetType == typeof(decimal))
        {
            return (T?)(object)decimal.Parse(raw);
        }

        if (targetType == typeof(DateTime))
        {
            return (T?)(object)DateTime.Parse(raw);
        }

        return (T?)Convert.ChangeType(raw, targetType);
    }
}
