using Commerce.Catalog.Application.Variants;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/variants")]
public sealed class VariantsController(IVariantService variantService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [RequirePermission("Catalog.Variants.View")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await variantService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Catalog.Variants.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVariantApiRequest request, CancellationToken cancellationToken)
    {
        var result = await variantService.UpdateAsync(id, new UpdateVariantRequest(
            request.Name,
            request.AttributeOptionIds,
            request.IsActive,
            request.IsDefault,
            request.DisplayOrder), cancellationToken).ConfigureAwait(false);

        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("Catalog.Variants.Delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await variantService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/catalog/products/{productId:int}/variants")]
public sealed class ProductVariantsController(IVariantService variantService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Catalog.Variants.View")]
    public async Task<IActionResult> List(int productId, [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var result = await variantService.ListForProductAsync(productId, includeInactive, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission("Catalog.Variants.Create")]
    public async Task<IActionResult> Create(int productId, [FromBody] CreateVariantApiRequest request, CancellationToken cancellationToken)
    {
        var result = await variantService.CreateAsync(new CreateVariantRequest(
            productId,
            request.Sku,
            request.Name,
            request.AttributeOptionIds,
            request.IsActive,
            request.IsDefault,
            request.DisplayOrder), cancellationToken).ConfigureAwait(false);

        return CatalogActionResults.ToActionResult(
            this,
            result,
            value => value,
            createdAtAction: nameof(VariantsController.Get),
            createdController: "Variants",
            createdId: value => value.Id);
    }

    [HttpPost("generate")]
    [RequirePermission("Catalog.Variants.Create")]
    public async Task<IActionResult> Generate(int productId, [FromBody] GenerateVariantsApiRequest request, CancellationToken cancellationToken)
    {
        var result = await variantService.GenerateAsync(new GenerateVariantsRequest(
            productId,
            request.SkuPrefix,
            request.SkipExisting), cancellationToken).ConfigureAwait(false);

        return CatalogActionResults.ToActionResult(this, result, value => value);
    }
}

public sealed record CreateVariantApiRequest(
    string Sku,
    string Name,
    IReadOnlyList<int> AttributeOptionIds,
    bool IsActive = true,
    bool IsDefault = false,
    int DisplayOrder = 0);

public sealed record UpdateVariantApiRequest(
    string Name,
    IReadOnlyList<int> AttributeOptionIds,
    bool IsActive = true,
    bool IsDefault = false,
    int DisplayOrder = 0);

public sealed record GenerateVariantsApiRequest(string SkuPrefix, bool SkipExisting = true);
