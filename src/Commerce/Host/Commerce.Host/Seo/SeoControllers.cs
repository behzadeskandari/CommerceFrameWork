using Commerce.Framework.Contracts.Tenancy;
using Commerce.Host.Authorization;
using Commerce.Seo.Contracts.Admin;
using Commerce.Seo.Contracts.Storefront;
using Commerce.Seo.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Seo;

internal static class SeoActionResults
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Commerce.Framework.Core.Results.Result<T> result, Func<T, object?> map) =>
        result.IsSuccess
            ? controller.Ok(new { data = map(result.Value!) })
            : MapError(controller, result.Error!);

    public static IActionResult MapError(ControllerBase controller, Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Validation => controller.BadRequest(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}

[ApiController]
[Route("api/admin/seo")]
public sealed class AdminSeoController(ISeoAdminService service) : ControllerBase
{
    [HttpGet("url-records")]
    [RequirePermission(SeoPermissions.View)]
    public async Task<IActionResult> ListUrlRecords([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await service.ListUrlRecordsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return SeoActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPut("url-records")]
    [RequirePermission(SeoPermissions.Manage)]
    public async Task<IActionResult> UpsertUrlRecord([FromBody] UpsertUrlRecordRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertUrlRecordAsync(request, cancellationToken).ConfigureAwait(false);
        return SeoActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPut("metadata")]
    [RequirePermission(SeoPermissions.Manage)]
    public async Task<IActionResult> UpsertMetadata([FromBody] UpsertSeoMetadataRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpsertMetadataAsync(request, cancellationToken).ConfigureAwait(false);
        return SeoActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("settings/{storeId:int}")]
    [RequirePermission(SeoPermissions.View)]
    public async Task<IActionResult> GetSettings(int storeId, CancellationToken cancellationToken)
    {
        var result = await service.GetSettingsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return SeoActionResults.ToActionResult(this, result, x => x);
    }

    [HttpPut("settings/{storeId:int}")]
    [RequirePermission(SeoPermissions.Manage)]
    public async Task<IActionResult> UpdateSettings(int storeId, [FromBody] UpdateSeoSettingsRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateSettingsAsync(storeId, request, cancellationToken).ConfigureAwait(false);
        return SeoActionResults.ToActionResult(this, result, x => x);
    }
}

[ApiController]
[Route("api/seo")]
public sealed class SeoStorefrontController(
    ISeoStorefrontService seoService,
    IStoreContext storeContext) : ControllerBase
{
    [HttpGet("resolve/{slug}")]
    public async Task<IActionResult> Resolve(string slug, [FromQuery] int? languageId, CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId ?? 1;
        var result = await seoService.ResolveSlugAsync(slug, languageId, storeId, cancellationToken).ConfigureAwait(false);
        return SeoActionResults.ToActionResult(this, result, x => x);
    }

    [HttpGet("metadata/{entityName}/{entityId:int}")]
    public async Task<IActionResult> Metadata(string entityName, int entityId, [FromQuery] int? languageId, CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId ?? 1;
        var result = await seoService.GetMetadataAsync(entityName, entityId, languageId, storeId, cancellationToken).ConfigureAwait(false);
        return SeoActionResults.ToActionResult(this, result, x => x);
    }
}

[ApiController]
public sealed class SeoPublicController(
    ISeoStorefrontService seoService,
    IStoreContext storeContext,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("/robots.txt")]
    public async Task<IActionResult> Robots(CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId ?? 1;
        var result = await seoService.GetRobotsTxtAsync(storeId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Content(result.Value!, "text/plain")
            : Content("User-agent: *\nAllow: /", "text/plain");
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var storeId = storeContext.CurrentStoreId ?? 1;
        var baseUrl = configuration["Commerce:PublicBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var result = await seoService.GetSitemapXmlAsync(storeId, baseUrl, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Content(result.Value!, "application/xml")
            : NotFound();
    }
}
