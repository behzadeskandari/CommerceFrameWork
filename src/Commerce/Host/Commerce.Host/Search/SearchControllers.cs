using Commerce.Framework.Contracts.Tenancy;
using Commerce.Search.Contracts.Admin;
using Commerce.Search.Contracts.Storefront;
using Commerce.Search.Infrastructure.Security;
using Commerce.Cache.Infrastructure.DependencyInjection;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Commerce.Host.Search;

[ApiController]
[Route("api/search")]
public sealed class SearchStorefrontController(IStoreContext storeContext, ISearchStorefrontService service) : ControllerBase
{
    [HttpGet("products")]
    [AllowAnonymous]
    [OutputCache(PolicyName = CacheOutputPolicies.StorefrontSearch)]
    public async Task<IActionResult> Search([FromQuery] ProductSearchRequestDto request, CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId ?? 1;
        var languageId = storeContext.CurrentLanguageId ?? 1;
        var result = await service.SearchProductsAsync(request, storeId, languageId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = result });
    }

    [HttpGet("suggest")]
    [AllowAnonymous]
    [OutputCache(PolicyName = CacheOutputPolicies.StorefrontSearch)]
    public async Task<IActionResult> Suggest([FromQuery] string q, CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId ?? 1;
        var languageId = storeContext.CurrentLanguageId ?? 1;
        var result = await service.SuggestAsync(q, storeId, languageId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = result });
    }
}

[ApiController]
[Route("api/admin/search")]
public sealed class AdminSearchController(ISearchAdminService service) : ControllerBase
{
    [HttpGet("status")]
    [RequirePermission(SearchPermissions.View)]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var status = await service.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { data = status });
    }

    [HttpPost("rebuild")]
    [RequirePermission(SearchPermissions.Manage)]
    public async Task<IActionResult> Rebuild(CancellationToken cancellationToken)
    {
        await service.QueueFullRebuildAsync(cancellationToken).ConfigureAwait(false);
        await service.ProcessPendingJobsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var status = await service.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { data = status });
    }

    [HttpPost("process-jobs")]
    [RequirePermission(SearchPermissions.Manage)]
    public async Task<IActionResult> ProcessJobs([FromQuery] int batchSize = 20, CancellationToken cancellationToken = default)
    {
        await service.ProcessPendingJobsAsync(batchSize, cancellationToken).ConfigureAwait(false);
        var status = await service.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { data = status });
    }
}
