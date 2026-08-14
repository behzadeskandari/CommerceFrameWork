using Commerce.Framework.PluginContracts.Admin;
using Commerce.Framework.PluginContracts.Lifecycle;
using Commerce.Framework.Plugins.Security;
using Commerce.Host.Authorization;
using Commerce.Host.Payments;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Plugins;

[ApiController]
[Route("api/admin/plugins")]
public sealed class AdminPluginsController(IPluginAdminService pluginAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PluginPermissions.View)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.ListAsync(cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{systemName}")]
    [RequirePermission(PluginPermissions.View)]
    public async Task<IActionResult> Get(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.GetAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{systemName}/settings")]
    [RequirePermission(PluginPermissions.Configure)]
    public async Task<IActionResult> GetSettings(
        string systemName,
        [FromQuery] int? storeId,
        CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.GetSettingsAsync(systemName, storeId, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("{systemName}/settings")]
    [RequirePermission(PluginPermissions.Configure)]
    public async Task<IActionResult> SaveSettings(
        string systemName,
        [FromBody] Dictionary<string, string> values,
        [FromQuery] int? storeId,
        CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.SaveSettingsAsync(systemName, values, storeId, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }

    [HttpGet("{systemName}/permissions")]
    [RequirePermission(PluginPermissions.View)]
    public async Task<IActionResult> GetPermissions(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.GetPermissionsAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{systemName}/stores")]
    [RequirePermission(PluginPermissions.Configure)]
    public async Task<IActionResult> GetStoreConfigurations(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.GetStoreConfigurationsAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("{systemName}/stores")]
    [RequirePermission(PluginPermissions.Configure)]
    public async Task<IActionResult> SaveStoreConfiguration(
        string systemName,
        [FromBody] PluginStoreConfigurationDto configuration,
        CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.SaveStoreConfigurationAsync(systemName, configuration, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }

    [HttpGet("{systemName}/migrations")]
    [RequirePermission(PluginPermissions.View)]
    public async Task<IActionResult> GetMigrationStatus(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.GetMigrationStatusAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{systemName}/ui")]
    [RequirePermission(PluginPermissions.View)]
    public async Task<IActionResult> GetUiMetadata(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.GetUiMetadataAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{systemName}/localization/{culture}")]
    [RequirePermission(PluginPermissions.View)]
    public async Task<IActionResult> GetLocalization(
        string systemName,
        string culture,
        CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.GetLocalizationAsync(systemName, culture, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{systemName}/install")]
    [RequirePermission(PluginPermissions.Install)]
    public async Task<IActionResult> Install(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.InstallAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }

    [HttpPost("{systemName}/enable")]
    [RequirePermission(PluginPermissions.Manage)]
    public async Task<IActionResult> Enable(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.EnableAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }

    [HttpPost("{systemName}/disable")]
    [RequirePermission(PluginPermissions.Manage)]
    public async Task<IActionResult> Disable(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.DisableAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }

    [HttpPost("{systemName}/uninstall")]
    [RequirePermission(PluginPermissions.Manage)]
    public async Task<IActionResult> Uninstall(
        string systemName,
        [FromQuery] PluginUninstallMode uninstallMode = PluginUninstallMode.KeepData,
        CancellationToken cancellationToken = default)
    {
        var result = await pluginAdminService.UninstallAsync(systemName, uninstallMode, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }

    [HttpPost("{systemName}/reload")]
    [RequirePermission(PluginPermissions.Manage)]
    public async Task<IActionResult> Reload(string systemName, CancellationToken cancellationToken)
    {
        var result = await pluginAdminService.ReloadAsync(systemName, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }

    [HttpPost("install-package")]
    [RequirePermission(PluginPermissions.Install)]
    public async Task<IActionResult> InstallPackage(IFormFile package, CancellationToken cancellationToken)
    {
        if (package is null || package.Length == 0)
        {
            return BadRequest(new { success = false, error = "Plugin package file is required." });
        }

        await using var stream = package.OpenReadStream();
        var result = await pluginAdminService.InstallFromPackageAsync(stream, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result);
    }
}
