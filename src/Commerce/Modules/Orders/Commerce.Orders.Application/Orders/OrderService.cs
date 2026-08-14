using Commerce.Cart.Contracts.Carts;
using Commerce.Checkout.Contracts.Checkout;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Application.Observability;
using Commerce.Framework.Contracts.Audit;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Results;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Payments.Contracts.Payments;
using Commerce.Pricing.Contracts.Pricing;
using Commerce.Promotions.Contracts.Usage;
using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Orders.Domain.ValueObjects;
using Commerce.Shipping.Contracts.Shipments;
using Microsoft.Extensions.Logging;

namespace Commerce.Orders.Application.Orders;

public sealed class OrderService(
    ICheckoutOrderPreparationService checkoutOrderPreparationService,
    IOrderRepository orderRepository,
    IOrderCreationIdempotencyRepository idempotencyRepository,
    IOrderCreationTransaction orderCreationTransaction,
    IOrderNumberGenerator orderNumberGenerator,
    IOrderAccessTokenGenerator accessTokenGenerator,
    ICurrentCustomerContext currentCustomerContext,
    IGuestCartContext guestCartContext,
    ICustomerReader customerReader,
    IInventoryOrderService inventoryOrderService,
    ICouponUsageService couponUsageService,
    IPromotionUsageService promotionUsageService,
    IStoreContext storeContext,
    IPaymentService? paymentService,
    IEnumerable<IOrderCreatedHandler> orderCreatedHandlers,
    IEnumerable<IOrderCancelledHandler> orderCancelledHandlers,
    IShipmentAdminService shipmentAdminService,
    IAuditPublisher auditPublisher,
    ICorrelationContext correlationContext,
    ILogger<OrderService> logger) : IOrderService, IAdminOrderService
{
    public async Task<Result<CreateOrderResultDto>> CreateFromCheckoutAsync(
        CreateOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Failure<CreateOrderResultDto>(OrderErrors.IdempotencyKeyRequired());
        }

        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<CreateOrderResultDto>(OrderErrors.StoreMismatch());
        }

        var normalizedKey = idempotencyKey.Trim();
        var existingIdempotency = await idempotencyRepository
            .GetByKeyAsync(storeId.Value, normalizedKey, cancellationToken)
            .ConfigureAwait(false);

        if (existingIdempotency is not null)
        {
            var existingOrder = await orderRepository
                .GetByIdWithDetailsAsync(existingIdempotency.OrderId, cancellationToken)
                .ConfigureAwait(false);

            if (existingOrder is not null)
            {
                return Result.Success(new CreateOrderResultDto(
                    existingOrder.Id,
                    existingOrder.OrderNumber,
                    existingOrder.GuestAccessToken));
            }
        }

        var existingByCheckout = await orderRepository
            .GetByCheckoutIdAsync(request.CheckoutId, cancellationToken)
            .ConfigureAwait(false);

        if (existingByCheckout is not null)
        {
            return Result.Failure<CreateOrderResultDto>(
                OrderErrors.OrderAlreadyCreated(existingByCheckout.Id));
        }

        var preparation = await checkoutOrderPreparationService
            .ValidateForOrderCreationAsync(request.CheckoutId, cancellationToken)
            .ConfigureAwait(false);

        if (!preparation.IsSuccess)
        {
            return Result.Failure<CreateOrderResultDto>(MapPreparationError(preparation.Error!));
        }

        var prep = preparation.Value!;
        if (prep.StoreId != storeId.Value)
        {
            return Result.Failure<CreateOrderResultDto>(OrderErrors.StoreMismatch());
        }

        if (!IsCheckoutOwnedByCurrentContext(prep))
        {
            return Result.Failure<CreateOrderResultDto>(OrderErrors.CheckoutOwnershipViolation());
        }

        var customerEmail = prep.GuestEmail;
        string? customerDisplayName = null;

        if (prep.CustomerId.HasValue)
        {
            var customerResult = await customerReader
                .GetByIdAsync(prep.CustomerId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (customerResult.IsSuccess)
            {
                customerEmail = customerResult.Value!.Email;
                customerDisplayName = $"{customerResult.Value.FirstName} {customerResult.Value.LastName}".Trim();
            }
        }

        var guestAccessToken = prep.CustomerId.HasValue ? null : accessTokenGenerator.GenerateToken();
        var orderNumber = await orderNumberGenerator.GenerateAsync(storeId.Value, cancellationToken).ConfigureAwait(false);

        var order = Order.CreateFromCheckout(
            orderNumber,
            prep.StoreId,
            prep.CheckoutId,
            prep.CartId,
            prep.CustomerId,
            prep.GuestEmail,
            customerEmail,
            customerDisplayName,
            guestAccessToken,
            prep.CurrencyId,
            prep.CurrencyCode,
            prep.RequiresShipping,
            MapAddress(prep.BillingAddress),
            MapAddress(prep.ShippingAddress),
            prep.SelectedShippingMethodId,
            prep.SelectedShippingProviderSystemName,
            prep.SelectedPaymentMethodId,
            prep.SelectedPaymentMethodSystemName,
            prep.Totals.Subtotal,
            prep.Totals.DiscountTotal,
            prep.Totals.ShippingTotal,
            prep.Totals.TaxTotal,
            prep.Totals.GrandTotal,
            prep.Items.Select(line => OrderItem.Create(
                0,
                prep.CheckoutId,
                line.CartItemId,
                line.OfferId,
                line.ProductId,
                line.VariantId,
                line.ProductName,
                line.VariantName,
                line.Sku,
                line.Quantity,
                line.UnitPrice,
                line.LineSubtotal,
                line.LineDiscount,
                line.LineTax,
                line.LineTotal,
                line.CurrencyCode,
                line.PrimaryImageUrl,
                line.PrimaryImageThumbnailUrl)),
            prep.OrderTaxLines.Select(x => OrderTaxLine.Create(
                0,
                x.Name,
                x.RatePercentage,
                x.TaxableAmount,
                x.Amount,
                prep.Totals.Currency,
                x.IsShippingTax,
                null,
                null)),
            prep.Totals.StoreCreditApplied,
            prep.Totals.GiftCardApplied,
            prep.AppliedGiftCardCode,
            prep.ReferralCode,
            prep.AffiliateId);

        var transactionResult = await orderCreationTransaction.ExecuteAsync(
            new OrderCreationTransactionRequest(order, prep.CheckoutId, prep.CartId, storeId.Value, normalizedKey),
            cancellationToken).ConfigureAwait(false);

        if (!transactionResult.Success)
        {
            if (transactionResult.ExistingOrderId.HasValue)
            {
                var conflictOrder = await orderRepository
                    .GetByIdWithDetailsAsync(transactionResult.ExistingOrderId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (conflictOrder is not null)
                {
                    return Result.Success(new CreateOrderResultDto(
                        conflictOrder.Id,
                        conflictOrder.OrderNumber,
                        conflictOrder.GuestAccessToken));
                }
            }

            return Result.Failure<CreateOrderResultDto>(
                OrderErrors.OrderCreationConflict(transactionResult.ErrorMessage ?? "Order creation failed."));
        }

        using (CommerceLogging.BeginOperationScope(
            logger,
            correlationContext,
            "order.created",
            ("OrderNumber", order.OrderNumber),
            ("CheckoutId", prep.CheckoutId)))
        {
            CommerceMetrics.OrderOperations.Add(1, new KeyValuePair<string, object?>("operation", "created"));
            logger.LogInformation(
                "Order {OrderNumber} created from checkout {CheckoutId}",
                order.OrderNumber,
                prep.CheckoutId);
        }

        foreach (var handler in orderCreatedHandlers)
        {
            await handler.HandleOrderCreatedAsync(transactionResult.OrderId!.Value, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(prep.AppliedCouponCode))
        {
            var couponResult = await couponUsageService.TryConsumeAsync(
                new CouponUsageRequest(
                    prep.AppliedCouponCode,
                    prep.StoreId,
                    transactionResult.OrderId!.Value,
                    prep.CustomerId),
                cancellationToken).ConfigureAwait(false);

            if (!couponResult.Success)
            {
                logger.LogWarning(
                    "Coupon {CouponCode} could not be consumed for order {OrderId}: {Error}",
                    prep.AppliedCouponCode,
                    transactionResult.OrderId,
                    couponResult.ErrorMessage);
            }
        }

        if (!string.IsNullOrWhiteSpace(prep.AppliedCouponCode))
        {
            await promotionUsageService.RecordOrderUsageAsync(
                new PromotionOrderUsageRequest(
                    prep.StoreId,
                    transactionResult.OrderId!.Value,
                    prep.CustomerId,
                    prep.AppliedCouponCode,
                    DateTime.UtcNow),
                cancellationToken).ConfigureAwait(false);
        }

        if (paymentService is not null && prep.Totals.GrandTotal == 0)
        {
            var paymentResult = await paymentService.CreateForOrderAsync(
                new CreatePaymentForOrderRequest(
                    transactionResult.OrderId!.Value,
                    PaymentMethodSystemName: PaymentProviderNames.FreeMethod),
                $"{normalizedKey}-payment",
                cancellationToken).ConfigureAwait(false);

            if (!paymentResult.IsSuccess)
            {
                logger.LogWarning(
                    "Free-order payment auto-create failed for order {OrderId}: {Error}",
                    transactionResult.OrderId,
                    paymentResult.Error?.Message);
            }
        }

        return Result.Success(new CreateOrderResultDto(
            transactionResult.OrderId!.Value,
            order.OrderNumber,
            guestAccessToken));
    }

    public async Task<Result<OrderDetailDto>> GetByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        if (!CanAccessOrder(order))
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderAccessDenied());
        }

        return Result.Success(OrderMapper.ToDetail(order));
    }

    public async Task<Result<OrderDetailDto>> GetByOrderNumberAsync(
        string orderNumber,
        string? guestAccessToken,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFoundByNumber(orderNumber));
        }

        if (order.CustomerId.HasValue)
        {
            if (!currentCustomerContext.IsAuthenticated ||
                !order.IsOwnedByCustomer(currentCustomerContext.CustomerId!.Value))
            {
                return Result.Failure<OrderDetailDto>(OrderErrors.OrderAccessDenied());
            }
        }
        else if (!order.IsAccessibleByGuest(guestAccessToken ?? string.Empty))
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderAccessDenied());
        }

        return Result.Success(OrderMapper.ToDetail(order));
    }

    public async Task<Result<PagedOrderSummaryResult>> ListCustomerOrdersAsync(
        OrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!currentCustomerContext.IsAuthenticated || !currentCustomerContext.CustomerId.HasValue)
        {
            return Result.Failure<PagedOrderSummaryResult>(OrderErrors.OrderAccessDenied());
        }

        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<PagedOrderSummaryResult>(OrderErrors.StoreMismatch());
        }

        var criteria = new OrderListCriteria(
            Math.Max(1, query.Page),
            Math.Clamp(query.PageSize, 1, 100),
            storeId.Value,
            currentCustomerContext.CustomerId.Value,
            query.OrderNumber,
            null,
            query.Status,
            query.CreatedFromUtc,
            query.CreatedToUtc);

        var (items, total) = await orderRepository.ListAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedOrderSummaryResult(
            items.Select(OrderMapper.ToSummary).ToList(),
            criteria.Page,
            criteria.PageSize,
            total));
    }

    public async Task<Result<OrderDetailDto>> CancelAsync(
        int orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        if (!CanAccessOrder(order))
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderAccessDenied());
        }

        return await CancelOrderInternalAsync(order, request.Reason ?? "Cancelled by customer.", null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<PagedOrderSummaryResult>> ListAsync(
        OrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        var criteria = new OrderListCriteria(
            Math.Max(1, query.Page),
            Math.Clamp(query.PageSize, 1, 100),
            query.StoreId,
            query.CustomerId,
            query.OrderNumber,
            query.Email,
            query.Status,
            query.CreatedFromUtc,
            query.CreatedToUtc);

        var (items, total) = await orderRepository.ListAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(new PagedOrderSummaryResult(
            items.Select(OrderMapper.ToSummary).ToList(),
            criteria.Page,
            criteria.PageSize,
            total));
    }

    Task<Result<OrderDetailDto>> IAdminOrderService.GetByIdAsync(int orderId, CancellationToken cancellationToken) =>
        GetAdminOrderByIdAsync(orderId, cancellationToken);

    public async Task<Result<OrderDetailDto>> CancelAdminAsync(
        int orderId,
        CancelOrderRequest request,
        string? actor,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId));
        }

        return await CancelOrderInternalAsync(
            order,
            request.Reason ?? "Cancelled by administrator.",
            actor,
            cancellationToken).ConfigureAwait(false) switch
        {
            { IsSuccess: true } success => await PublishOrderCancelledAuditAsync(order, actor, success, cancellationToken).ConfigureAwait(false),
            var failure => failure
        };
    }

    private async Task<Result<OrderDetailDto>> PublishOrderCancelledAuditAsync(
        Order order,
        string? actor,
        Result<OrderDetailDto> result,
        CancellationToken cancellationToken)
    {
        await auditPublisher.PublishAsync(new AuditPublishRequest(
            AuditCategory.Order,
            AuditActions.OrderCancelled,
            Success: true,
            EntityType: nameof(Order),
            EntityId: order.Id.ToString(),
            StoreId: order.StoreId,
            ActorType: AuditActorType.Administrator,
            ActorDisplay: actor,
            Details: new Dictionary<string, string?>
            {
                ["orderNumber"] = order.OrderNumber
            }), cancellationToken).ConfigureAwait(false);
        return result;
    }

    async Task<Result<OrderDetailDto>> IAdminOrderService.CancelAsync(
        int orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken) =>
        await CancelAdminAsync(orderId, request, "admin", cancellationToken).ConfigureAwait(false);

    private async Task<Result<OrderDetailDto>> GetAdminOrderByIdAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        return order is null
            ? Result.Failure<OrderDetailDto>(OrderErrors.OrderNotFound(orderId))
            : Result.Success(OrderMapper.ToDetail(order));
    }

    private async Task<Result<OrderDetailDto>> CancelOrderInternalAsync(
        Order order,
        string reason,
        string? actor,
        CancellationToken cancellationToken)
    {
        try
        {
            order.Cancel(reason, actor);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDetailDto>(OrderErrors.InvalidOrderState(ex.Message));
        }

        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);

        var isPaid = order.PaymentStatus is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded;
        if (isPaid)
        {
            var restockLines = order.Items
                .Where(x => x.ActiveQuantity > 0)
                .Select(x => new InventoryOrderLineAdjustment(x.OfferId, x.ActiveQuantity))
                .ToList();

            if (restockLines.Count > 0)
            {
                await inventoryOrderService
                    .RestockForOrderAsync(order.Id, order.StoreId, restockLines, reason, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        else
        {
            var releaseResult = await inventoryOrderService
                .ReleaseForOrderAsync(order.Id, order.StoreId, cancellationToken)
                .ConfigureAwait(false);

            if (!releaseResult.IsSuccess)
            {
                logger.LogWarning(
                    "Order {OrderNumber} cancelled but inventory release failed: {Error}",
                    order.OrderNumber,
                    releaseResult.Error!.Message);
            }
        }

        await shipmentAdminService
            .CancelOpenShipmentsForOrderAsync(order.Id, reason, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Order {OrderNumber} cancelled.", order.OrderNumber);

        foreach (var handler in orderCancelledHandlers)
        {
            await handler.HandleOrderCancelledAsync(order.Id, reason, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(OrderMapper.ToDetail(order));
    }

    private bool CanAccessOrder(Order order)
    {
        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue || order.StoreId != storeId.Value)
        {
            return false;
        }

        if (order.CustomerId.HasValue)
        {
            return currentCustomerContext.IsAuthenticated &&
                   order.IsOwnedByCustomer(currentCustomerContext.CustomerId!.Value);
        }

        return false;
    }

    private bool IsCheckoutOwnedByCurrentContext(OrderPreparationResult prep)
    {
        if (prep.CustomerId.HasValue)
        {
            return currentCustomerContext.IsAuthenticated &&
                   currentCustomerContext.CustomerId == prep.CustomerId.Value;
        }

        var guestToken = guestCartContext.GetGuestToken();
        return !string.IsNullOrWhiteSpace(guestToken);
    }

    private static OrderAddressSnapshot? MapAddress(CheckoutAddressDto? address) =>
        address is null
            ? null
            : OrderAddressSnapshot.Create(
                address.FirstName,
                address.LastName,
                address.Country,
                address.City,
                address.Address1,
                address.PostalCode,
                address.StateProvince,
                address.Address2,
                address.PhoneNumber);

    private static Commerce.Framework.Core.Errors.Error MapPreparationError(Commerce.Framework.Core.Errors.Error error)
    {
        var message = error.Message ?? string.Empty;
        if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return OrderErrors.CheckoutNotFound(0);
        }

        if (message.Contains("expired", StringComparison.OrdinalIgnoreCase))
        {
            return OrderErrors.CheckoutExpired();
        }

        if (message.Contains("ready", StringComparison.OrdinalIgnoreCase))
        {
            return OrderErrors.CheckoutNotReady(message);
        }

        return OrderErrors.CheckoutNotReady(message);
    }
}
