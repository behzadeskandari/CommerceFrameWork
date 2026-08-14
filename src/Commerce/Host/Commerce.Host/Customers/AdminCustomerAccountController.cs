using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Contracts.Customers;
using Commerce.Customers.Domain.Enums;
using Commerce.Customers.Infrastructure.Security;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Customers;

[ApiController]
[Route("api/admin/customers")]
public sealed class AdminCustomerAccountController(
    ICustomerAccountAdminService accountAdminService,
    ICustomerPreferenceService preferenceService,
    ILoyaltyService loyaltyService,
    IStoreCreditService storeCreditService,
    ICustomerActivityService activityService) : ControllerBase
{
    [HttpPut("{id:int}/group")]
    [RequirePermission(CustomersPermissions.Manage)]
    public async Task<IActionResult> AssignGroup(
        int id,
        [FromBody] AssignCustomerGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountAdminService.AssignCustomerGroupAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPut("{id:int}/tax-profile")]
    [RequirePermission(CustomersPermissions.Manage)]
    public async Task<IActionResult> UpdateTaxProfile(
        int id,
        [FromBody] UpdateCustomerTaxProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountAdminService.UpdateTaxProfileAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/deactivate")]
    [RequirePermission(CustomersPermissions.Manage)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var result = await accountAdminService.DeactivateAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}/purchase-history")]
    [RequirePermission(CustomersPermissions.View)]
    public async Task<IActionResult> PurchaseHistory(int id, CancellationToken cancellationToken)
    {
        var result = await accountAdminService.GetPurchaseHistoryAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("{id:int}/preferences")]
    [RequirePermission(CustomersPermissions.View)]
    public async Task<IActionResult> ListPreferences(int id, [FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await preferenceService.ListAsync(id, storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("{id:int}/loyalty")]
    [RequirePermission(CustomersPermissions.LoyaltyView)]
    public async Task<IActionResult> GetLoyalty(int id, [FromQuery] int storeId, CancellationToken cancellationToken)
    {
        var result = await loyaltyService.GetAccountAsync(id, storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("{id:int}/loyalty/transactions")]
    [RequirePermission(CustomersPermissions.LoyaltyView)]
    public async Task<IActionResult> ListLoyaltyTransactions(int id, [FromQuery] int storeId, CancellationToken cancellationToken)
    {
        var result = await loyaltyService.ListTransactionsAsync(id, storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPost("{id:int}/loyalty/earn")]
    [RequirePermission(CustomersPermissions.LoyaltyManage)]
    public async Task<IActionResult> EarnLoyalty(
        int id,
        [FromQuery] int storeId,
        [FromBody] AdminEarnLoyaltyRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await loyaltyService.EarnAsync(
            id,
            storeId,
            request.Points,
            idempotencyKey ?? Guid.NewGuid().ToString("N"),
            CustomerAccountReferenceType.Manual,
            null,
            request.Reason,
            request.ExpiresAtUtc,
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, x => x);
    }

    [HttpGet("{id:int}/store-credit")]
    [RequirePermission(CustomersPermissions.LoyaltyView)]
    public async Task<IActionResult> GetStoreCredit(
        int id,
        [FromQuery] int storeId,
        [FromQuery] string currencyCode,
        CancellationToken cancellationToken)
    {
        var result = await storeCreditService.GetAccountAsync(id, storeId, currencyCode, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPost("{id:int}/store-credit/credit")]
    [RequirePermission(CustomersPermissions.StoreCreditManage)]
    public async Task<IActionResult> CreditStoreCredit(
        int id,
        [FromQuery] int storeId,
        [FromQuery] string currencyCode,
        [FromBody] CreditStoreCreditRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await storeCreditService.CreditAsync(
            id,
            storeId,
            currencyCode,
            request,
            idempotencyKey ?? Guid.NewGuid().ToString("N"),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, x => x);
    }

    [HttpGet("{id:int}/activity")]
    [RequirePermission(CustomersPermissions.View)]
    public async Task<IActionResult> ListActivity(int id, [FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await activityService.ListAsync(id, storeId, 50, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    private IActionResult ToActionResult(Result result) =>
        result.IsSuccess ? Ok(new { success = true }) : MapFailure(result.Error!);

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> selector) =>
        result.IsSuccess ? Ok(new { success = true, data = selector(result.Value!) }) : MapFailure(result.Error!);

    private IActionResult MapFailure(Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Unauthorized => Unauthorized(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}

public sealed record AdminEarnLoyaltyRequest(int Points, string? Reason, DateTime? ExpiresAtUtc);

[ApiController]
[Route("api/admin/customer-segments")]
public sealed class AdminCustomerSegmentsController(ICustomerSegmentAdminService segmentService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(CustomersPermissions.SegmentsView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await segmentService.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(CustomersPermissions.SegmentsView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await segmentService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPost]
    [RequirePermission(CustomersPermissions.SegmentsManage)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerSegmentRequest request, CancellationToken cancellationToken)
    {
        var result = await segmentService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(CustomersPermissions.SegmentsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerSegmentRequest request, CancellationToken cancellationToken)
    {
        var result = await segmentService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(CustomersPermissions.SegmentsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await segmentService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("{customerId:int}/evaluate")]
    [RequirePermission(CustomersPermissions.SegmentsManage)]
    public async Task<IActionResult> Evaluate(
        int customerId,
        [FromBody] EvaluateCustomerSegmentsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await segmentService.EvaluateCustomerSegmentsAsync(
            customerId,
            request.StoreId,
            request.CustomerGroupId,
            request.OrderCount,
            request.LifetimeSpend,
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, x => x);
    }

    private IActionResult ToActionResult(Result result) =>
        result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { success = false, error = result.Error!.Message });

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> selector) =>
        result.IsSuccess ? Ok(new { success = true, data = selector(result.Value!) }) : BadRequest(new { success = false, error = result.Error!.Message });
}

public sealed record EvaluateCustomerSegmentsRequest(
    int StoreId,
    int? CustomerGroupId,
    int OrderCount,
    decimal LifetimeSpend);

[ApiController]
[Route("api/admin/loyalty-rewards")]
public sealed class AdminLoyaltyRewardsController(ILoyaltyRewardAdminService rewardService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(CustomersPermissions.LoyaltyView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await rewardService.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPost]
    [RequirePermission(CustomersPermissions.LoyaltyManage)]
    public async Task<IActionResult> Create([FromBody] CreateLoyaltyRewardRequest request, CancellationToken cancellationToken)
    {
        var result = await rewardService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(CustomersPermissions.LoyaltyManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLoyaltyRewardRequest request, CancellationToken cancellationToken)
    {
        var result = await rewardService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(CustomersPermissions.LoyaltyManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await rewardService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result) =>
        result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { success = false, error = result.Error!.Message });

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> selector) =>
        result.IsSuccess ? Ok(new { success = true, data = selector(result.Value!) }) : BadRequest(new { success = false, error = result.Error!.Message });
}

[ApiController]
[Route("api/customers/me/account")]
public sealed class CustomerAccountController(
    ICustomerAccountStorefrontService accountService,
    ICustomerPreferenceService preferenceService,
    ILoyaltyService loyaltyService,
    IStoreCreditService storeCreditService,
    ICustomerActivityService activityService,
    ICurrentCustomerContext customerContext,
    IStoreContext storeContext) : ControllerBase
{
    [HttpGet("overview")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var result = await accountService.GetOverviewAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("preferences")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ListPreferences(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerStore(out var customerId, out var storeId, out var error))
        {
            return error!;
        }

        var result = await preferenceService.ListAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPut("preferences")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpsertPreference(
        [FromBody] UpsertCustomerPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerStore(out var customerId, out var storeId, out var error))
        {
            return error!;
        }

        var payload = request with { StoreId = request.StoreId ?? storeId };
        var result = await preferenceService.UpsertAsync(customerId, payload, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("loyalty")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetLoyalty(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerStore(out var customerId, out var storeId, out var error))
        {
            return error!;
        }

        var result = await loyaltyService.GetAccountAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("loyalty/transactions")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ListLoyaltyTransactions(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerStore(out var customerId, out var storeId, out var error))
        {
            return error!;
        }

        var result = await loyaltyService.ListTransactionsAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("loyalty/rewards")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ListRewards(CancellationToken cancellationToken)
    {
        var result = await accountService.ListAvailableRewardsAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpPost("loyalty/redeem")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> RedeemReward(
        [FromBody] RedeemLoyaltyRewardRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerStore(out var customerId, out var storeId, out var error))
        {
            return error!;
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new { success = false, error = "Idempotency-Key header is required." });
        }

        var result = await loyaltyService.RedeemRewardAsync(
            customerId,
            storeId,
            request,
            idempotencyKey,
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, x => x);
    }

    [HttpGet("store-credit")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetStoreCredit(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerStore(out var customerId, out var storeId, out var error))
        {
            return error!;
        }

        var currency = storeContext.CurrentCurrencyCode ?? "USD";
        var result = await storeCreditService.GetAccountAsync(customerId, storeId, currency, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    [HttpGet("activity")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ListActivity(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerStore(out var customerId, out var storeId, out var error))
        {
            return error!;
        }

        var result = await activityService.ListAsync(customerId, storeId, 50, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, x => x);
    }

    private bool TryGetCustomerStore(out int customerId, out int storeId, out IActionResult? error)
    {
        customerId = 0;
        storeId = 0;
        error = null;

        if (!customerContext.IsAuthenticated || !customerContext.CustomerId.HasValue)
        {
            error = Unauthorized(new { success = false, error = "Customer authentication is required." });
            return false;
        }

        if (!storeContext.CurrentStoreId.HasValue)
        {
            error = BadRequest(new { success = false, error = "Store context is required." });
            return false;
        }

        customerId = customerContext.CustomerId.Value;
        storeId = storeContext.CurrentStoreId.Value;
        return true;
    }

    private IActionResult ToActionResult<T>(Commerce.Framework.Core.Results.Result<T> result, Func<T, object?> selector) =>
        result.IsSuccess ? Ok(new { success = true, data = selector(result.Value!) }) : BadRequest(new { success = false, error = result.Error!.Message });
}
