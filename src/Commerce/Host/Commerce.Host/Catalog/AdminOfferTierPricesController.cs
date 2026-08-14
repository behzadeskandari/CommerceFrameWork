using Commerce.Catalog.Contracts.Offers;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/admin/catalog/offers/{offerId:int}/tier-prices")]
public sealed class AdminOfferTierPricesController(IOfferTierPriceAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> List(int offerId, CancellationToken cancellationToken)
    {
        var items = await service.ListAsync(offerId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = items });
    }

    [HttpPost]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> Create(int offerId, [FromBody] CreateOfferTierPriceRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateAsync(offerId, request, cancellationToken).ConfigureAwait(false);
        return Created(string.Empty, new { data = item });
    }

    [HttpPut("{tierPriceId:int}")]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> Update(int offerId, int tierPriceId, [FromBody] UpdateOfferTierPriceRequest request, CancellationToken cancellationToken)
    {
        var item = await service.UpdateAsync(offerId, tierPriceId, request, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = item });
    }

    [HttpDelete("{tierPriceId:int}")]
    [RequirePermission("Catalog.Products.Update")]
    public async Task<IActionResult> Delete(int offerId, int tierPriceId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(offerId, tierPriceId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = new { } });
    }
}
