using FluentAssertions;
using Payments.Domain.Entities;

namespace Payments.Tests.Domain;

public class PaymentTests
{
    [Fact]
    public void Create_WithValidAmount_StartsPending()
    {
        var orderId = Guid.NewGuid();

        var payment = Payment.Create(orderId, 50m);

        payment.OrderId.Should().Be(orderId);
        payment.Amount.Should().Be(50m);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.FailureReason.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositiveAmount_Throws(decimal amount)
    {
        var act = () => Payment.Create(Guid.NewGuid(), amount);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Complete_FromPending_ReturnsTrueAndSetsCompleted()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);

        var result = payment.Complete();

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ReturnsFalse()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);
        payment.Complete();

        var result = payment.Complete();

        result.Should().BeFalse();
    }

    [Fact]
    public void Fail_FromPending_ReturnsTrueAndSetsFailedWithReason()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);

        var result = payment.Fail("Fondos insuficientes");

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailureReason.Should().Be("Fondos insuficientes");
    }

    [Fact]
    public void Fail_WhenAlreadyCompleted_ReturnsFalseAndKeepsCompleted()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);
        payment.Complete();

        var result = payment.Fail("demasiado tarde");

        result.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Completed);
    }

    [Fact]
    public void Cancel_FromPending_ReturnsTrueAndSetsCancelled()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);

        var result = payment.Cancel();

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyFailed_ReturnsFalse()
    {
        var payment = Payment.Create(Guid.NewGuid(), 50m);
        payment.Fail("rechazado");

        var result = payment.Cancel();

        result.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Failed);
    }
}
