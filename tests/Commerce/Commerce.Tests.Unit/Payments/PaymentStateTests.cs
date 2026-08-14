using Commerce.Payments.Domain.Entities;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Orders.Domain.ValueObjects;
using PaymentEntityStatus = Commerce.Payments.Domain.Enums.PaymentStatus;
using PaymentRefundStatus = Commerce.Payments.Domain.Enums.RefundStatus;
using OrderPaymentStatus = Commerce.Orders.Domain.Enums.PaymentStatus;
using Xunit;

namespace Commerce.Tests.Unit.Payments;

public sealed class PaymentStateTests
{
    [Fact]
    public void Create_SetsPendingStatus()
    {
        var payment = CreateSamplePayment();

        Assert.Equal(PaymentEntityStatus.Pending, payment.Status);
        Assert.Equal(100m, payment.Amount);
        Assert.Equal(0m, payment.RefundedAmount);
    }

    [Fact]
    public void MarkCaptured_FromInitiated_Succeeds()
    {
        var payment = CreateSamplePayment();
        payment.MarkInitiated("provider-1");
        payment.MarkCaptured("provider-1");

        Assert.Equal(PaymentEntityStatus.Captured, payment.Status);
    }

    [Fact]
    public void ApplyRefund_Partial_SetsPartiallyRefunded()
    {
        var payment = CreateSamplePayment();
        payment.MarkCaptured();

        var refund = payment.ApplyRefund(40m, "USD", "Partial return");

        Assert.Equal(PaymentEntityStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(40m, payment.RefundedAmount);
        Assert.Equal(PaymentRefundStatus.Pending, refund.Status);
    }

    [Fact]
    public void ApplyRefund_Full_SetsRefunded()
    {
        var payment = CreateSamplePayment();
        payment.MarkCaptured();

        payment.ApplyRefund(100m, "USD");

        Assert.Equal(PaymentEntityStatus.Refunded, payment.Status);
        Assert.Equal(100m, payment.RefundedAmount);
    }

    [Fact]
    public void ApplyRefund_RejectsExcessAmount()
    {
        var payment = CreateSamplePayment();
        payment.MarkCaptured();

        Assert.Throws<InvalidOperationException>(() => payment.ApplyRefund(150m, "USD"));
    }

    [Fact]
    public void Order_ApplyPaymentAuthorized_UpdatesStatusAndHistory()
    {
        var order = CreateSampleOrder();

        order.ApplyPaymentAuthorized("Provider authorized payment.");

        Assert.Equal(OrderPaymentStatus.Authorized, order.PaymentStatus);
        Assert.Contains(order.StatusHistory, x =>
            x.HistoryType == OrderStatusHistoryType.Payment &&
            x.ToStatus == OrderPaymentStatus.Authorized.ToString());
    }

    [Fact]
    public void Order_MarkPaymentPaid_UpdatesStatusAndHistory()
    {
        var order = CreateSampleOrder();

        order.MarkPaymentPaid("Payment captured.");

        Assert.Equal(OrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.Contains(order.StatusHistory, x => x.ToStatus == OrderPaymentStatus.Paid.ToString());
    }

    [Fact]
    public void Order_ApplyPartialRefund_UpdatesStatusAndHistory()
    {
        var order = CreateSampleOrder();
        order.MarkPaymentPaid();

        order.ApplyPartialRefund("Partial refund issued.");

        Assert.Equal(OrderPaymentStatus.PartiallyRefunded, order.PaymentStatus);
    }

    private static Payment CreateSamplePayment() =>
        Payment.Create(1, 10, null, "USD", 100m, "Payment.Manual", "key-1");

    private static Order CreateSampleOrder() =>
        Order.CreateFromCheckout(
            "ORD-2026-000100",
            1,
            1,
            1,
            null,
            "guest@example.com",
            null,
            null,
            "guest-token",
            1,
            "USD",
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            100m,
            0m,
            0m,
            0m,
            100m,
            [OrderItem.Create(0, 1, 1, 10, 5, null, "Product", null, "SKU", 1, 100m, 100m, 0m, 0m, 100m, "USD")]);
}
