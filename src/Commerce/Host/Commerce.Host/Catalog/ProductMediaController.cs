using Commerce.Catalog.Application.Media;
using Commerce.Catalog.Contracts.Media;
using Commerce.Host.Catalog;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/products/{productId:int}/media")]
public sealed class ProductMediaController(IProductMediaService productMediaService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Catalog.Products.View")]
    public async Task<IActionResult> List(int productId, CancellationToken cancellationToken)
    {
        var items = await productMediaService.GetForProductAsync(productId, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> Assign(
        int productId,
        [FromBody] AssignProductMediaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productMediaService.AssignAsync(productId, request, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{mediaAssetId:int}")]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> Remove(int productId, int mediaAssetId, CancellationToken cancellationToken)
    {
        var result = await productMediaService.RemoveAsync(productId, mediaAssetId, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result);
    }
}

[ApiController]
[Route("api/catalog/variants/{variantId:int}/media")]
public sealed class ProductVariantMediaController(IProductMediaService productMediaService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Catalog.Variants.View")]
    public async Task<IActionResult> List(int variantId, CancellationToken cancellationToken)
    {
        var items = await productMediaService.GetForVariantAsync(variantId, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true, data = items });
    }

    [HttpPost]
    [RequirePermission("Catalog.Variants.Update")]
    public async Task<IActionResult> Assign(
        int variantId,
        [FromBody] AssignProductMediaRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productMediaService.AssignVariantMediaAsync(variantId, request, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpDelete("{mediaAssetId:int}")]
    [RequirePermission("Catalog.Variants.Update")]
    public async Task<IActionResult> Remove(int variantId, int mediaAssetId, CancellationToken cancellationToken)
    {
        var result = await productMediaService.RemoveVariantMediaAsync(variantId, mediaAssetId, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result);
    }
}
