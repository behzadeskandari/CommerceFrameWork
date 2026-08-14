using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.Customers;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.Customers;

public sealed class CustomerService(
    ICustomerRepository customerRepository,
    ICustomerAddressRepository addressRepository,
    IEnumerable<ICustomerRegisteredHandler> registeredHandlers,
    IAuditPublisher auditPublisher) : ICustomerService
{
    public async Task<Result<CustomerDetailDto>> RegisterAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var normalizedEmail = request.Email.Trim().ToUpperInvariant();

            if (await customerRepository.GetByIdentityUserIdAsync(request.IdentityUserId, cancellationToken)
                    .ConfigureAwait(false) is not null)
            {
                return Result.Failure<CustomerDetailDto>(
                    Error.Conflict("A customer profile already exists for this identity user."));
            }

            if (await customerRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken)
                    .ConfigureAwait(false) is not null)
            {
                return Result.Failure<CustomerDetailDto>(
                    Error.Conflict($"A customer with email '{request.Email}' already exists."));
            }

            var customer = Customer.Create(
                request.IdentityUserId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.PhoneNumber);

            await customerRepository.AddAsync(customer, cancellationToken).ConfigureAwait(false);
            foreach (var handler in registeredHandlers)
            {
                await handler.HandleCustomerRegisteredAsync(customer.Id, customer.Email, cancellationToken).ConfigureAwait(false);
            }

            return Result.Success(await MapDetailAsync(customer, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CustomerDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<CustomerDetailDto>> UpdateAsync(
        int customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.Deleted)
        {
            return Result.Failure<CustomerDetailDto>(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        try
        {
            customer.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);
            await customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await PublishCustomerChangedAuditAsync(customer, cancellationToken).ConfigureAwait(false);
            return Result.Success(await MapDetailAsync(customer, cancellationToken).ConfigureAwait(false));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CustomerDetailDto>(Error.Validation(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CustomerDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeactivateAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.Deleted)
        {
            return Result.Failure(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        customer.Deactivate();
        await customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
        await PublishCustomerChangedAuditAsync(customer, cancellationToken, deactivated: true).ConfigureAwait(false);
        return Result.Success();
    }

    private Task PublishCustomerChangedAuditAsync(
        Customer customer,
        CancellationToken cancellationToken,
        bool deactivated = false) =>
        auditPublisher.PublishAsync(new AuditPublishRequest(
            AuditCategory.Customer,
            AuditActions.CustomerUpdated,
            Success: true,
            EntityType: nameof(Customer),
            EntityId: customer.Id.ToString(),
            Details: new Dictionary<string, string?>
            {
                ["email"] = customer.Email,
                ["deactivated"] = deactivated.ToString()
            }), cancellationToken);

    public async Task<Result<CustomerDetailDto>> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.Deleted)
        {
            return Result.Failure<CustomerDetailDto>(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        return Result.Success(await MapDetailAsync(customer, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<IReadOnlyList<CustomerSummaryDto>>> ListAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var customers = await customerRepository.ListAsync(includeDeleted, cancellationToken).ConfigureAwait(false);
        var summaries = customers
            .Select(c => new CustomerSummaryDto(
                c.Id,
                c.Email,
                c.FirstName,
                c.LastName,
                c.PhoneNumber,
                c.Active,
                c.Deleted,
                c.CreatedAtUtc))
            .ToList();

        return Result.Success<IReadOnlyList<CustomerSummaryDto>>(summaries);
    }

    private async Task<CustomerDetailDto> MapDetailAsync(Customer customer, CancellationToken cancellationToken)
    {
        var addresses = await addressRepository
            .ListByCustomerIdAsync(customer.Id, cancellationToken)
            .ConfigureAwait(false);

        return new CustomerDetailDto(
            customer.Id,
            customer.IdentityUserId,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.PhoneNumber,
            customer.Active,
            customer.Deleted,
            customer.IsTaxExempt,
            customer.TaxRegistrationNumber,
            customer.CustomerGroupId,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc,
            addresses.Select(MapAddress).ToList());
    }

    internal static CustomerAddressDto MapAddress(CustomerAddress address) =>
        new(
            address.Id,
            address.CustomerId,
            address.Label,
            address.FirstName,
            address.LastName,
            address.Country,
            address.StateProvince,
            address.City,
            address.Address1,
            address.Address2,
            address.PostalCode,
            address.PhoneNumber,
            address.IsDefaultBilling,
            address.IsDefaultShipping,
            address.CreatedAtUtc,
            address.UpdatedAtUtc);
}
