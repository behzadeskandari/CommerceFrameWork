using Commerce.Host.Authorization;
using Commerce.Integration.Contracts.Webhooks;
using Commerce.Integration.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Integration;

[ApiController]
[Route("api/admin/webhooks")]
public sealed class AdminWebhooksController(IWebhookAdminService webhookAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(IntegrationPermissions.WebhooksView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await webhookAdminService.ListSubscriptionsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(IntegrationPermissions.WebhooksView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await webhookAdminService.GetSubscriptionAsync(id, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(IntegrationPermissions.WebhooksManage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await webhookAdminService.CreateSubscriptionAsync(request, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(
            this,
            result,
            value => new { subscription = value.Subscription, secret = value.Secret },
            StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(IntegrationPermissions.WebhooksManage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateWebhookSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await webhookAdminService.UpdateSubscriptionAsync(id, request, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/rotate-secret")]
    [RequirePermission(IntegrationPermissions.WebhooksManage)]
    public async Task<IActionResult> RotateSecret(int id, CancellationToken cancellationToken)
    {
        var result = await webhookAdminService.RotateSecretAsync(id, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => new { secret = value });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(IntegrationPermissions.WebhooksManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await webhookAdminService.DeleteSubscriptionAsync(id, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result);
    }

    [HttpGet("{id:int}/deliveries")]
    [RequirePermission(IntegrationPermissions.WebhooksView)]
    public async Task<IActionResult> ListDeliveries(int id, CancellationToken cancellationToken)
    {
        var result = await webhookAdminService.ListDeliveriesAsync(id, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class IntegrationActionResults
{
    internal static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Commerce.Framework.Core.Results.Result<T> result,
        Func<T, object?> dataSelector,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(controller, result.Error!);
    }

    internal static IActionResult ToActionResult(
        ControllerBase controller,
        Commerce.Framework.Core.Results.Result result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(new { success = true });
        }

        return MapFailure(controller, result.Error!);
    }

    private static IActionResult MapFailure(ControllerBase controller, Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}
