using Commerce.Framework.Contracts.Configuration;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Host.Authorization;
using Commerce.Store.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Store;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(
    ISettingService settingService,
    IStoreContext storeContext) : ControllerBase
{
    [HttpGet]
    [RequirePermission(StorePermissions.SettingsView)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId;
        var settings = await settingService.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true, data = settings });
    }

    [HttpPut]
    [RequirePermission(StorePermissions.SettingsUpdate)]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsApiRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storeId = request.StoreId ?? storeContext.CurrentStoreId;
        foreach (var entry in request.Settings)
        {
            await settingService.SetAsync(entry.Key, entry.Value, storeId, cancellationToken).ConfigureAwait(false);
        }

        return Ok(new { success = true });
    }
}

public sealed record UpdateSettingsApiRequest(
    IReadOnlyList<SettingEntryApiRequest> Settings,
    int? StoreId = null);

public sealed record SettingEntryApiRequest(string Key, string Value);
