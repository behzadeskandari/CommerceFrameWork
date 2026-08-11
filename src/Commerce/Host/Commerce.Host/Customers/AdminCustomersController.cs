using Commerce.Customers.Application.Customers;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Customers;

[ApiController]
[Route("api/admin/customers")]
public sealed class AdminCustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Customers.View")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await customerService.ListAsync(includeDeleted: false, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("Customers.View")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await customerService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("Customers.Update")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerApiRequest request, CancellationToken cancellationToken)
    {
        var result = await customerService.UpdateAsync(
            id,
            new UpdateCustomerRequest(request.FirstName, request.LastName, request.PhoneNumber),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    private IActionResult ToActionResult<T>(Result<T> result, Func<T, object?> dataSelector)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(result.Error!);
    }

    private IActionResult MapFailure(Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}
