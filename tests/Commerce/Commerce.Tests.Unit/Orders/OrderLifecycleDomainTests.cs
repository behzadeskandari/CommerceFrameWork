using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Orders.Domain.ValueObjects;
using Commerce.Payments.Domain.Entities;
using Xunit;

namespace Commerce.Tests.Unit.Orders;

public sealed class OrderLifecycleDomainTests
{
    [Fact]
    public void Confirm_FromPending_SetsConfirmedStatus()
    {
        var order = CreateSampleOrder();

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void MarkProcessing_FromConfirmed_SetsProcessingStatus()
    {
        var order = CreateSampleOrder();
        order.Confirm();

        order.MarkProcessing();

        Assert.Equal(OrderStatus.Processing, order.Status);
    }

    [Fact]
    public void Complete_SetsCompletedStatus()
    {
        var order = CreateSampleOrder();
        order.Confirm();
        order.MarkProcessing();

        order.Complete();

        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void CancelPartial_OneOfTwoItems_SetsPartiallyCancelled()
    {
        var order = CreateSampleOrder(quantity: 2);
        SetItemId(order.Items.First(), 101);

        order.CancelPartial([(101, 1)], "Partial cancel.");

        Assert.Equal(OrderStatus.PartiallyCancelled, order.Status);
        Assert.Equal(1, order.Items.First().CancelledQuantity);
        Assert.Equal(1, order.Items.First().ActiveQuantity);
    }

    [Fact]
    public void CancelPartial_AllItems_SetsCancelledStatus()
    {
        var order = CreateSampleOrder(quantity: 2);
        SetItemId(order.Items.First(), 101);

        order.CancelPartial([(101, 2)], "Full line cancel.");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(FulfillmentStatus.Cancelled, order.FulfillmentStatus);
    }

    [Fact]
    public void CalculateRefundAmount_UsesLineTotalProportionally()
    {
        var order = CreateSampleOrder(quantity: 2, lineTotal: 100m);
        SetItemId(order.Items.First(), 101);

        var amount = order.CalculateRefundAmount([(101, 1)]);

        Assert.Equal(50m, amount);
    }

    [Fact]
    public void ReturnCase_ApproveAndCompleteWorkflow()
    {
        var returnCase = ReturnCase.Create(
            1,
            1,
            null,
            ReturnResolutionType.Refund,
            "Damaged item",
            "USD",
            null,
            [ReturnCaseItem.Create(10, 20, 30, 1)]);

        returnCase.Approve("Approved by admin.");
        returnCase.SetReturnShipment("TRACK-123");
        returnCase.MarkReceived();
        returnCase.MarkRestocked();
        returnCase.RecordRefund(25m, 99);
        returnCase.Complete();

        Assert.Equal(ReturnStatus.Completed, returnCase.Status);
        Assert.Equal(25m, returnCase.RefundAmount);
        Assert.Equal(99, returnCase.RefundId);
    }

    [Fact]
    public void ReturnCase_Reject_FromRequested()
    {
        var returnCase = ReturnCase.Create(
            1,
            1,
            null,
            ReturnResolutionType.Refund,
            "Wrong size",
            "USD",
            null,
            [ReturnCaseItem.Create(10, 20, 30, 1)]);

        returnCase.Reject("Outside return window.");

        Assert.Equal(ReturnStatus.Rejected, returnCase.Status);
    }

    [Fact]
    public void Refund_IdempotencyKey_IsStored()
    {
        var payment = Payment.Create(1, 10, null, "USD", 100m, "Payment.Manual", "pay-key");
        payment.MarkCaptured();

        var refund = payment.ApplyRefund(40m, "USD", "Partial return", "refund-key-1");

        Assert.Equal("refund-key-1", refund.IdempotencyKey);
        Assert.Equal(40m, payment.RefundedAmount);
    }

    private static Order CreateSampleOrder(int quantity = 1, decimal lineTotal = 100m)
    {
        var unitPrice = lineTotal / quantity;
        return Order.CreateFromCheckout(
            "ORD-2026-000200",
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
            lineTotal,
            0m,
            0m,
            0m,
            lineTotal,
            [OrderItem.Create(0, 1, 1, 10, 5, null, "Product", null, "SKU", quantity, unitPrice, lineTotal, 0m, 0m, lineTotal, "USD")]);
    }

    private static void SetItemId(OrderItem item, int id) =>
        typeof(OrderItem).GetProperty(nameof(OrderItem.Id))!.SetValue(item, id);
}
