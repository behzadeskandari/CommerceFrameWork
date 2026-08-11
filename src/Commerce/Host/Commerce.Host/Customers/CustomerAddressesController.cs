using Commerce.Customers.Application.Addresses;
using Commerce.Customers.Application.Customers;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Customers;

[ApiController]
[Authorize]
[Route("api/customers/me/addresses")]
public sealed class CustomerAddressesController(
    ICustomerAddressService addressService,
    ICurrentCustomerContext currentCustomerContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Customer profile is not available for the current user." });
        }

        var result = await addressService.ListAsync(customerId, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, value => value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Customer profile is not available for the current user." });
        }

        var result = await addressService.ListAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return MapFailure(result.Error!);
        }

        var address = result.Value!.FirstOrDefault(x => x.Id == id);
        if (address is null)
        {
            return NotFound(new { success = false, error = $"Address '{id}' was not found." });
        }

        return Ok(new { success = true, data = address });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddCustomerAddressApiRequest request, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Customer profile is not available for the current user." });
        }

        var result = await addressService.AddAsync(
            customerId,
            new AddCustomerAddressRequest(
                request.Label,
                request.FirstName,
                request.LastName,
                request.Country,
                request.City,
                request.Address1,
                request.PostalCode,
                request.StateProvince,
                request.Address2,
                request.PhoneNumber,
                request.IsDefaultBilling,
                request.IsDefaultShipping),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value, createdId: value => value.Id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCustomerAddressApiRequest request,
        CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Customer profile is not available for the current user." });
        }

        var result = await addressService.UpdateAsync(
            customerId,
            id,
            new UpdateCustomerAddressRequest(
                request.Label,
                request.FirstName,
                request.LastName,
                request.Country,
                request.City,
                request.Address1,
                request.PostalCode,
                request.StateProvince,
                request.Address2,
                request.PhoneNumber,
                request.IsDefaultBilling,
                request.IsDefaultShipping),
            cancellationToken).ConfigureAwait(false);

        return ToActionResult(result, value => value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (currentCustomerContext.CustomerId is not int customerId)
        {
            return Unauthorized(new { success = false, error = "Customer profile is not available for the current user." });
        }

        var result = await addressService.DeleteAsync(customerId, id, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }

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

    private IActionResult MapFailure(Commerce.Framework.Core.Errors.Error error) =>
        error.Type switch
        {
            Commerce.Framework.Core.Errors.ErrorType.NotFound => NotFound(new { success = false, error = error.Message }),
            Commerce.Framework.Core.Errors.ErrorType.Conflict => Conflict(new { success = false, error = error.Message }),
            _ => BadRequest(new { success = false, error = error.Message })
        };
}

public sealed record AddCustomerAddressApiRequest(
    string Label,
    string FirstName,
    string LastName,
    string Country,
    string City,
    string Address1,
    string PostalCode,
    string? StateProvince = null,
    string? Address2 = null,
    string? PhoneNumber = null,
    bool IsDefaultBilling = false,
    bool IsDefaultShipping = false);

public sealed record UpdateCustomerAddressApiRequest(
    string Label,
    string FirstName,
    string LastName,
    string Country,
    string City,
    string Address1,
    string PostalCode,
    string? StateProvince = null,
    string? Address2 = null,
    string? PhoneNumber = null,
    bool IsDefaultBilling = false,
    bool IsDefaultShipping = false);
