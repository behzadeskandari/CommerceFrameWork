using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Application.Customers;
using Commerce.Customers.Contracts.Customers;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Customers.Application.Addresses;

public interface ICustomerAddressService
{
    Task<Result<CustomerAddressDto>> AddAsync(
        int customerId,
        AddCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerAddressDto>> UpdateAsync(
        int customerId,
        int addressId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int customerId, int addressId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerAddressDto>>> ListAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}

public sealed class CustomerAddressService(
    ICustomerRepository customerRepository,
    ICustomerAddressRepository addressRepository) : ICustomerAddressService
{
    public async Task<Result<CustomerAddressDto>> AddAsync(
        int customerId,
        AddCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false) is not { Deleted: false })
        {
            return Result.Failure<CustomerAddressDto>(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        try
        {
            var addressValue = Address.Create(
                request.FirstName,
                request.LastName,
                request.Country,
                request.City,
                request.Address1,
                request.PostalCode,
                request.StateProvince,
                request.Address2,
                request.PhoneNumber);

            if (request.IsDefaultBilling || request.IsDefaultShipping)
            {
                await ClearDefaultFlagsAsync(
                    customerId,
                    clearBilling: request.IsDefaultBilling,
                    clearShipping: request.IsDefaultShipping,
                    excludeAddressId: null,
                    cancellationToken).ConfigureAwait(false);
            }

            var address = CustomerAddress.Create(
                customerId,
                request.Label,
                addressValue,
                request.IsDefaultBilling,
                request.IsDefaultShipping);

            await addressRepository.AddAsync(address, cancellationToken).ConfigureAwait(false);
            return Result.Success(CustomerService.MapAddress(address));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CustomerAddressDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<CustomerAddressDto>> UpdateAsync(
        int customerId,
        int addressId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var address = await addressRepository.GetByIdAsync(addressId, cancellationToken).ConfigureAwait(false);
        if (address is null || address.CustomerId != customerId)
        {
            return Result.Failure<CustomerAddressDto>(
                Error.NotFound($"Address '{addressId}' was not found for customer '{customerId}'."));
        }

        try
        {
            var addressValue = Address.Create(
                request.FirstName,
                request.LastName,
                request.Country,
                request.City,
                request.Address1,
                request.PostalCode,
                request.StateProvince,
                request.Address2,
                request.PhoneNumber);

            if (request.IsDefaultBilling || request.IsDefaultShipping)
            {
                await ClearDefaultFlagsAsync(
                    customerId,
                    clearBilling: request.IsDefaultBilling,
                    clearShipping: request.IsDefaultShipping,
                    excludeAddressId: addressId,
                    cancellationToken).ConfigureAwait(false);
            }

            address.UpdateDetails(
                request.Label,
                addressValue,
                request.IsDefaultBilling,
                request.IsDefaultShipping);

            await addressRepository.UpdateAsync(address, cancellationToken).ConfigureAwait(false);
            return Result.Success(CustomerService.MapAddress(address));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CustomerAddressDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int customerId, int addressId, CancellationToken cancellationToken = default)
    {
        var address = await addressRepository.GetByIdAsync(addressId, cancellationToken).ConfigureAwait(false);
        if (address is null || address.CustomerId != customerId)
        {
            return Result.Failure(Error.NotFound($"Address '{addressId}' was not found for customer '{customerId}'."));
        }

        await addressRepository.DeleteAsync(address, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<CustomerAddressDto>>> ListAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        if (await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result.Failure<IReadOnlyList<CustomerAddressDto>>(
                Error.NotFound($"Customer '{customerId}' was not found."));
        }

        var addresses = await addressRepository.ListByCustomerIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        var dtos = addresses.Select(CustomerService.MapAddress).ToList();
        return Result.Success<IReadOnlyList<CustomerAddressDto>>(dtos);
    }

    private async Task ClearDefaultFlagsAsync(
        int customerId,
        bool clearBilling,
        bool clearShipping,
        int? excludeAddressId = null,
        CancellationToken cancellationToken = default)
    {
        var addresses = await addressRepository.ListByCustomerIdAsync(customerId, cancellationToken).ConfigureAwait(false);

        foreach (var existing in addresses)
        {
            if (excludeAddressId.HasValue && existing.Id == excludeAddressId.Value)
            {
                continue;
            }

            var changed = false;

            if (clearBilling && existing.IsDefaultBilling)
            {
                existing.SetDefaultBilling(false);
                changed = true;
            }

            if (clearShipping && existing.IsDefaultShipping)
            {
                existing.SetDefaultShipping(false);
                changed = true;
            }

            if (changed)
            {
                await addressRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
