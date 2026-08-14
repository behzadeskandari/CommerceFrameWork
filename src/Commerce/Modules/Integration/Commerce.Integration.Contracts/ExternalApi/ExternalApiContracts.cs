using Commerce.Framework.Core.Results;

namespace Commerce.Integration.Contracts.ExternalApi;

public sealed record ExternalOrderSummaryDto(
    int Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string FulfillmentStatus,
    decimal GrandTotal,
    string CurrencyCode,
    int? CustomerId,
    DateTime CreatedAtUtc);

public sealed record ExternalOrderDetailDto(
    int Id,
    string OrderNumber,
    int StoreId,
    string Status,
    string PaymentStatus,
    string FulfillmentStatus,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string CurrencyCode,
    int? CustomerId,
    string? CustomerEmail,
    IReadOnlyList<ExternalOrderLineDto> Lines,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ExternalOrderLineDto(
    int Id,
    int ProductId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string CurrencyCode);

public sealed record ExternalOrderListQuery(int Page = 1, int PageSize = 20);

public sealed record ExternalPagedOrderResult(
    IReadOnlyList<ExternalOrderSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface IExternalOrderService
{
    Task<Result<ExternalPagedOrderResult>> ListOrdersAsync(
        int? storeId,
        ExternalOrderListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ExternalOrderDetailDto>> GetOrderAsync(
        int orderId,
        int? storeId,
        CancellationToken cancellationToken = default);
}
