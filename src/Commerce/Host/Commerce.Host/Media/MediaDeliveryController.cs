using Commerce.Media.Contracts.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Commerce.Host.Media;

[ApiController]
[Route("api/media")]
public sealed class MediaDeliveryController(IMediaService mediaService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var meta = await mediaService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (!meta.IsSuccess)
        {
            return NotFound(new { success = false, error = meta.Error!.Message });
        }

        if (!meta.Value!.IsPublic)
        {
            return Unauthorized(new { success = false, error = "Media is private." });
        }

        var streamResult = await mediaService.OpenPublicReadAsync(id, thumbnail: false, cancellationToken).ConfigureAwait(false);
        return await ToFileResultAsync(streamResult, meta.Value.ContentType, meta.Value.FileName, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}/thumbnail")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThumbnail(int id, CancellationToken cancellationToken)
    {
        var meta = await mediaService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (!meta.IsSuccess)
        {
            return NotFound(new { success = false, error = meta.Error!.Message });
        }

        if (!meta.Value!.IsPublic)
        {
            return Unauthorized(new { success = false, error = "Media is private." });
        }

        var streamResult = await mediaService.OpenPublicReadAsync(id, thumbnail: true, cancellationToken).ConfigureAwait(false);
        return await ToFileResultAsync(streamResult, "image/jpeg", $"thumb-{meta.Value.FileName}", cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{id:int}/private")]
    [Authorize]
    public async Task<IActionResult> GetPrivate(int id, CancellationToken cancellationToken)
    {
        var meta = await mediaService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (!meta.IsSuccess)
        {
            return NotFound(new { success = false, error = meta.Error!.Message });
        }

        var streamResult = await mediaService.OpenAuthorizedReadAsync(id, thumbnail: false, cancellationToken).ConfigureAwait(false);
        return await ToFileResultAsync(streamResult, meta.Value!.ContentType, meta.Value.FileName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IActionResult> ToFileResultAsync(
        Commerce.Framework.Core.Results.Result<Stream> streamResult,
        string contentType,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (!streamResult.IsSuccess || streamResult.Value is null)
        {
            return NotFound(new { success = false, error = streamResult.Error?.Message ?? "Media file not found." });
        }

        Response.Headers[HeaderNames.CacheControl] = "public,max-age=3600";
        Response.Headers[HeaderNames.ETag] = $"\"{fileName.GetHashCode():x}\"";
        return File(streamResult.Value, contentType, enableRangeProcessing: true);
    }
}
