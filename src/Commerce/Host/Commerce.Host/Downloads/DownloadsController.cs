using Commerce.Customers.Contracts.Customers;
using Commerce.Downloads.Contracts.Storefront;
using Commerce.Host.Downloads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Downloads;

[ApiController]
[Route("api/downloads")]
[Authorize]
public sealed class DownloadsController(
    ICustomerDownloadService downloadService,
    ICurrentCustomerContext currentCustomerContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var result = await downloadService.ListAsync(customerId, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{entitlementId:int}/files/{fileId:int}")]
    public async Task<IActionResult> Download(int entitlementId, int fileId, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Authentication required." });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await downloadService
            .DownloadAsync(customerId, entitlementId, fileId, ipAddress, userAgent, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return DownloadActionResults.ToActionResult(this, result, _ => (object?)null);
        }

        var content = result.Value!;
        return File(content.Content, content.ContentType, content.FileName, enableRangeProcessing: true);
    }
}
