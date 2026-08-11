using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Commerce.Store.Application.Currencies;
using Commerce.Store.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Store;

[ApiController]
[Route("api/currencies")]
public sealed class CurrenciesController(ICurrencyService currencyService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await currencyService.ListAsync(includeInactive: false, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission(StorePermissions.CurrenciesView)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await currencyService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPost]
    [RequirePermission(StorePermissions.CurrenciesCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var result = await currencyService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(StorePermissions.CurrenciesUpdate)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var result = await currencyService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
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
