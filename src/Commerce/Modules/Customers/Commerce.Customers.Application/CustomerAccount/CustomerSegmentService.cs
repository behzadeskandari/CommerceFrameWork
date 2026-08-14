using Commerce.Customers.Application.Abstractions;
using Commerce.Customers.Contracts.CustomerAccount;
using Commerce.Customers.Domain.Entities;
using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Customers.Application.CustomerAccount;

public sealed class CustomerSegmentAdminService(
    ICustomerSegmentRepository repository,
    ICustomerActivityService activityService) : ICustomerSegmentAdminService
{
    public async Task<Result<IReadOnlyList<CustomerSegmentSummaryDto>>> ListAsync(
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var segments = await repository.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<CustomerSegmentSummaryDto>>(
            segments.Select(CustomerAccountMapper.MapSegmentSummary).ToList());
    }

    public async Task<Result<CustomerSegmentDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var segment = await repository.GetByIdWithRulesAsync(id, cancellationToken).ConfigureAwait(false);
        return segment is null
            ? Result.Failure<CustomerSegmentDetailDto>(Error.NotFound($"Segment '{id}' was not found."))
            : Result.Success(CustomerAccountMapper.MapSegmentDetail(segment));
    }

    public async Task<Result<CustomerSegmentDetailDto>> CreateAsync(
        CreateCustomerSegmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var segment = CustomerSegment.Create(
                request.StoreId,
                request.Name,
                request.Description,
                request.Rules.Select(CustomerAccountMapper.CreateRule));

            await repository.AddAsync(segment, cancellationToken).ConfigureAwait(false);
            return Result.Success(CustomerAccountMapper.MapSegmentDetail(segment));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<CustomerSegmentDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result<CustomerSegmentDetailDto>> UpdateAsync(
        int id,
        UpdateCustomerSegmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var segment = await repository.GetByIdWithRulesAsync(id, cancellationToken).ConfigureAwait(false);
        if (segment is null)
        {
            return Result.Failure<CustomerSegmentDetailDto>(Error.NotFound($"Segment '{id}' was not found."));
        }

        try
        {
            segment.Update(request.Name, request.Description, request.IsActive);
            segment.ReplaceRules(request.Rules.Select(CustomerAccountMapper.CreateRule));
            await repository.UpdateAsync(segment, cancellationToken).ConfigureAwait(false);
            return Result.Success(CustomerAccountMapper.MapSegmentDetail(segment));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure<CustomerSegmentDetailDto>(Error.Validation(ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var segment = await repository.GetByIdWithRulesAsync(id, cancellationToken).ConfigureAwait(false);
        if (segment is null)
        {
            return Result.Failure(Error.NotFound($"Segment '{id}' was not found."));
        }

        await repository.DeleteAsync(segment, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<CustomerSegmentSummaryDto>>> EvaluateCustomerSegmentsAsync(
        int customerId,
        int storeId,
        int? customerGroupId,
        int orderCount,
        decimal lifetimeSpend,
        CancellationToken cancellationToken = default)
    {
        var segments = await repository.ListAsync(storeId, cancellationToken).ConfigureAwait(false);
        var matched = new List<CustomerSegment>();

        foreach (var segment in segments.Where(x => x.IsActive))
        {
            if (segment.Rules.All(rule => EvaluateRule(rule, customerGroupId, orderCount, lifetimeSpend)))
            {
                matched.Add(segment);
            }
        }

        await repository.RemoveMembershipsForCustomerAsync(customerId, storeId, cancellationToken).ConfigureAwait(false);
        var utcNow = DateTime.UtcNow;
        foreach (var segment in matched)
        {
            await repository.AddMembershipAsync(
                CustomerSegmentMembership.Create(segment.Id, customerId, storeId, utcNow),
                cancellationToken).ConfigureAwait(false);

            await activityService.LogAsync(
                customerId,
                storeId,
                CustomerActivityType.SegmentAssigned,
                $"Assigned to segment '{segment.Name}'.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return Result.Success<IReadOnlyList<CustomerSegmentSummaryDto>>(
            matched.Select(CustomerAccountMapper.MapSegmentSummary).ToList());
    }

    private static bool EvaluateRule(
        CustomerSegmentRule rule,
        int? customerGroupId,
        int orderCount,
        decimal lifetimeSpend) =>
        rule.RuleType switch
        {
            CustomerSegmentRuleType.CustomerGroup =>
                customerGroupId.HasValue && rule.CustomerGroupId == customerGroupId,
            CustomerSegmentRuleType.MinOrderCount =>
                rule.MinOrderCount.HasValue && orderCount >= rule.MinOrderCount.Value,
            CustomerSegmentRuleType.MinLifetimeSpend =>
                rule.MinLifetimeSpend.HasValue && lifetimeSpend >= rule.MinLifetimeSpend.Value,
            _ => false
        };
}

public sealed class CustomerAccountAdminService(
    ICustomerRepository customerRepository,
    ICustomerActivityService activityService,
    IAdminOrderService orderService,
    IStoreContext storeContext) : ICustomerAccountAdminService
{
    public async Task<Result> AssignCustomerGroupAsync(
        int customerId,
        AssignCustomerGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.Deleted)
        {
            return Result.Failure(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        customer.AssignCustomerGroup(request.CustomerGroupId);
        await customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
        await activityService.LogAsync(
            customerId,
            storeContext.CurrentStoreId,
            CustomerActivityType.GroupAssigned,
            request.CustomerGroupId.HasValue
                ? $"Assigned to customer group {request.CustomerGroupId.Value}."
                : "Customer group cleared.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    public async Task<Result> UpdateTaxProfileAsync(
        int customerId,
        UpdateCustomerTaxProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.Deleted)
        {
            return Result.Failure(Error.NotFound($"Customer '{customerId}' was not found."));
        }

        customer.UpdateTaxProfile(request.IsTaxExempt, request.TaxRegistrationNumber);
        await customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
        return Result.Success();
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
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<CustomerPurchaseHistoryItemDto>>> GetPurchaseHistoryAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<IReadOnlyList<CustomerPurchaseHistoryItemDto>>(Error.Validation("Store context is required."));
        }

        var result = await orderService
            .ListAsync(new OrderListQuery(Page: 1, PageSize: 50, CustomerId: customerId, StoreId: storeId), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result.Failure<IReadOnlyList<CustomerPurchaseHistoryItemDto>>(result.Error!);
        }

        var items = result.Value!.Items
            .Select(x => new CustomerPurchaseHistoryItemDto(
                x.Id,
                x.OrderNumber,
                x.GrandTotal,
                x.CurrencyCode,
                x.Status.ToString(),
                x.CreatedAtUtc))
            .ToList();

        return Result.Success<IReadOnlyList<CustomerPurchaseHistoryItemDto>>(items);
    }
}

public sealed class OrderPaidLoyaltyHandler(
    IServiceScopeFactory scopeFactory,
    ILoyaltyService loyaltyService,
    ICustomerActivityService activityService) : IOrderPaidHandler
{
    private const int DefaultPointsPerCurrencyUnit = 1;
    private static readonly TimeSpan DefaultPointExpiration = TimeSpan.FromDays(365);

    public async Task HandleOrderPaidAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var orderResult = await orderService.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (orderResult.IsFailure || orderResult.Value!.Customer.CustomerId is not int customerId)
        {
            return;
        }

        var order = orderResult.Value;
        var points = (int)Math.Floor(order.Totals.GrandTotal * DefaultPointsPerCurrencyUnit);
        if (points <= 0)
        {
            return;
        }

        await loyaltyService.EarnAsync(
            customerId,
            order.StoreId,
            points,
            $"order-paid-{orderId}",
            CustomerAccountReferenceType.Order,
            orderId,
            $"Points earned for order {order.OrderNumber}.",
            DateTime.UtcNow.Add(DefaultPointExpiration),
            cancellationToken).ConfigureAwait(false);

        await activityService.LogAsync(
            customerId,
            order.StoreId,
            CustomerActivityType.OrderPlaced,
            $"Order {order.OrderNumber} paid.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
