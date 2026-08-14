using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Entities;

namespace Commerce.Orders.Application.Orders;

internal static class OrderMapper
{
    public static OrderSummaryDto ToSummary(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.StoreId,
            order.Status,
            order.PaymentStatus,
            order.FulfillmentStatus,
            order.GrandTotal,
            order.CurrencyCode,
            order.CustomerEmail,
            order.CustomerDisplayName,
            order.CustomerId,
            order.CreatedAtUtc);

    public static OrderDetailDto ToDetail(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.StoreId,
            order.CheckoutId,
            order.Status,
            order.PaymentStatus,
            order.FulfillmentStatus,
            new OrderCustomerDto(
                order.CustomerId,
                order.CustomerEmail,
                order.CustomerDisplayName,
                !order.CustomerId.HasValue),
            new OrderTotalsDto(
                order.Subtotal,
                order.DiscountTotal,
                order.ShippingTotal,
                order.TaxTotal,
                order.GrandTotal,
                order.CurrencyCode),
            order.RequiresShipping,
            MapAddress(order.BillingAddress),
            MapAddress(order.ShippingAddress),
            order.SelectedShippingMethodId,
            order.SelectedShippingProviderSystemName,
            order.SelectedPaymentMethodId,
            order.SelectedPaymentMethodSystemName,
            order.Items.Select(ToItem).ToList(),
            order.TaxLines.Select(ToTaxLine).ToList(),
            order.StatusHistory.Select(ToHistory).ToList(),
            order.CreatedAtUtc,
            order.UpdatedAtUtc);

    private static OrderItemDto ToItem(OrderItem item) =>
        new(
            item.Id,
            item.OfferId,
            item.ProductId,
            item.VariantId,
            item.ProductName,
            item.VariantName,
            item.Sku,
            item.Quantity,
            item.UnitPrice,
            item.LineSubtotal,
            item.DiscountTotal,
            item.TaxTotal,
            item.LineTotal,
            item.CurrencyCode,
            item.PrimaryImageUrl,
            item.PrimaryImageThumbnailUrl);

    private static OrderTaxLineDto ToTaxLine(OrderTaxLine line) =>
        new(
            line.Id,
            line.Name,
            line.RatePercentage,
            line.TaxableAmount,
            line.TaxAmount,
            line.CurrencyCode,
            line.IsShippingTax,
            line.TaxCategoryId,
            line.TaxCategoryName);

    private static OrderStatusHistoryDto ToHistory(OrderStatusHistory history) =>
        new(
            history.Id,
            history.HistoryType,
            history.FromStatus,
            history.ToStatus,
            history.Reason,
            history.Actor,
            history.CreatedAtUtc);

    private static OrderAddressDto? MapAddress(Domain.ValueObjects.OrderAddressSnapshot? address) =>
        address is null
            ? null
            : new OrderAddressDto(
                address.FirstName,
                address.LastName,
                address.Country,
                address.StateProvince,
                address.City,
                address.Address1,
                address.Address2,
                address.PostalCode,
                address.PhoneNumber);
}
