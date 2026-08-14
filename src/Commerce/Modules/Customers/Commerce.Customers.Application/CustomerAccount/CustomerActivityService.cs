using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.Customers;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Customers.Application.CustomerAccount;

public sealed class CustomerActivityService(ICustomerActivityRepository repository) : ICustomerActivityService
{
    public async Task<Result<IReadOnlyList<CustomerActivityDto>>> ListAsync(
        int customerId,
        int? storeId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var activities = await repository
            .ListAsync(customerId, storeId, Math.Clamp(limit, 1, 200), cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<CustomerActivityDto>>(
            activities.Select(CustomerAccountMapper.MapActivity).ToList());
    }

    public async Task LogAsync(
        int customerId,
        int? storeId,
        CustomerActivityType activityType,
        string summary,
        string? detailsJson = null,
        CancellationToken cancellationToken = default)
    {
        var activity = CustomerActivityLog.Create(customerId, storeId, activityType, summary, detailsJson);
        await repository.AddAsync(activity, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class CustomerAccountStorefrontService(
    ICurrentCustomerContext customerContext,
    IStoreContext storeContext,
    ICustomerPreferenceService preferenceService,
    ILoyaltyService loyaltyService,
    IStoreCreditService storeCreditService,
    ICustomerActivityService activityService,
    ILoyaltyRepository loyaltyRepository) : ICustomerAccountStorefrontService
{
    public async Task<Result<CustomerAccountOverviewDto>> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        if (!customerContext.IsAuthenticated || !customerContext.CustomerId.HasValue)
        {
            return Result.Failure<CustomerAccountOverviewDto>(Error.Validation("Customer authentication is required."));
        }

        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<CustomerAccountOverviewDto>(Error.Validation("Store context is required."));
        }

        var customerId = customerContext.CustomerId.Value;
        var preferences = await preferenceService.ListAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        var loyalty = await loyaltyService.GetAccountAsync(customerId, storeId.Value, cancellationToken).ConfigureAwait(false);
        var storeCredit = await storeCreditService
            .GetAccountAsync(customerId, storeId.Value, storeContext.CurrentCurrencyCode ?? "USD", cancellationToken)
            .ConfigureAwait(false);
        var activity = await activityService.ListAsync(customerId, storeId, 20, cancellationToken).ConfigureAwait(false);

        return Result.Success(new CustomerAccountOverviewDto(
            preferences.IsSuccess ? preferences.Value!.ToArray() : [],
            loyalty.IsSuccess ? loyalty.Value : null,
            storeCredit.IsSuccess ? storeCredit.Value : null,
            activity.IsSuccess ? activity.Value!.ToArray() : []));
    }

    public async Task<Result<IReadOnlyList<LoyaltyRewardDto>>> ListAvailableRewardsAsync(
        CancellationToken cancellationToken = default)
    {
        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<IReadOnlyList<LoyaltyRewardDto>>(Error.Validation("Store context is required."));
        }

        var rewards = await loyaltyRepository.ListRewardsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<LoyaltyRewardDto>>(
            rewards.Where(x => x.IsActive).Select(CustomerAccountMapper.MapReward).ToList());
    }
}
