using Commerce.Checkout.Contracts.Checkout;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Checkout;

[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController(ICheckoutService checkoutService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Start(CancellationToken cancellationToken)
    {
        var result = await checkoutService.StartAsync(cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("{id:int}/guest-contact")]
    [AllowAnonymous]
    public async Task<IActionResult> SetGuestContact(
        int id,
        [FromBody] SetGuestContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.SetGuestContactAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("{id:int}/billing-address")]
    [AllowAnonymous]
    public async Task<IActionResult> SetBillingAddress(
        int id,
        [FromBody] SetBillingAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.SetBillingAddressAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("{id:int}/shipping-address")]
    [AllowAnonymous]
    public async Task<IActionResult> SetShippingAddress(
        int id,
        [FromBody] SetShippingAddressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.SetShippingAddressAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/shipping-options")]
    [AllowAnonymous]
    public async Task<IActionResult> GetShippingOptions(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value.ShippingOptions);
    }

    [HttpPut("{id:int}/shipping-method")]
    [AllowAnonymous]
    public async Task<IActionResult> SelectShippingMethod(
        int id,
        [FromBody] SelectShippingMethodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.SelectShippingMethodAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/payment-methods")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPaymentMethods(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value.PaymentMethods);
    }

    [HttpPut("{id:int}/payment-method")]
    [AllowAnonymous]
    public async Task<IActionResult> SelectPaymentMethod(
        int id,
        [FromBody] SelectPaymentMethodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.SelectPaymentMethodAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.RefreshAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.ValidateAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/gift-cards")]
    [AllowAnonymous]
    public async Task<IActionResult> ApplyGiftCard(
        int id,
        [FromBody] ApplyGiftCardRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.ApplyGiftCardAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}/gift-cards")]
    [AllowAnonymous]
    public async Task<IActionResult> RemoveGiftCard(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.RemoveGiftCardAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/store-credit")]
    [Authorize]
    public async Task<IActionResult> ApplyStoreCredit(
        int id,
        [FromBody] ApplyStoreCreditRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.ApplyStoreCreditAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}/store-credit")]
    [Authorize]
    public async Task<IActionResult> RemoveStoreCredit(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.RemoveStoreCreditAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/referral-code")]
    [AllowAnonymous]
    public async Task<IActionResult> ApplyReferralCode(
        int id,
        [FromBody] ApplyReferralCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await checkoutService.ApplyReferralCodeAsync(id, request, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}/referral-code")]
    [AllowAnonymous]
    public async Task<IActionResult> RemoveReferralCode(int id, CancellationToken cancellationToken)
    {
        var result = await checkoutService.RemoveReferralCodeAsync(id, cancellationToken).ConfigureAwait(false);
        return CheckoutActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class CheckoutActionResults
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
