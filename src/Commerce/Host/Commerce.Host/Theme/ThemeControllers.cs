using Commerce.Framework.Contracts.Tenancy;
using Commerce.Host.Authorization;
using Commerce.Themes.Contracts;
using Commerce.Themes.Contracts.Admin;
using Commerce.Themes.Contracts.Storefront;
using Commerce.Themes.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Theme;

[ApiController]
[Route("api/themes")]
public sealed class ThemeRuntimeController(IThemeStorefrontService service, IStoreContext storeContext) : ControllerBase
{
    [HttpGet("runtime")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRuntime(CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId ?? 1;
        var runtime = await service.GetRuntimeAsync(storeId, storeContext.IsRtl, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = runtime });
    }
}

[ApiController]
[Route("api/admin/themes")]
public sealed class AdminThemesController(IThemeAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ThemePermissions.View)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var themes = await service.ListThemesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { data = themes });
    }

    [HttpGet("{systemName}")]
    [RequirePermission(ThemePermissions.View)]
    public async Task<IActionResult> Get(string systemName, CancellationToken cancellationToken)
    {
        var theme = await service.GetThemeAsync(systemName, cancellationToken).ConfigureAwait(false);
        return theme is null ? NotFound(new { success = false, error = "Theme not found." }) : Ok(new { data = theme });
    }

    [HttpGet("store/{storeId:int}")]
    [RequirePermission(ThemePermissions.View)]
    public async Task<IActionResult> GetStoreAssignment(int storeId, CancellationToken cancellationToken)
    {
        var assignment = await service.GetStoreAssignmentAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = assignment });
    }

    [HttpPut("store/{storeId:int}")]
    [RequirePermission(ThemePermissions.Manage)]
    public async Task<IActionResult> SaveStoreAssignment(int storeId, [FromBody] UpdateStoreThemeAssignmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await service.SaveStoreAssignmentAsync(storeId, request, cancellationToken).ConfigureAwait(false);
            return Ok(new { data = assignment });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
