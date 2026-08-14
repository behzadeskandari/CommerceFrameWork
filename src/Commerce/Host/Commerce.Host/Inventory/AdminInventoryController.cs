using Commerce.Host.Authorization;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Inventory;

[ApiController]
[Route("api/admin/inventory")]
public sealed class AdminInventoryController(
    IInventoryAdminService inventoryAdminService,
    IInventoryTransferService transferService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(InventoryPermissions.View)]
    public async Task<IActionResult> List([FromQuery] InventoryListQuery query, CancellationToken cancellationToken)
    {
        var result = await inventoryAdminService.ListAsync(query, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(InventoryPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await inventoryAdminService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(InventoryPermissions.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inventoryAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPost("{id:int}/adjust")]
    [RequirePermission(InventoryPermissions.Adjust)]
    public async Task<IActionResult> Adjust(
        int id,
        [FromBody] AdjustInventoryStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inventoryAdminService
            .AdjustAsync(id, request, "admin", cancellationToken)
            .ConfigureAwait(false);

        return InventoryActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/movements")]
    [RequirePermission(InventoryPermissions.View)]
    public async Task<IActionResult> Movements(int id, CancellationToken cancellationToken)
    {
        var result = await inventoryAdminService.ListMovementsAsync(id, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/reservations")]
    [RequirePermission(InventoryPermissions.View)]
    public async Task<IActionResult> Reservations(int id, CancellationToken cancellationToken)
    {
        var result = await inventoryAdminService.ListReservationsAsync(id, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/low-stock-threshold")]
    [RequirePermission(InventoryPermissions.Manage)]
    public async Task<IActionResult> SetLowStockThreshold(
        int id,
        [FromBody] SetLowStockThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inventoryAdminService.SetLowStockThresholdAsync(id, request, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("transfer")]
    [RequirePermission(InventoryPermissions.Adjust)]
    public async Task<IActionResult> Transfer([FromBody] TransferInventoryStockRequest request, CancellationToken cancellationToken)
    {
        var result = await transferService.TransferAsync(request, "admin", cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => new
        {
            sourceMovement = value.SourceMovement,
            destinationMovement = value.DestinationMovement
        });
    }

    [HttpPost("{id:int}/receive-incoming")]
    [RequirePermission(InventoryPermissions.Adjust)]
    public async Task<IActionResult> ReceiveIncoming(
        int id,
        [FromBody] ReceiveIncomingStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await transferService.ReceiveIncomingAsync(id, request, "admin", cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class InventoryActionResults
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
        Commerce.Framework.Core.Results.Result result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, new { success = true });
        }

        return MapFailure(controller, result.Error!);
    }

    private static IActionResult MapFailure(
        ControllerBase controller,
        Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}
