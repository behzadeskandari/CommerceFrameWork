using Commerce.Host.Authorization;
using Commerce.Pricing.Contracts.AdvancedPricing;
using Commerce.Pricing.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Pricing;

[ApiController]
[Route("api/admin/pricing/customer-groups")]
public sealed class AdminCustomerGroupsController(ICustomerGroupAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PricingPermissions.CustomerGroupsView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var items = await service.ListGroupsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = items });
    }

    [HttpGet("{id:int}")]
    [RequirePermission(PricingPermissions.CustomerGroupsView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var item = await service.GetGroupAsync(id, cancellationToken).ConfigureAwait(false);
        return item is null ? NotFound() : Ok(new { data = item });
    }

    [HttpPost]
    [RequirePermission(PricingPermissions.CustomerGroupsManage)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerGroupRequest request, CancellationToken cancellationToken)
    {
        var item = await service.CreateGroupAsync(request, cancellationToken).ConfigureAwait(false);
        return Created(string.Empty, new { data = item });
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PricingPermissions.CustomerGroupsManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerGroupRequest request, CancellationToken cancellationToken)
    {
        var item = await service.UpdateGroupAsync(id, request, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = item });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PricingPermissions.CustomerGroupsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteGroupAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = new { } });
    }

    [HttpGet("{id:int}/prices")]
    [RequirePermission(PricingPermissions.CustomerGroupsView)]
    public async Task<IActionResult> ListPrices(int id, CancellationToken cancellationToken)
    {
        var items = await service.ListGroupPricesAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = items });
    }

    [HttpPost("{id:int}/prices")]
    [RequirePermission(PricingPermissions.CustomerGroupsManage)]
    public async Task<IActionResult> AddPrice(int id, [FromBody] CreateCustomerGroupPriceRequest request, CancellationToken cancellationToken)
    {
        var item = await service.AddGroupPriceAsync(request with { CustomerGroupId = id }, cancellationToken).ConfigureAwait(false);
        return Created(string.Empty, new { data = item });
    }

    [HttpPut("prices/{priceId:int}")]
    [RequirePermission(PricingPermissions.CustomerGroupsManage)]
    public async Task<IActionResult> UpdatePrice(int priceId, [FromBody] UpdateCustomerGroupPriceRequest request, CancellationToken cancellationToken)
    {
        var item = await service.UpdateGroupPriceAsync(priceId, request, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = item });
    }

    [HttpDelete("prices/{priceId:int}")]
    [RequirePermission(PricingPermissions.CustomerGroupsManage)]
    public async Task<IActionResult> DeletePrice(int priceId, CancellationToken cancellationToken)
    {
        await service.DeleteGroupPriceAsync(priceId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = new { } });
    }
}

[ApiController]
[Route("api/admin/pricing")]
public sealed class AdminAdvancedPricingController(IAdvancedPricingService service) : ControllerBase
{
    [HttpPost("preview")]
    [RequirePermission(PricingPermissions.CustomerGroupsView)]
    public async Task<IActionResult> Preview([FromBody] PricePreviewRequest request, CancellationToken cancellationToken)
    {
        var result = await service.PreviewAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = result });
    }
}
