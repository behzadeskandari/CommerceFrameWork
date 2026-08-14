using Commerce.Integration.Contracts.ApiClients;
using Commerce.Integration.Contracts.ExternalApi;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Integration;

[ApiController]
[Route("api/external/orders")]
public sealed class ExternalOrdersController(IExternalOrderService externalOrderService) : ControllerBase
{
    [HttpGet]
    [RequireApiScope(ApiScopes.OrdersRead)]
    public async Task<IActionResult> List(
        [FromQuery] ExternalOrderListQuery query,
        CancellationToken cancellationToken)
    {
        var storeId = HttpContext.GetApiClientAuthentication()?.StoreId;
        var result = await externalOrderService.ListOrdersAsync(storeId, query, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequireApiScope(ApiScopes.OrdersRead)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var storeId = HttpContext.GetApiClientAuthentication()?.StoreId;
        var result = await externalOrderService.GetOrderAsync(id, storeId, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }
}
