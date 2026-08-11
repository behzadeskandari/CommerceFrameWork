using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

internal static class CatalogActionResults
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
        string? createdController = null,
        Func<T, int>? createdId = null)
    {
        if (result.IsSuccess)
        {
            if (createdAtAction is not null && createdId is not null)
            {
                return controller.CreatedAtAction(
                    createdAtAction,
                    createdController,
                    new { id = createdId(result.Value!) },
                    new { success = true, data = dataSelector(result.Value!) });
            }

            return controller.Ok(new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(controller, result.Error!);
    }

    private static IActionResult MapFailure(ControllerBase controller, Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}
