using FluentAssertions;
using Moq;
using Orders.Application.Abstractions;
using Orders.Application.Orders;
using Orders.Application.Orders.Queries.GetOrderById;

namespace Orders.Tests.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderReadStore> _orderReadStore = new();
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _handler = new GetOrderByIdQueryHandler(_orderReadStore.Object);
    }

    [Fact]
    public async Task Handle_WhenOrderExists_ReturnsSummary()
    {
        var orderId = Guid.NewGuid();
        var summary = new OrderSummary(orderId, Guid.NewGuid(), "Pending", 10m, [], DateTimeOffset.UtcNow);
        _orderReadStore.Setup(s => s.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        var result = await _handler.Handle(new GetOrderByIdQuery(orderId), CancellationToken.None);

        result.Should().Be(summary);
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ReturnsNull()
    {
        var orderId = Guid.NewGuid();
        _orderReadStore.Setup(s => s.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((OrderSummary?)null);

        var result = await _handler.Handle(new GetOrderByIdQuery(orderId), CancellationToken.None);

        result.Should().BeNull();
    }
}
