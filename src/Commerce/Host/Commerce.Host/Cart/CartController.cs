using Commerce.Cart.Contracts.Carts;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Cart;

[ApiController]
[Route("api/cart")]
public sealed class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await cartService.GetCartAsync(cancellationToken).ConfigureAwait(false);
        return CartActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("items")]
    [AllowAnonymous]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        var result = await cartService.AddItemAsync(request, cancellationToken).ConfigureAwait(false);
        return CartActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("items/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateItem(
        int id,
        [FromBody] UpdateCartItemQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await cartService.UpdateItemQuantityAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CartActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("items/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> RemoveItem(int id, CancellationToken cancellationToken)
    {
        var result = await cartService.RemoveItemAsync(id, cancellationToken).ConfigureAwait(false);
        return CartActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete]
    [AllowAnonymous]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var result = await cartService.ClearCartAsync(cancellationToken).ConfigureAwait(false);
        return CartActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("merge")]
    [Authorize]
    public async Task<IActionResult> MergeGuestCart(CancellationToken cancellationToken)
    {
        var result = await cartService.MergeGuestCartAsync(cancellationToken).ConfigureAwait(false);
        return CartActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class CartActionResults
{
    internal static IActionResult ToActionResult<T>(ControllerBase controller, Result<T> result, Func<T, object?> dataSelector)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(new { success = true, data = dataSelector(result.Value!) });
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
