using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using Moq;
using Orders.Application.Abstractions;
using Orders.Application.Orders;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Tests.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IOrderReadStore> _orderReadStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _handler = new CreateOrderCommandHandler(_orderRepository.Object, _orderReadStore.Object, _unitOfWork.Object, _eventPublisher.Object);
    }

    [Fact]
    public async Task Handle_PersistsOrderAndPublishesOrderCreatedWithCorrectData()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand(customerId, [new CreateOrderLineRequest(productId, "Producto", 10m, 3)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.TotalAmount.Should().Be(30m);
        result.Status.Should().Be(nameof(OrderStatus.Pending));

        _orderRepository.Verify(r => r.AddAsync(It.Is<Order>(o => o.CustomerId == customerId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderReadStore.Verify(s => s.UpsertAsync(It.Is<OrderSummary>(sum => sum.CustomerId == customerId && sum.TotalAmount == 30m), It.IsAny<CancellationToken>()), Times.Once);

        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<OrderCreated>(e =>
                e.CustomerId == customerId &&
                e.TotalAmount == 30m &&
                e.Lines.Single().ProductId == productId &&
                e.Lines.Single().Quantity == 3 &&
                e.Lines.Single().UnitPrice == 10m),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
