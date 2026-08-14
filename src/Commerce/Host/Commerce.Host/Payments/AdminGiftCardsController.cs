using Commerce.Host.Authorization;
using Commerce.Payments.Contracts.GiftCards;
using Commerce.Payments.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Payments;

[ApiController]
[Route("api/admin/gift-cards")]
public sealed class AdminGiftCardsController(IGiftCardAdminService giftCardAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PaymentsPermissions.GiftCardsView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await giftCardAdminService.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return GiftCardActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PaymentsPermissions.GiftCardsView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await giftCardAdminService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return GiftCardActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(PaymentsPermissions.GiftCardsManage)]
    public async Task<IActionResult> Create([FromBody] CreateGiftCardRequest request, CancellationToken cancellationToken)
    {
        var result = await giftCardAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return GiftCardActionResults.ToActionResult(this, result, value => value, StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PaymentsPermissions.GiftCardsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGiftCardRequest request, CancellationToken cancellationToken)
    {
        var result = await giftCardAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return GiftCardActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PaymentsPermissions.GiftCardsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await giftCardAdminService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return GiftCardActionResults.ToActionResult(this, result, _ => new { success = true });
    }

    [HttpGet("{id:int}/transactions")]
    [RequirePermission(PaymentsPermissions.GiftCardsView)]
    public async Task<IActionResult> ListTransactions(int id, CancellationToken cancellationToken)
    {
        var result = await giftCardAdminService.ListTransactionsAsync(id, cancellationToken).ConfigureAwait(false);
        return GiftCardActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class GiftCardActionResults
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
