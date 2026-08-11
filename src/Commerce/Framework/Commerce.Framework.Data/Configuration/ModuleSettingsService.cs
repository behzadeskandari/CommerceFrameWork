using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Data.Db;
using Commerce.Framework.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Framework.Data.Configuration;

public sealed class ModuleSettingsService(CommerceDbContext dbContext) : IModuleSettings
{
    public async Task<string?> GetAsync(
        string moduleSystemName,
        string key,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleSystemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var settingName = BuildSettingName(moduleSystemName, key);
        var effectiveStoreId = storeId ?? 0;

        var setting = await dbContext.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Name == settingName && x.StoreId == effectiveStoreId,
                cancellationToken)
            .ConfigureAwait(false);

        return setting?.Value;
    }

    public async Task SetAsync(
        string moduleSystemName,
        string key,
        string value,
        int? storeId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleSystemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var settingName = BuildSettingName(moduleSystemName, key);
        var effectiveStoreId = storeId ?? 0;

        var setting = await dbContext.Settings
            .FirstOrDefaultAsync(
                x => x.Name == settingName && x.StoreId == effectiveStoreId,
                cancellationToken)
            .ConfigureAwait(false);

        if (setting is null)
        {
            dbContext.Settings.Add(new Setting
            {
                Name = settingName,
                Value = value,
                StoreId = effectiveStoreId
            });
        }
        else
        {
            setting.Value = value;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildSettingName(string moduleSystemName, string key) =>
        $"Module.{moduleSystemName}.{key}";
}
