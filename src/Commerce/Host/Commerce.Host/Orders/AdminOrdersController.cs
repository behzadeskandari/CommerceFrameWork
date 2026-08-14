using Commerce.Host.Authorization;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Orders;

[ApiController]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(
    IAdminOrderService adminOrderService,
    IOrderLifecycleService orderLifecycleService,
    IReturnAdminService returnAdminService) : ControllerBase
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

    [HttpPost("{id:int}/confirm")]
    [RequirePermission(OrdersPermissions.Manage)]
    public async Task<IActionResult> Confirm(
        int id,
        [FromBody] ConfirmOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderLifecycleService.ConfirmAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/processing")]
    [RequirePermission(OrdersPermissions.Manage)]
    public async Task<IActionResult> MarkProcessing(int id, CancellationToken cancellationToken)
    {
        var result = await orderLifecycleService.MarkProcessingAsync(id, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/complete")]
    [RequirePermission(OrdersPermissions.Manage)]
    public async Task<IActionResult> Complete(
        int id,
        [FromBody] CompleteOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderLifecycleService.CompleteAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/partial-cancel")]
    [RequirePermission(OrdersPermissions.Cancel)]
    public async Task<IActionResult> PartialCancel(
        int id,
        [FromBody] PartialCancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderLifecycleService.CancelPartialAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/refund")]
    [RequirePermission(OrdersPermissions.Refund)]
    public async Task<IActionResult> Refund(
        int id,
        [FromBody] RefundOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await orderLifecycleService.RefundAsync(id, request, idempotencyKey, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/returns")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> ListReturns(int id, CancellationToken cancellationToken)
    {
        var result = await returnAdminService.ListByOrderAsync(id, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/returns")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> CreateReturn(
        int id,
        [FromBody] CreateReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnAdminService.CreateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }
}

[ApiController]
[Route("api/admin/returns")]
public sealed class AdminReturnsController(IReturnAdminService returnAdminService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await returnAdminService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/approve")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> Approve(
        int id,
        [FromBody] ApproveReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnAdminService.ApproveAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/reject")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] RejectReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnAdminService.RejectAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/shipment")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> SetReturnShipment(
        int id,
        [FromBody] ReturnShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnAdminService.SetReturnShipmentAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/received")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> MarkReceived(int id, CancellationToken cancellationToken)
    {
        var result = await returnAdminService.MarkReceivedAsync(id, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/complete")]
    [RequirePermission(OrdersPermissions.Returns)]
    public async Task<IActionResult> Complete(
        int id,
        [FromBody] CompleteReturnRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await returnAdminService.CompleteAsync(id, request, idempotencyKey, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }
}
