using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Payments.Contracts.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Payments;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentForOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await paymentService
            .CreateForOrderAsync(request, idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        return PaymentActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("by-order/{orderId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByOrder(int orderId, CancellationToken cancellationToken)
    {
        var result = await paymentService.GetByOrderIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        return PaymentActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class PaymentActionResults
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

    internal static IActionResult ToActionResult(ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(new { success = true });
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
