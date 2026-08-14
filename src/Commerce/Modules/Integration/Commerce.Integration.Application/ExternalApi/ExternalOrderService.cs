using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Integration.Contracts.ExternalApi;
using Commerce.Orders.Contracts.Orders;

namespace Commerce.Integration.Application.ExternalApi;

public sealed class ExternalOrderService(IAdminOrderService adminOrderService) : IExternalOrderService
{
    public async Task<Result<ExternalPagedOrderResult>> ListOrdersAsync(
        int? storeId,
        ExternalOrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        var listQuery = new OrderListQuery(
            query.Page,
            query.PageSize,
            StoreId: storeId);

        var result = await adminOrderService.ListAsync(listQuery, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return Result.Failure<ExternalPagedOrderResult>(result.Error!);
        }

        var page = result.Value!;
        return Result.Success(new ExternalPagedOrderResult(
            page.Items.Select(MapSummary).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount));
    }

    public async Task<Result<ExternalOrderDetailDto>> GetOrderAsync(
        int orderId,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        var result = await adminOrderService.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return Result.Failure<ExternalOrderDetailDto>(result.Error!);
        }

        var order = result.Value!;
        if (storeId.HasValue && order.StoreId != storeId.Value)
        {
            return Result.Failure<ExternalOrderDetailDto>(Error.NotFound("Order not found."));
        }

        return Result.Success(MapDetail(order));
    }

    private static ExternalOrderSummaryDto MapSummary(OrderSummaryDto order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            order.FulfillmentStatus.ToString(),
            order.GrandTotal,
            order.CurrencyCode,
            order.CustomerId,
            order.CreatedAtUtc);

    private static ExternalOrderDetailDto MapDetail(OrderDetailDto order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.StoreId,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            order.FulfillmentStatus.ToString(),
            order.Totals.Subtotal,
            order.Totals.DiscountTotal,
            order.Totals.ShippingTotal,
            order.Totals.TaxTotal,
            order.Totals.GrandTotal,
            order.Totals.CurrencyCode,
            order.Customer.CustomerId,
            order.Customer.Email,
            order.Items.Select(item => new ExternalOrderLineDto(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.Sku,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.CurrencyCode)).ToList(),
            order.CreatedAtUtc,
            order.UpdatedAtUtc);
}
