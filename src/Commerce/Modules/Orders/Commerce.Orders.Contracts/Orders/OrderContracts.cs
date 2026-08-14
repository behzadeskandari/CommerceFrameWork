using Commerce.Framework.Core.Results;
using Commerce.Orders.Domain.Enums;

namespace Commerce.Orders.Contracts.Orders;

public sealed record OrderAddressDto(
    string FirstName,
    string LastName,
    string Country,
    string? StateProvince,
    string City,
    string Address1,
    string? Address2,
    string PostalCode,
    string? PhoneNumber);

public sealed record OrderTaxLineDto(
    int Id,
    string Name,
    decimal? RatePercentage,
    decimal TaxableAmount,
    decimal TaxAmount,
    string CurrencyCode,
    bool IsShippingTax,
    int? TaxCategoryId,
    string? TaxCategoryName);

public sealed record OrderItemDto(
    int Id,
    int OfferId,
    int ProductId,
    int? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineSubtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal LineTotal,
    string CurrencyCode,
    string? PrimaryImageUrl,
    string? PrimaryImageThumbnailUrl);

public sealed record OrderTotalsDto(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    string CurrencyCode);

public sealed record OrderCustomerDto(
    int? CustomerId,
    string? Email,
    string? DisplayName,
    bool IsGuest);

public sealed record OrderStatusHistoryDto(
    int Id,
    OrderStatusHistoryType HistoryType,
    string? FromStatus,
    string ToStatus,
    string Reason,
    string? Actor,
    DateTime CreatedAtUtc);

public sealed record OrderSummaryDto(
    int Id,
    string OrderNumber,
    int StoreId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    FulfillmentStatus FulfillmentStatus,
    decimal GrandTotal,
    string CurrencyCode,
    string? CustomerEmail,
    string? CustomerDisplayName,
    int? CustomerId,
    DateTime CreatedAtUtc);

public sealed record OrderDetailDto(
    int Id,
    string OrderNumber,
    int StoreId,
    int CheckoutId,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    FulfillmentStatus FulfillmentStatus,
    OrderCustomerDto Customer,
    OrderTotalsDto Totals,
    bool RequiresShipping,
    OrderAddressDto? BillingAddress,
    OrderAddressDto? ShippingAddress,
    string? SelectedShippingMethodId,
    string? SelectedShippingProviderSystemName,
    string? SelectedPaymentMethodId,
    string? SelectedPaymentMethodSystemName,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderTaxLineDto> TaxLines,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateOrderRequest(int CheckoutId);

public sealed record CreateOrderResultDto(
    int Id,
    string OrderNumber,
    string? GuestAccessToken);

public sealed record CancelOrderRequest(string? Reason);

public sealed record OrderListQuery(
    int Page = 1,
    int PageSize = 20,
    string? OrderNumber = null,
    string? Email = null,
    int? CustomerId = null,
    int? StoreId = null,
    OrderStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null);

public sealed record PagedOrderSummaryResult(
    IReadOnlyList<OrderSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public interface IOrderService
{
    Task<Result<CreateOrderResultDto>> CreateFromCheckoutAsync(
        CreateOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> GetByOrderNumberAsync(
        string orderNumber,
        string? guestAccessToken,
        CancellationToken cancellationToken = default);

    Task<Result<PagedOrderSummaryResult>> ListCustomerOrdersAsync(
        OrderListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> CancelAsync(
        int orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken = default);
}

public interface IOrderFulfillmentSync
{
    Task<Result> SyncFulfillmentAsync(int orderId, CancellationToken cancellationToken = default);
}

public interface IOrderFulfillmentUpdater
{
    Task<Result> UpdateFulfillmentStatusAsync(
        int orderId,
        FulfillmentStatus status,
        string reason,
        CancellationToken cancellationToken = default);
}

public interface IAdminOrderService
{
    Task<Result<PagedOrderSummaryResult>> ListAsync(
        OrderListQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);

    Task<Result<OrderDetailDto>> CancelAsync(
        int orderId,
        CancelOrderRequest request,
        CancellationToken cancellationToken = default);
}
