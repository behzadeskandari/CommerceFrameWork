using Commerce.Host.Authorization;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Inventory.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Inventory;

[ApiController]
[Route("api/admin/inventory/warehouses")]
public sealed class AdminWarehousesController(IWarehouseAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(InventoryPermissions.View)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await service.ListWarehousesAsync(storeId, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(InventoryPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetWarehouseAsync(id, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost]
    [RequirePermission(InventoryPermissions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateWarehouseAsync(request, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(InventoryPermissions.Manage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateWarehouseAsync(id, request, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("{id:int}/activate")]
    [RequirePermission(InventoryPermissions.Manage)]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var result = await service.ActivateWarehouseAsync(id, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/deactivate")]
    [RequirePermission(InventoryPermissions.Manage)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeactivateWarehouseAsync(id, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result);
    }

    [HttpPost("{warehouseId:int}/locations")]
    [RequirePermission(InventoryPermissions.Manage)]
    public async Task<IActionResult> CreateLocation(
        int warehouseId,
        [FromBody] CreateStockLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateStockLocationAsync(warehouseId, request, cancellationToken).ConfigureAwait(false);
        return InventoryActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }
}
