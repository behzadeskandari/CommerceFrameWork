using Commerce.Host.Authorization;
using Commerce.Pricing.Contracts.Discounts;
using Commerce.Pricing.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Pricing;

[ApiController]
[Route("api/admin/discounts")]
public sealed class AdminDiscountsController(IDiscountAdminService discountAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PricingPermissions.DiscountsView)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await discountAdminService.ListAsync(cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PricingPermissions.DiscountsView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await discountAdminService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(PricingPermissions.DiscountsCreate)]
    public async Task<IActionResult> Create([FromBody] CreateDiscountRequest request, CancellationToken cancellationToken)
    {
        var result = await discountAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PricingPermissions.DiscountsUpdate)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDiscountRequest request, CancellationToken cancellationToken)
    {
        var result = await discountAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PricingPermissions.DiscountsDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await discountAdminService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/activate")]
    [RequirePermission(PricingPermissions.DiscountsManage)]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var result = await discountAdminService.ActivateAsync(id, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/deactivate")]
    [RequirePermission(PricingPermissions.DiscountsManage)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await discountAdminService.DeactivateAsync(id, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/coupons")]
public sealed class AdminCouponsController(ICouponAdminService couponAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PricingPermissions.CouponsView)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await couponAdminService.ListAsync(cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PricingPermissions.CouponsView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await couponAdminService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(PricingPermissions.CouponsManage)]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest request, CancellationToken cancellationToken)
    {
        var result = await couponAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PricingPermissions.CouponsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponRequest request, CancellationToken cancellationToken)
    {
        var result = await couponAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PricingPermissions.CouponsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await couponAdminService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return PricingActionResults.ToActionResult(this, result);
    }
}

internal static class PricingActionResults
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
