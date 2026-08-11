using Commerce.Catalog.Application.Storefront;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/storefront")]
public sealed class CatalogStorefrontController(IStorefrontCatalogService storefrontCatalogService) : ControllerBase
{
    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<IActionResult> ListProducts([FromQuery] string? term, CancellationToken cancellationToken)
    {
        var result = await storefrontCatalogService.ListProductsAsync(term, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("products/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(int id, CancellationToken cancellationToken)
    {
        var result = await storefrontCatalogService.GetProductByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("products/by-slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await storefrontCatalogService.GetProductBySlugAsync(slug, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }
}
