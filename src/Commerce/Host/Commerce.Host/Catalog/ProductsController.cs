using Commerce.Catalog.Application.Categories;
using Commerce.Catalog.Application.Products;
using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await productService.ListAsync(includeDeleted: false, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost]
    [RequirePermission("Catalog.Products.Create")]
    public async Task<IActionResult> Create([FromBody] CreateProductApiRequest request, CancellationToken cancellationToken)
    {
        var result = await productService.CreateAsync(new CreateProductRequest(
            request.Name,
            request.Sku,
            request.ProductType,
            request.ShortDescription,
            request.Description,
            request.Slug,
            request.Published,
            request.DisplayOrder,
            request.CategoryIds), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductApiRequest request, CancellationToken cancellationToken)
    {
        var result = await productService.UpdateAsync(id, new UpdateProductRequest(
            request.Name,
            request.ProductType,
            request.ShortDescription,
            request.Description,
            request.Slug,
            request.Published,
            request.DisplayOrder,
            request.CategoryIds), cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("Catalog.Products.Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await productService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/categories/{categoryId:int}")]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> AssignCategory(int id, int categoryId, CancellationToken cancellationToken)
    {
        var result = await productService.AssignCategoryAsync(new AssignProductCategoryRequest(id, categoryId), cancellationToken)
            .ConfigureAwait(false);
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

public sealed record CreateProductApiRequest(
    string Name,
    string Sku,
    ProductType ProductType,
    string? ShortDescription = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    int DisplayOrder = 0,
    IReadOnlyList<int>? CategoryIds = null);

public sealed record UpdateProductApiRequest(
    string Name,
    ProductType ProductType,
    string? ShortDescription = null,
    string? Description = null,
    string? Slug = null,
    bool Published = false,
    int DisplayOrder = 0,
    IReadOnlyList<int>? CategoryIds = null);
