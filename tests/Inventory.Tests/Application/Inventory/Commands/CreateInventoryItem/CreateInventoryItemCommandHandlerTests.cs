using FluentAssertions;
using Inventory.Application.Abstractions;
using Inventory.Application.Exceptions;
using Inventory.Application.Inventory.Commands.CreateInventoryItem;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Moq;

namespace Inventory.Tests.Application.Inventory.Commands.CreateInventoryItem;

public class CreateInventoryItemCommandHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateInventoryItemCommandHandler _handler;

    public CreateInventoryItemCommandHandlerTests()
    {
        _handler = new CreateInventoryItemCommandHandler(_inventoryItemRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WhenProductHasNoItemYet_CreatesIt()
    {
        var productId = Guid.NewGuid();
        _inventoryItemRepository.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItem?)null);

        var result = await _handler.Handle(new CreateInventoryItemCommand(productId, 10), CancellationToken.None);

        result.ProductId.Should().Be(productId);
        result.AvailableQuantity.Should().Be(10);
        _inventoryItemRepository.Verify(r => r.AddAsync(It.Is<InventoryItem>(i => i.ProductId == productId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductAlreadyHasAnItem_ThrowsAndDoesNotPersist()
    {
        var productId = Guid.NewGuid();
        var existing = InventoryItem.Create(productId, 5);
        _inventoryItemRepository.Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var act = () => _handler.Handle(new CreateInventoryItemCommand(productId, 10), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateInventoryItemException>();
        _inventoryItemRepository.Verify(r => r.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
