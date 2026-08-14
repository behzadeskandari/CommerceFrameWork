using Commerce.Cms.Contracts.Admin;
using Commerce.Cms.Contracts.Storefront;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Commerce.Cms.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Cms;

internal static class CmsActionResults
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Result<T> result, Func<T, object?> map, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatus == StatusCodes.Status200OK
                ? controller.Ok(new { data = map(result.Value!) })
                : controller.StatusCode(successStatus, new { data = map(result.Value!) });
        }

        return MapError(controller, result.Error!);
    }

    public static IActionResult ToActionResult(ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(new { data = new { } });
        }

        return MapError(controller, result.Error!);
    }

    private static IActionResult MapError(ControllerBase controller, Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Validation => controller.BadRequest(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Forbidden => controller.Forbid(),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}

[ApiController]
[Route("api/admin/cms/pages")]
public sealed class AdminContentPagesController(IContentPageAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(CmsPermissions.PagesView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(CmsPermissions.PagesView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost]
    [RequirePermission(CmsPermissions.PagesManage)]
    public async Task<IActionResult> Create([FromBody] CreateContentPageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(CmsPermissions.PagesManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContentPageRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(CmsPermissions.PagesManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/publish")]
    [RequirePermission(CmsPermissions.PagesManage)]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        var result = await service.PublishAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/unpublish")]
    [RequirePermission(CmsPermissions.PagesManage)]
    public async Task<IActionResult> Unpublish(int id, CancellationToken cancellationToken)
    {
        var result = await service.UnpublishAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/cms/topics")]
public sealed class AdminTopicsController(ITopicAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(CmsPermissions.TopicsView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(CmsPermissions.TopicsView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost]
    [RequirePermission(CmsPermissions.TopicsManage)]
    public async Task<IActionResult> Create([FromBody] CreateTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(CmsPermissions.TopicsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(CmsPermissions.TopicsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/cms/menus")]
public sealed class AdminMenusController(IMenuAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(CmsPermissions.MenusView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(CmsPermissions.MenusView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost]
    [RequirePermission(CmsPermissions.MenusManage)]
    public async Task<IActionResult> Create([FromBody] CreateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(CmsPermissions.MenusManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(CmsPermissions.MenusManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/cms/widgets")]
public sealed class AdminWidgetsController(IWidgetAdminService service) : ControllerBase
{
    [HttpGet("zones")]
    [RequirePermission(CmsPermissions.WidgetsView)]
    public async Task<IActionResult> ListZones(CancellationToken cancellationToken)
    {
        var result = await service.ListZonesAsync(cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("instances")]
    [RequirePermission(CmsPermissions.WidgetsView)]
    public async Task<IActionResult> ListInstances([FromQuery] int? storeId, [FromQuery] string? zoneSystemName, CancellationToken cancellationToken)
    {
        var result = await service.ListInstancesAsync(storeId, zoneSystemName, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("instances")]
    [RequirePermission(CmsPermissions.WidgetsManage)]
    public async Task<IActionResult> CreateInstance([FromBody] CreateWidgetInstanceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateInstanceAsync(request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpPut("instances/{id:int}")]
    [RequirePermission(CmsPermissions.WidgetsManage)]
    public async Task<IActionResult> UpdateInstance(int id, [FromBody] UpdateWidgetInstanceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateInstanceAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpDelete("instances/{id:int}")]
    [RequirePermission(CmsPermissions.WidgetsManage)]
    public async Task<IActionResult> DeleteInstance(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteInstanceAsync(id, cancellationToken).ConfigureAwait(false);
        return CmsActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/cms")]
public sealed class CmsStorefrontController(ICmsStorefrontService service) : ControllerBase
{
    [HttpGet("pages/by-slug/{slug}")]
    public async Task<IActionResult> GetPageBySlug(string slug, CancellationToken cancellationToken)
    {
        var page = await service.GetPageBySlugAsync(slug, cancellationToken).ConfigureAwait(false);
        return page is null ? NotFound() : Ok(new { data = page });
    }

    [HttpGet("menus/{systemName}")]
    public async Task<IActionResult> GetMenu(string systemName, CancellationToken cancellationToken)
    {
        var menu = await service.GetMenuAsync(systemName, cancellationToken).ConfigureAwait(false);
        return menu is null ? NotFound() : Ok(new { data = menu });
    }

    [HttpGet("widgets/{zoneSystemName}")]
    public async Task<IActionResult> GetWidgets(string zoneSystemName, CancellationToken cancellationToken)
    {
        var widgets = await service.GetWidgetsAsync(zoneSystemName, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = widgets });
    }
}
