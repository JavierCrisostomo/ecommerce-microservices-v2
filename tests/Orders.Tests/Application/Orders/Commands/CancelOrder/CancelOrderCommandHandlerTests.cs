using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using Moq;
using Orders.Application.Abstractions;
using Orders.Application.Orders.Commands.CancelOrder;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Tests.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderReadStore> _orderReadStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _handler = new CancelOrderCommandHandler(_orderRepository.Object, _orderReadStore.Object, _unitOfWork.Object, _eventPublisher.Object);
    }

    private static Order CreatePendingOrder(Guid customerId)
        => Order.Create(customerId, [new NewOrderLine(Guid.NewGuid(), "Producto", 10m, 1)]);

    [Fact]
    public async Task Handle_WhenOrderExists_CancelsPersistsAndPublishes()
    {
        var customerId = Guid.NewGuid();
        var order = CreatePendingOrder(customerId);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await _handler.Handle(new CancelOrderCommand(order.Id, "Stock insuficiente"), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Stock insuficiente");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<OrderCancelled>(e => e.OrderId == order.Id && e.CustomerId == customerId && e.Reason == "Stock insuficiente"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_DoesNothing()
    {
        var orderId = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        await _handler.Handle(new CancelOrderCommand(orderId, "no importa"), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCancelled>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
