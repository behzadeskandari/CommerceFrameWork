using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.CustomerAccount;

public sealed class CustomerPreferenceService(
    ICustomerPreferenceRepository repository,
    ICustomerRepository customerRepository,
    ICustomerActivityService activityService) : ICustomerPreferenceService
{
    public async Task<Result<IReadOnlyList<CustomerPreferenceDto>>> ListAsync(
        int customerId,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.Deleted)
        {
            return Result.Failure<IReadOnlyList<CustomerPreferenceDto>>(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        var preferences = await repository.ListByCustomerAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<CustomerPreferenceDto>>(
            preferences.Select(CustomerAccountMapper.MapPreference).ToList());
    }

    public async Task<Result<CustomerPreferenceDto>> UpsertAsync(
        int customerId,
        UpsertCustomerPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.Deleted)
        {
            return Result.Failure<CustomerPreferenceDto>(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        var existing = await repository
            .GetByKeyAsync(customerId, request.StoreId, request.PreferenceKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            try
            {
                var preference = CustomerPreference.Create(
                    customerId,
                    request.StoreId,
                    request.PreferenceKey,
                    request.PreferenceValue);
                await repository.AddAsync(preference, cancellationToken).ConfigureAwait(false);
                await activityService.LogAsync(
                    customerId,
                    request.StoreId,
                    CustomerActivityType.PreferenceUpdated,
                    $"Preference '{request.PreferenceKey}' created.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return Result.Success(CustomerAccountMapper.MapPreference(preference));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<CustomerPreferenceDto>(Error.Validation(ex.Message));
            }
        }

        existing.UpdateValue(request.PreferenceValue);
        await repository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        await activityService.LogAsync(
            customerId,
            request.StoreId,
            CustomerActivityType.PreferenceUpdated,
            $"Preference '{request.PreferenceKey}' updated.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(CustomerAccountMapper.MapPreference(existing));
    }
}
