using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Commerce.Media.Contracts.Media;
using Commerce.Media.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Media;

[ApiController]
[Route("api/media")]
public sealed class MediaController(IMediaService mediaService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(MediaPermissions.View)]
    public async Task<IActionResult> List(
        [FromQuery] string? term,
        [FromQuery] MediaType? mediaType,
        CancellationToken cancellationToken)
    {
        var result = await mediaService.ListAsync(term, mediaType, cancellationToken).ConfigureAwait(false);
        return MediaActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}/metadata")]
    [RequirePermission(MediaPermissions.View)]
    public async Task<IActionResult> GetMetadata(int id, CancellationToken cancellationToken)
    {
        var result = await mediaService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return MediaActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("upload")]
    [RequirePermission(MediaPermissions.Upload)]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] bool isPublic = true,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { success = false, error = "A file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await mediaService.UploadAsync(
            new UploadMediaRequest(stream, file.FileName, file.ContentType, file.Length, isPublic),
            cancellationToken).ConfigureAwait(false);

        return MediaActionResults.ToActionResult(
            this,
            result,
            value => value,
            createdAtAction: nameof(GetMetadata),
            createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(MediaPermissions.Update)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMediaApiRequest request, CancellationToken cancellationToken)
    {
        var result = await mediaService.UpdateAsync(
            id,
            new UpdateMediaRequest(request.Title, request.AltText, request.IsPublic),
            cancellationToken).ConfigureAwait(false);

        return MediaActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(MediaPermissions.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await mediaService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return MediaActionResults.ToActionResult(this, result);
    }
}

public sealed record UpdateMediaApiRequest(string? Title, string? AltText, bool IsPublic = true);

internal static class MediaPermissions
{
    internal const string View = "Media.View";
    internal const string Upload = "Media.Upload";
    internal const string Update = "Media.Update";
    internal const string Delete = "Media.Delete";
}

internal static class MediaActionResults
{
    internal static IActionResult ToActionResult(ControllerBase controller, Result result) =>
        result.IsSuccess
            ? controller.Ok(new { success = true })
            : MapFailure(controller, result.Error!);

    internal static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Result<T> result,
        Func<T, object?> dataSelector,
        string? createdAtAction = null,
        Func<T, int>? createdId = null)
    {
        if (result.IsSuccess)
        {
            if (createdAtAction is not null && createdId is not null)
            {
                return controller.CreatedAtAction(
                    createdAtAction,
                    new { id = createdId(result.Value!) },
                    new { success = true, data = dataSelector(result.Value!) });
            }

            return controller.Ok(new { success = true, data = dataSelector(result.Value!) });
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
