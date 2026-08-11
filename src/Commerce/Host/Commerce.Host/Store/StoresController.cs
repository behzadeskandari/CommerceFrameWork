using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Commerce.Store.Application.Stores;
using Commerce.Store.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Store;

[ApiController]
[Route("api/stores")]
public sealed class StoresController(IStoreService storeService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(StorePermissions.StoresView)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await storeService.ListAsync(includeInactive: true, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(StorePermissions.StoresView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await storeService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost]
    [RequirePermission(StorePermissions.StoresCreate)]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
    {
        var result = await storeService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(StorePermissions.StoresUpdate)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStoreRequest request, CancellationToken cancellationToken)
    {
        var result = await storeService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(StorePermissions.StoresDelete)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await storeService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/domains")]
    [RequirePermission(StorePermissions.StoresUpdate)]
    public async Task<IActionResult> AddDomain(int id, [FromBody] AddStoreDomainRequest request, CancellationToken cancellationToken)
    {
        var result = await storeService.AddDomainAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess) return Ok(new { success = true });
        return MapFailure(result.Error!);
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> dataSelector, Func<T, int>? createdId = null)
    {
        if (result.IsSuccess)
        {
            if (createdId is not null)
            {
                return CreatedAtAction(nameof(Get), new { id = createdId(result.Value!) }, new { success = true, data = dataSelector(result.Value!) });
            }

            return Ok(new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult MapFailure(Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}
