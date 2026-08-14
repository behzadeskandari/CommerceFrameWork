using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;

namespace Commerce.Orders.Application.Abstractions;

public interface IOrderRepository
{
    Task<Order?> GetByIdWithDetailsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    Task<Order?> GetByCheckoutIdAsync(int checkoutId, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task SaveAsync(Order order, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        OrderListCriteria criteria,
        CancellationToken cancellationToken = default);
}

public interface IReturnCaseRepository
{
    Task<ReturnCase?> GetByIdWithItemsAsync(int returnCaseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReturnCase>> ListByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task AddAsync(ReturnCase returnCase, CancellationToken cancellationToken = default);

    Task SaveAsync(ReturnCase returnCase, CancellationToken cancellationToken = default);
}

public sealed record OrderListCriteria(
    int Page,
    int PageSize,
    int? StoreId,
    int? CustomerId,
    string? OrderNumber,
    string? Email,
    OrderStatus? Status,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc);

public interface IOrderNumberSequenceRepository
{
    Task<StoreOrderNumberSequence> GetOrCreateAsync(int storeId, int year, CancellationToken cancellationToken = default);

    Task SaveAsync(StoreOrderNumberSequence sequence, CancellationToken cancellationToken = default);
}

public interface IOrderCreationIdempotencyRepository
{
    Task<OrderCreationIdempotency?> GetByKeyAsync(
        int storeId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrderCreationIdempotency record, CancellationToken cancellationToken = default);
}

public interface IOrderCreationTransaction
{
    Task<OrderCreationTransactionResult> ExecuteAsync(
        OrderCreationTransactionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record OrderCreationTransactionRequest(
    Order Order,
    int CheckoutId,
    int CartId,
    int StoreId,
    string IdempotencyKey);

public sealed record OrderCreationTransactionResult(
    bool Success,
    int? OrderId,
    int? ExistingOrderId,
    string? ErrorMessage,
    bool IsConflict);
