using Commerce.Catalog.Application.Offers;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Catalog;

[ApiController]
[Route("api/catalog/offers")]
public sealed class OffersController(IOfferService offerService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [RequirePermission("Catalog.Offers.View")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await offerService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission("Catalog.Offers.Create")]
    public async Task<IActionResult> Create([FromBody] CreateOfferApiRequest request, CancellationToken cancellationToken)
    {
        var result = await offerService.CreateAsync(new CreateOfferRequest(
            request.ProductId,
            request.VariantId,
            request.StoreId,
            request.CurrencyId,
            request.CurrencyCode,
            request.Price,
            request.CompareAtPrice,
            request.IsActive,
            request.ValidFromUtc,
            request.ValidToUtc), cancellationToken).ConfigureAwait(false);

        return CatalogActionResults.ToActionResult(
            this,
            result,
            value => value,
            createdAtAction: nameof(Get),
            createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Catalog.Offers.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOfferApiRequest request, CancellationToken cancellationToken)
    {
        var result = await offerService.UpdateAsync(id, new UpdateOfferRequest(
            request.Price,
            request.CompareAtPrice,
            request.IsActive,
            request.ValidFromUtc,
            request.ValidToUtc), cancellationToken).ConfigureAwait(false);

        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("products/{productId:int}")]
    [RequirePermission("Catalog.Offers.View")]
    public async Task<IActionResult> ListForProduct(int productId, [FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await offerService.ListForProductAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("variants/{variantId:int}")]
    [RequirePermission("Catalog.Offers.View")]
    public async Task<IActionResult> ListForVariant(int variantId, [FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await offerService.ListForVariantAsync(variantId, storeId, cancellationToken).ConfigureAwait(false);
        return CatalogActionResults.ToActionResult(this, result, value => value);
    }
}

public sealed record CreateOfferApiRequest(
    int ProductId,
    int? VariantId,
    int StoreId,
    int CurrencyId,
    string CurrencyCode,
    decimal Price,
    decimal? CompareAtPrice = null,
    bool IsActive = true,
    DateTime? ValidFromUtc = null,
    DateTime? ValidToUtc = null);

public sealed record UpdateOfferApiRequest(
    decimal Price,
    decimal? CompareAtPrice = null,
    bool IsActive = true,
    DateTime? ValidFromUtc = null,
    DateTime? ValidToUtc = null);
