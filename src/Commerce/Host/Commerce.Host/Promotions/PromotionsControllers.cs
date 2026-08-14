using Commerce.Host.Authorization;
using Commerce.Promotions.Contracts.Admin;
using Commerce.Promotions.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Promotions;

internal static class PromotionActionResults
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Commerce.Framework.Core.Results.Result<T> result, Func<T, object?> map, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatus == StatusCodes.Status200OK
                ? controller.Ok(new { data = map(result.Value!) })
                : controller.StatusCode(successStatus, new { data = map(result.Value!) });
        }

        return MapError(controller, result.Error!);
    }

    public static IActionResult ToActionResult(ControllerBase controller, Commerce.Framework.Core.Results.Result result) =>
        result.IsSuccess ? controller.Ok(new { data = new { } }) : MapError(controller, result.Error!);

    private static IActionResult MapError(ControllerBase controller, Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Validation => controller.BadRequest(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}

[ApiController]
[Route("api/admin/promotions")]
public sealed class AdminPromotionsController(IPromotionAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PromotionPermissions.View)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return PromotionActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PromotionPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return PromotionActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost]
    [RequirePermission(PromotionPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return PromotionActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PromotionPermissions.Manage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return PromotionActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("{id:int}/activate")]
    [RequirePermission(PromotionPermissions.Manage)]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var result = await service.ActivateAsync(id, cancellationToken).ConfigureAwait(false);
        return PromotionActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/deactivate")]
    [RequirePermission(PromotionPermissions.Manage)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeactivateAsync(id, cancellationToken).ConfigureAwait(false);
        return PromotionActionResults.ToActionResult(this, result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PromotionPermissions.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return PromotionActionResults.ToActionResult(this, result);
    }
}
