using Commerce.Catalog.Application.Categories;
using Commerce.Framework.Core.Results;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await categoryService.ListAsync(cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await categoryService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost]
    [CatalogAdminRequired]
    public async Task<IActionResult> Create([FromBody] CreateCategoryApiRequest request, CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateAsync(new CreateCategoryRequest(
            request.Name,
            request.ParentCategoryId,
            request.Description,
            request.Slug,
            request.Published,
            request.DisplayOrder), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [CatalogAdminRequired]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryApiRequest request, CancellationToken cancellationToken)
    {
        var result = await categoryService.UpdateAsync(id, new UpdateCategoryRequest(
            request.Name,
            request.ParentCategoryId,
            request.Description,
            request.Slug,
            request.Published,
            request.DisplayOrder), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    [HttpDelete("{id:int}")]
    [CatalogAdminRequired]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await categoryService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> dataSelector, Func<T, int>? createdId = null)
    {
        if (result.IsSuccess)
        {
            if (createdId is not null)
            {
                return CreatedAtAction(nameof(Get), new { id = createdId(result.Value!) }, new { success = true, data = dataSelector(result.Value!) });
            }

            return Ok(new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult MapFailure(Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}

public sealed record CreateCategoryApiRequest(
    string Name,
    int? ParentCategoryId = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    int DisplayOrder = 0);

public sealed record UpdateCategoryApiRequest(
    string Name,
    int? ParentCategoryId = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    int DisplayOrder = 0);
