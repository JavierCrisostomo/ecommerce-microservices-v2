using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using Moq;
using Payments.Application.Abstractions;
using Payments.Application.Payments.Commands.ProcessPayment;
using Payments.Domain.Entities;
using Payments.Domain.Repositories;

namespace Payments.Tests.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly ProcessPaymentCommandHandler _handler;

    public ProcessPaymentCommandHandlerTests()
    {
        _handler = new ProcessPaymentCommandHandler(_paymentRepository.Object, _paymentGateway.Object, _unitOfWork.Object, _eventPublisher.Object);

        _paymentRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);
    }

    [Fact]
    public async Task Handle_WhenPaymentAlreadyExists_IsIdempotentAndDoesNothing()
    {
        var orderId = Guid.NewGuid();
        var existing = Payment.Create(orderId, 50m);
        _paymentRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _handler.Handle(new ProcessPaymentCommand(orderId, 50m), CancellationToken.None);

        _paymentGateway.Verify(g => g.Charge(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
        _paymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGatewayApproves_CompletesPaymentAndPublishesPaymentCompleted()
    {
        var orderId = Guid.NewGuid();
        _paymentGateway.Setup(g => g.Charge(orderId, 50m)).Returns(new PaymentGatewayResult(true, null));

        await _handler.Handle(new ProcessPaymentCommand(orderId, 50m), CancellationToken.None);

        _paymentRepository.Verify(r => r.AddAsync(
            It.Is<Payment>(p => p.OrderId == orderId && p.Status == PaymentStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<PaymentCompleted>(e => e.OrderId == orderId && e.Amount == 50m),
            It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<PaymentFailed>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGatewayRejects_FailsPaymentAndPublishesPaymentFailed()
    {
        var orderId = Guid.NewGuid();
        _paymentGateway.Setup(g => g.Charge(orderId, 1500m)).Returns(new PaymentGatewayResult(false, "Monto demasiado alto"));

        await _handler.Handle(new ProcessPaymentCommand(orderId, 1500m), CancellationToken.None);

        _paymentRepository.Verify(r => r.AddAsync(
            It.Is<Payment>(p => p.OrderId == orderId && p.Status == PaymentStatus.Failed && p.FailureReason == "Monto demasiado alto"),
            It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<PaymentFailed>(e => e.OrderId == orderId && e.Reason == "Monto demasiado alto"),
            It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<PaymentCompleted>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
