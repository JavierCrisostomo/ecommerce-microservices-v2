using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using Moq;
using Orders.Application.Abstractions;
using Orders.Application.Orders.Commands.ConfirmOrder;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Tests.Application.Orders.Commands.ConfirmOrder;

public class ConfirmOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderReadStore> _orderReadStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly ConfirmOrderCommandHandler _handler;

    public ConfirmOrderCommandHandlerTests()
    {
        _handler = new ConfirmOrderCommandHandler(_orderRepository.Object, _orderReadStore.Object, _unitOfWork.Object, _eventPublisher.Object);
    }

    private static Order CreatePendingOrder(Guid customerId)
        => Order.Create(customerId, [new NewOrderLine(Guid.NewGuid(), "Producto", 10m, 1)]);

    [Fact]
    public async Task Handle_WhenOrderExists_ConfirmsPersistsAndPublishes()
    {
        var customerId = Guid.NewGuid();
        var order = CreatePendingOrder(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await _handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Confirmed);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<OrderConfirmed>(e => e.OrderId == order.Id && e.CustomerId == customerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_DoesNothing()
    {
        var orderId = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        await _handler.Handle(new ConfirmOrderCommand(orderId), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderConfirmed>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
