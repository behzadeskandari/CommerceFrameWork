using Commerce.Downloads.Contracts.Admin;
using Commerce.Downloads.Infrastructure.Security;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Downloads;

[ApiController]
[Route("api/admin/downloads/products/{productId:int}")]
public sealed class AdminProductDownloadsController(IDownloadAdminService downloadAdminService) : ControllerBase
{
    [HttpGet("settings")]
    [RequirePermission(DownloadPermissions.View)]
    public async Task<IActionResult> GetSettings(int productId, CancellationToken cancellationToken)
    {
        var result = await downloadAdminService.GetSettingsAsync(productId, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("settings")]
    [RequirePermission(DownloadPermissions.Configure)]
    public async Task<IActionResult> SaveSettings(
        int productId,
        [FromBody] SaveProductDownloadSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await downloadAdminService.SaveSettingsAsync(productId, request, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("files")]
    [RequirePermission(DownloadPermissions.View)]
    public async Task<IActionResult> ListFiles(int productId, CancellationToken cancellationToken)
    {
        var result = await downloadAdminService.ListFilesAsync(productId, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("files")]
    [RequirePermission(DownloadPermissions.Configure)]
    public async Task<IActionResult> AddFile(
        int productId,
        [FromBody] AddProductDownloadFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await downloadAdminService.AddFileAsync(productId, request, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result, value => value, successStatusCode: StatusCodes.Status201Created);
    }

    [HttpPut("files/{fileId:int}")]
    [RequirePermission(DownloadPermissions.Configure)]
    public async Task<IActionResult> UpdateFile(
        int productId,
        int fileId,
        [FromBody] UpdateProductDownloadFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await downloadAdminService.UpdateFileAsync(productId, fileId, request, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("files/{fileId:int}")]
    [RequirePermission(DownloadPermissions.Configure)]
    public async Task<IActionResult> RemoveFile(int productId, int fileId, CancellationToken cancellationToken)
    {
        var result = await downloadAdminService.RemoveFileAsync(productId, fileId, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result);
    }

    [HttpGet("history")]
    [RequirePermission(DownloadPermissions.View)]
    public async Task<IActionResult> GetHistory(int productId, CancellationToken cancellationToken)
    {
        var result = await downloadAdminService.GetProductHistoryAsync(productId, cancellationToken).ConfigureAwait(false);
        return DownloadActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class DownloadActionResults
{
    internal static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Result<T> result,
        Func<T, object?> dataSelector,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(controller, result.Error!);
    }

    internal static IActionResult ToActionResult(ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(new { success = true });
        }

        return MapFailure(controller, result.Error!);
    }

    private static IActionResult MapFailure(ControllerBase controller, Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, new { success = false, error = error.Message }),
            ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}
