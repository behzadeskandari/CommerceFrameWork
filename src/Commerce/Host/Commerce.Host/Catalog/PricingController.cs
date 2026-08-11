using Commerce.Catalog.Contracts.Pricing;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/pricing")]
public sealed class PricingController(IPricingService pricingService, ICatalogPricingReader pricingReader) : ControllerBase
{
    [HttpGet("products/{productId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductPrice(int productId, [FromQuery] int? currencyId, CancellationToken cancellationToken)
    {
        var result = await pricingService.ResolveProductPriceAsync(productId, currencyId, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("variants/{variantId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVariantPrice(int variantId, [FromQuery] int? currencyId, CancellationToken cancellationToken)
    {
        var result = await pricingService.ResolveVariantPriceAsync(variantId, currencyId, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("offers/{offerId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOfferPrice(int offerId, CancellationToken cancellationToken)
    {
        var price = await pricingReader.GetOfferPriceAsync(offerId, cancellationToken).ConfigureAwait(false);
        return price is null
            ? NotFound(new { success = false, error = $"Offer '{offerId}' was not found or is inactive." })
            : Ok(new { success = true, data = price });
    }
}
