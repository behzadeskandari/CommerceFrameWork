using Commerce.Host.Authorization;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Orders;

[ApiController]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(IAdminOrderService adminOrderService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(OrdersPermissions.View)]
    public async Task<IActionResult> List([FromQuery] OrderListQuery query, CancellationToken cancellationToken)
    {
        var result = await adminOrderService.ListAsync(query, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(OrdersPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await adminOrderService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/cancel")]
    [RequirePermission(OrdersPermissions.Cancel)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOrderService.CancelAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }
}
