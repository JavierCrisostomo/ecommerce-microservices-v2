using FluentAssertions;
using Inventory.Application.Inventory.Queries.GetStockByProductId;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Moq;

namespace Inventory.Tests.Application.Inventory.Queries.GetStockByProductId;

public class GetStockByProductIdQueryHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepository = new();
    private readonly GetStockByProductIdQueryHandler _handler;

    public GetStockByProductIdQueryHandlerTests()
    {
        _handler = new GetStockByProductIdQueryHandler(_inventoryItemRepository.Object);
    }

    [Fact]
    public async Task Handle_WhenItemExists_ReturnsStockSummary()
    {
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, 7);
        _inventoryItemRepository.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _handler.Handle(new GetStockByProductIdQuery(productId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.AvailableQuantity.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsNull()
    {
        var productId = Guid.NewGuid();
        _inventoryItemRepository.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItem?)null);

        var result = await _handler.Handle(new GetStockByProductIdQuery(productId), CancellationToken.None);

        result.Should().BeNull();
    }
}
