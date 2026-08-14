using Commerce.Host.Authorization;
using Commerce.Scheduling.Contracts.Admin;
using Commerce.Scheduling.Infrastructure.Security;
using Commerce.Framework.Scheduling;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Scheduling;

internal static class SchedulingActionResults
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Commerce.Framework.Core.Results.Result<T> result, Func<T, object?> map) =>
        result.IsSuccess
            ? controller.Ok(new { data = map(result.Value!) })
            : MapError(controller, result.Error!);

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
[Route("api/admin/scheduling/jobs")]
public sealed class AdminBackgroundJobsController(IBackgroundJobAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(SchedulingPermissions.View)]
    public async Task<IActionResult> List(
        [FromQuery] BackgroundJobStatus? status,
        [FromQuery] string? jobType,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListJobsAsync(status, jobType, take, cancellationToken).ConfigureAwait(false);
        return SchedulingActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(SchedulingPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetJobAsync(id, cancellationToken).ConfigureAwait(false);
        return SchedulingActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("{id:int}/cancel")]
    [RequirePermission(SchedulingPermissions.Manage)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await service.CancelJobAsync(id, cancellationToken).ConfigureAwait(false);
        return SchedulingActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/retry")]
    [RequirePermission(SchedulingPermissions.Manage)]
    public async Task<IActionResult> Retry(int id, CancellationToken cancellationToken)
    {
        var result = await service.RetryJobAsync(id, cancellationToken).ConfigureAwait(false);
        return SchedulingActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/scheduling/recurring")]
public sealed class AdminRecurringJobsController(IBackgroundJobAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(SchedulingPermissions.View)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await service.ListRecurringAsync(cancellationToken).ConfigureAwait(false);
        return SchedulingActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("{scheduleKey}/enable")]
    [RequirePermission(SchedulingPermissions.Manage)]
    public async Task<IActionResult> Enable(string scheduleKey, CancellationToken cancellationToken)
    {
        var result = await service.EnableRecurringAsync(scheduleKey, cancellationToken).ConfigureAwait(false);
        return SchedulingActionResults.ToActionResult(this, result);
    }

    [HttpPost("{scheduleKey}/disable")]
    [RequirePermission(SchedulingPermissions.Manage)]
    public async Task<IActionResult> Disable(string scheduleKey, CancellationToken cancellationToken)
    {
        var result = await service.DisableRecurringAsync(scheduleKey, cancellationToken).ConfigureAwait(false);
        return SchedulingActionResults.ToActionResult(this, result);
    }
}
