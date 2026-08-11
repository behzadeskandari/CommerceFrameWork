using Commerce.Cart.Domain.Entities;
using Commerce.Cart.Domain.Enums;
using Commerce.Checkout.Domain.Entities;
using Commerce.Checkout.Domain.Enums;
using Commerce.Framework.Data.Db;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Commerce.Orders.Infrastructure.Transactions;

public sealed class OrderCreationTransaction(
    CommerceDbContext dbContext,
    IInventoryOrderService inventoryOrderService,
    ILogger<OrderCreationTransaction> logger) : IOrderCreationTransaction
{
    public async Task<OrderCreationTransactionResult> ExecuteAsync(
        OrderCreationTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await ExecuteWithoutTransactionAsync(request, cancellationToken).ConfigureAwait(false);
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var result = await ExecuteCoreAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.Success)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return await HandleConflictAsync(request, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<OrderCreationTransactionResult> ExecuteWithoutTransactionAsync(
        OrderCreationTransactionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return ExecuteCoreAsync(request, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return HandleConflictAsync(request, ex, cancellationToken);
        }
    }

    private async Task<OrderCreationTransactionResult> ExecuteCoreAsync(
        OrderCreationTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var existingOrder = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CheckoutId == request.CheckoutId, cancellationToken)
            .ConfigureAwait(false);

        if (existingOrder is not null)
        {
            return new OrderCreationTransactionResult(
                true,
                existingOrder.Id,
                existingOrder.Id,
                null,
                false);
        }

        var checkout = await dbContext.Set<CheckoutSession>()
            .FirstOrDefaultAsync(x => x.Id == request.CheckoutId, cancellationToken)
            .ConfigureAwait(false);

        if (checkout is null)
        {
            return new OrderCreationTransactionResult(false, null, null, "Checkout not found.", false);
        }

        if (checkout.Status == CheckoutStatus.Completed)
        {
            var orderForCheckout = await dbContext.Set<Order>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CheckoutId == request.CheckoutId, cancellationToken)
                .ConfigureAwait(false);

            if (orderForCheckout is not null)
            {
                return new OrderCreationTransactionResult(
                    true,
                    orderForCheckout.Id,
                    orderForCheckout.Id,
                    null,
                    false);
            }
        }

        if (checkout.Status != CheckoutStatus.ReadyForOrder)
        {
            return new OrderCreationTransactionResult(
                false,
                null,
                null,
                $"Checkout is not ready for order (status: {checkout.Status}).",
                false);
        }

        var cart = await dbContext.Set<ShoppingCart>()
            .FirstOrDefaultAsync(x => x.Id == request.CartId, cancellationToken)
            .ConfigureAwait(false);

        if (cart is null)
        {
            return new OrderCreationTransactionResult(false, null, null, "Cart not found.", false);
        }

        dbContext.Set<Order>().Add(request.Order);
        checkout.MarkCompleted();
        if (cart.Status != CartStatus.Converted)
        {
            cart.MarkConverted();
        }

        var idempotency = OrderCreationIdempotency.CreatePending(
            request.StoreId,
            request.IdempotencyKey,
            request.CheckoutId);
        dbContext.Set<OrderCreationIdempotency>().Add(idempotency);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        idempotency.AssignOrderId(request.Order.Id);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var reservationResult = await inventoryOrderService.ReserveForOrderAsync(
            new InventoryOrderReservationRequest(
                request.Order.Id,
                request.StoreId,
                request.Order.Items
                    .Select(item => new InventoryOrderLineDto(
                        item.OfferId,
                        item.ProductId,
                        item.VariantId,
                        item.Quantity))
                    .ToList()),
            cancellationToken).ConfigureAwait(false);

        if (!reservationResult.Success)
        {
            return new OrderCreationTransactionResult(
                false,
                null,
                null,
                string.Join(' ', reservationResult.Errors),
                false);
        }

        logger.LogInformation(
            "Order {OrderId} created atomically for checkout {CheckoutId}",
            request.Order.Id,
            request.CheckoutId);

        return new OrderCreationTransactionResult(true, request.Order.Id, null, null, false);
    }

    private async Task<OrderCreationTransactionResult> HandleConflictAsync(
        OrderCreationTransactionRequest request,
        DbUpdateException ex,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(ex, "Order creation conflict for checkout {CheckoutId}", request.CheckoutId);

        var existing = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CheckoutId == request.CheckoutId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return new OrderCreationTransactionResult(
                true,
                existing.Id,
                existing.Id,
                null,
                true);
        }

        return new OrderCreationTransactionResult(
            false,
            null,
            null,
            "Order creation conflict.",
            true);
    }
}
