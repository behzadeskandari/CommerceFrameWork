using Commerce.Host.Tax;
using Commerce.Tax.Contracts.Admin;
using Commerce.Tax.Infrastructure.Security;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Tax;

[ApiController]
[Route("api/admin/tax/settings")]
public sealed class AdminTaxSettingsController(ITaxSettingsAdminService service) : ControllerBase
{
    [HttpGet]
    [RequirePermission(TaxPermissions.View)]
    public async Task<IActionResult> Get([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var settings = await service.GetAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = settings });
    }

    [HttpPut]
    [RequirePermission(TaxPermissions.Manage)]
    public async Task<IActionResult> Update([FromQuery] int? storeId, [FromBody] UpdateTaxSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await service.UpdateAsync(storeId, request, cancellationToken).ConfigureAwait(false);
        return Ok(new { data = settings });
    }
}
