using Commerce.Host.Authorization;
using Commerce.Integration.Contracts.ApiClients;
using Commerce.Integration.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Integration;

[ApiController]
[Route("api/admin/api-clients")]
public sealed class AdminApiClientsController(IApiClientAdminService apiClientAdminService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(IntegrationPermissions.ApiClientsView)]
    public async Task<IActionResult> List([FromQuery] int? storeId, CancellationToken cancellationToken)
    {
        var result = await apiClientAdminService.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(IntegrationPermissions.ApiClientsView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await apiClientAdminService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost]
    [RequirePermission(IntegrationPermissions.ApiClientsManage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApiClientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await apiClientAdminService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(
            this,
            result,
            value => new { client = value.Client, apiKey = value.ApiKey },
            StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(IntegrationPermissions.ApiClientsManage)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateApiClientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await apiClientAdminService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result, value => value);
    }

    [HttpPost("{id:int}/revoke")]
    [RequirePermission(IntegrationPermissions.ApiClientsManage)]
    public async Task<IActionResult> Revoke(int id, CancellationToken cancellationToken)
    {
        var result = await apiClientAdminService.RevokeAsync(id, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(IntegrationPermissions.ApiClientsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await apiClientAdminService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return IntegrationActionResults.ToActionResult(this, result);
    }
}
