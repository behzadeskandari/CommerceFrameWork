using Commerce.Customers.Contracts.Affiliates;
using Commerce.Customers.Infrastructure.Security;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Customers;

[ApiController]
[Route("api/admin/affiliates")]
public sealed class AdminAffiliatesController(IAffiliateAdminService affiliateAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(CustomersPermissions.AffiliatesView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await affiliateAdminService.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return AffiliateActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(CustomersPermissions.AffiliatesView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await affiliateAdminService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return AffiliateActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(CustomersPermissions.AffiliatesManage)]
    public async Task<IActionResult> Create([FromBody] CreateAffiliateRequest request, CancellationToken cancellationToken)
    {
        var result = await affiliateAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return AffiliateActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(CustomersPermissions.AffiliatesManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAffiliateRequest request, CancellationToken cancellationToken)
    {
        var result = await affiliateAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return AffiliateActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(CustomersPermissions.AffiliatesManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await affiliateAdminService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return AffiliateActionResults.ToActionResult(this, result, _ => new { success = true });
    }

    [HttpGet("{id:int}/commissions")]
    [RequirePermission(CustomersPermissions.AffiliatesView)]
    public async Task<IActionResult> ListCommissions(int id, CancellationToken cancellationToken)
    {
        var result = await affiliateAdminService.ListCommissionsAsync(id, cancellationToken).ConfigureAwait(false);
        return AffiliateActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/referrals")]
    [RequirePermission(CustomersPermissions.AffiliatesView)]
    public async Task<IActionResult> ListReferrals(int id, CancellationToken cancellationToken)
    {
        var result = await affiliateAdminService.ListReferralsAsync(id, cancellationToken).ConfigureAwait(false);
        return AffiliateActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class AffiliateActionResults
{
    internal static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Commerce.Framework.Core.Results.Result<T> result,
        Func<T, object?> dataSelector,
        int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatus, new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(controller, result.Error!);
    }

    internal static IActionResult ToActionResult(
        ControllerBase controller,
        Commerce.Framework.Core.Results.Result result,
        Func<object?, object?> dataSelector)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(new { success = true, data = dataSelector(null) });
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
