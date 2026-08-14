using Commerce.Host.Authorization;
using Commerce.Notifications.Contracts.Admin;
using Commerce.Notifications.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Notifications;

internal static class NotificationActionResults
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Commerce.Framework.Core.Results.Result<T> result, Func<T, object?> map, int successStatus = StatusCodes.Status200OK) =>
        result.IsSuccess
            ? (successStatus == StatusCodes.Status200OK
                ? controller.Ok(new { data = map(result.Value!) })
                : controller.StatusCode(successStatus, new { data = map(result.Value!) }))
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
[Route("api/admin/notifications/templates")]
public sealed class AdminNotificationTemplatesController(INotificationTemplateAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NotificationPermissions.View)]
    public async Task<IActionResult> List([FromQuery] int? storeId, [FromQuery] Commerce.Notifications.Domain.Enums.NotificationEventType? eventType, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(storeId, eventType, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(NotificationPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost]
    [RequirePermission(NotificationPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(NotificationPermissions.Manage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("{id:int}/activate")]
    [RequirePermission(NotificationPermissions.Manage)]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var result = await service.ActivateAsync(id, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/deactivate")]
    [RequirePermission(NotificationPermissions.Manage)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeactivateAsync(id, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(NotificationPermissions.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/notifications/logs")]
public sealed class AdminNotificationLogsController(INotificationHistoryAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NotificationPermissions.View)]
    public async Task<IActionResult> List(
        [FromQuery] int? storeId,
        [FromQuery] Commerce.Notifications.Domain.Enums.NotificationDeliveryStatus? status,
        [FromQuery] int? customerId,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(storeId, status, customerId, take, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("{id:int}/retry")]
    [RequirePermission(NotificationPermissions.Manage)]
    public async Task<IActionResult> Retry(int id, CancellationToken cancellationToken)
    {
        var result = await service.RetryAsync(id, cancellationToken).ConfigureAwait(false);
        return NotificationActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/notifications/in-app")]
public sealed class InAppNotificationsController(
    Commerce.Notifications.Contracts.Storefront.IInAppNotificationStorefrontService service,
    Commerce.Customers.Contracts.Customers.ICurrentCustomerContext customerContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var customerId = customerContext.CustomerId;
        if (!customerId.HasValue)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var items = await service.ListUnreadAsync(customerId.Value, storeId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = items });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var customerId = customerContext.CustomerId;
        if (!customerId.HasValue)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        await service.MarkReadAsync(customerId.Value, id, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = new { } });
    }
}
