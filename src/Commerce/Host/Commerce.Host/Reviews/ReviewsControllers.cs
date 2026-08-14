using Commerce.Customers.Contracts.Customers;
using Commerce.Host.Authorization;
using Commerce.Reviews.Contracts.Admin;
using Commerce.Reviews.Contracts.Storefront;
using Commerce.Reviews.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Reviews;

internal static class ReviewsActionResults
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Commerce.Framework.Core.Results.Result<T> result, Func<T, object?> map, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatus == StatusCodes.Status200OK
                ? controller.Ok(new { data = map(result.Value!) })
                : controller.StatusCode(successStatus, new { data = map(result.Value!) });
        }

        return MapError(controller, result.Error!);
    }

    public static IActionResult ToActionResult(ControllerBase controller, Commerce.Framework.Core.Results.Result result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(new { data = new { } });
        }

        return MapError(controller, result.Error!);
    }

    private static IActionResult MapError(ControllerBase controller, Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Validation => controller.BadRequest(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Forbidden => controller.Forbid(),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}

[ApiController]
[Route("api/reviews/products/{productId:int}")]
public sealed class ProductReviewsController(IReviewStorefrontService reviewService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await reviewService.ListApprovedAsync(productId, page, pageSize, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(int productId, CancellationToken cancellationToken = default)
    {
        var result = await reviewService.GetRatingSummaryAsync(productId, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Submit(
        int productId,
        [FromBody] SubmitProductReviewRequest request,
        [FromServices] ICurrentCustomerContext currentCustomerContext,
        CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var result = await reviewService.SubmitAsync(customerId, productId, request, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }
}

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(
    IReviewStorefrontService reviewService,
    ICurrentCustomerContext currentCustomerContext) : ControllerBase
{
    [HttpGet("me/products/{productId:int}")]
    [Authorize]
    public async Task<IActionResult> GetOwn(int productId, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var result = await reviewService.GetOwnReviewAsync(customerId, productId, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPut("{reviewId:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int reviewId, [FromBody] UpdateProductReviewRequest request, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var result = await reviewService.UpdateOwnAsync(customerId, reviewId, request, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }
}

[ApiController]
[Route("api/wishlist")]
[Authorize]
public sealed class WishlistController(
    IWishlistStorefrontService wishlistService,
    ICurrentCustomerContext currentCustomerContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var result = await wishlistService.GetAsync(customerId, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddWishlistItemRequest request, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var result = await wishlistService.AddItemAsync(customerId, request, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x, StatusCodes.Status201Created);
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var result = await wishlistService.RemoveItemAsync(customerId, productId, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/reviews")]
public sealed class AdminReviewsController(IReviewAdminService reviewService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ReviewPermissions.View)]
    public async Task<IActionResult> List(
        [FromQuery] int? storeId,
        [FromQuery] int? productId,
        [FromQuery] Commerce.Reviews.Domain.Enums.ReviewModerationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await reviewService.ListAsync(storeId, productId, status, page, pageSize, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(ReviewPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await reviewService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPost("{id:int}/approve")]
    [RequirePermission(ReviewPermissions.Manage)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await reviewService.ApproveAsync(id, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result);
    }

    [HttpPost("{id:int}/reject")]
    [RequirePermission(ReviewPermissions.Manage)]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        var result = await reviewService.RejectAsync(id, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(ReviewPermissions.Manage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await reviewService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/admin/wishlists")]
public sealed class AdminWishlistsController(IWishlistAdminService wishlistService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(ReviewPermissions.View)]
    public async Task<IActionResult> List(
        [FromQuery] int? storeId,
        [FromQuery] int? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await wishlistService.ListAsync(storeId, customerId, page, pageSize, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(ReviewPermissions.View)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await wishlistService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return ReviewsActionResults.ToActionResult(this, result, x => x);
    }
}
