using Commerce.Host.Authorization;
using Commerce.Shipping.Contracts.Admin;
using Commerce.Shipping.Contracts.Shipments;
using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Shipping;

[ApiController]
[Route("api/admin/shipping/methods")]
public sealed class AdminShippingMethodsController(IShippingAdminService shippingAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.ListMethodsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.GetMethodAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateShippingMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.CreateMethodAsync(request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShippingMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.UpdateMethodAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.DeleteMethodAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/shipping/zones")]
public sealed class AdminShippingZonesController(IShippingAdminService shippingAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.ListZonesAsync(storeId, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.GetZoneAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateShippingZoneRequest request, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.CreateZoneAsync(request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShippingZoneRequest request, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.UpdateZoneAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.DeleteZoneAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/shipping/rates")]
public sealed class AdminShippingRatesController(IShippingAdminService shippingAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> List(
        [FromQuery] int? storeId,
        [FromQuery] int? methodId,
        CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.ListRatesAsync(storeId, methodId, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.GetRateAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateShippingRateRequest request, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.CreateRateAsync(request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShippingRateRequest request, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.UpdateRateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.DeleteRateAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/shipping/providers")]
public sealed class AdminShippingProvidersController(IShippingProviderRegistry providerRegistry) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ShippingPermissions.View)]
    public IActionResult List() =>
        Ok(new { success = true, data = providerRegistry.ListProviders() });
}

[ApiController]
[Route("api/admin/shipping/settings")]
public sealed class AdminShippingSettingsController(IShippingAdminService shippingAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ShippingPermissions.Configure)]
    public async Task<IActionResult> Get([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut]
    [RequirePermission(ShippingPermissions.Configure)]
    public async Task<IActionResult> Update(
        [FromQuery] int? storeId,
        [FromBody] UpdateShippingSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await shippingAdminService.UpdateSettingsAsync(storeId, request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }
}

[ApiController]
[Route("api/admin/shipping/shipments")]
public sealed class AdminShipmentsController(IShipmentAdminService shipmentAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> ListByOrder([FromQuery] int orderId, CancellationToken cancellationToken)
    {
        var result = await shipmentAdminService.ListByOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(ShippingPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await shipmentAdminService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        var result = await shipmentAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}/tracking")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> UpdateTracking(
        int id,
        [FromBody] UpdateShipmentTrackingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await shipmentAdminService.UpdateTrackingAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/ship")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> MarkShipped(int id, CancellationToken cancellationToken)
    {
        var result = await shipmentAdminService.MarkShippedAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/deliver")]
    [RequirePermission(ShippingPermissions.Manage)]
    public async Task<IActionResult> MarkDelivered(int id, CancellationToken cancellationToken)
    {
        var result = await shipmentAdminService.MarkDeliveredAsync(id, cancellationToken).ConfigureAwait(false);
        return ShippingActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class ShippingActionResults
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
