using FluentAssertions;
using Moq;
using Payments.Application.Payments.Queries.GetPaymentByOrderId;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;

namespace Payments.Tests.Application.Payments.Queries.GetPaymentByOrderId;

public class GetPaymentByOrderIdQueryHandlerTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly GetPaymentByOrderIdQueryHandler _handler;

    public GetPaymentByOrderIdQueryHandlerTests()
    {
        _handler = new GetPaymentByOrderIdQueryHandler(_paymentRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenPaymentExists_ReturnsSummary()
    {
        var orderId = Guid.NewGuid();
        var payment = Payment.Create(orderId, 50m);
        payment.Complete();
        _paymentRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        var result = await _handler.Handle(new GetPaymentByOrderIdQuery(orderId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.Status.Should().Be(nameof(PaymentStatus.Completed));
        result.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_WhenPaymentDoesNotExist_ReturnsNull()
    {
        var orderId = Guid.NewGuid();
        _paymentRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Payment?)null);

        var result = await _handler.Handle(new GetPaymentByOrderIdQuery(orderId), CancellationToken.None);

        result.Should().BeNull();
    }
}
