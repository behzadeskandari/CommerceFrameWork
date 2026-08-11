using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Orders;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await orderService
            .CreateFromCheckoutAsync(request, idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        return OrderActionResults.ToActionResult(this, result, value => value, successStatusCode: StatusCodes.Status201Created);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await orderService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(
        [FromQuery] OrderListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await orderService.ListCustomerOrdersAsync(query, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("by-number/{orderNumber}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByNumber(
        string orderNumber,
        [FromQuery] string? accessToken,
        CancellationToken cancellationToken)
    {
        var result = await orderService
            .GetByOrderNumberAsync(orderNumber, accessToken, cancellationToken)
            .ConfigureAwait(false);

        return OrderActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orderService.CancelAsync(id, request, cancellationToken).ConfigureAwait(false);
        return OrderActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class OrderActionResults
{
    internal static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Result<T> result,
        Func<T, object?> dataSelector,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(controller, result.Error!);
    }

    private static IActionResult MapFailure(ControllerBase controller, Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}
