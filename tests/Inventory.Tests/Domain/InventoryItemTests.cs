using FluentAssertions;
using Inventory.Domain.Entities;

namespace Inventory.Tests.Domain;

public class InventoryItemTests
{
    [Fact]
    public void Create_WithValidQuantity_SetsAvailableQuantity()
    {
        var productId = Guid.NewGuid();

        var item = InventoryItem.Create(productId, 10);

        item.ProductId.Should().Be(productId);
        item.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public void Create_WithNegativeQuantity_Throws()
    {
        var act = () => InventoryItem.Create(Guid.NewGuid(), -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TryReserve_WhenEnoughStock_DecrementsAndReturnsTrue()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), 10);

        var result = item.TryReserve(4);

        result.Should().BeTrue();
        item.AvailableQuantity.Should().Be(6);
    }

    [Fact]
    public void TryReserve_WhenNotEnoughStock_ReturnsFalseAndLeavesQuantityUnchanged()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), 3);

        var result = item.TryReserve(4);

        result.Should().BeFalse();
        item.AvailableQuantity.Should().Be(3);
    }

    [Fact]
    public void TryReserve_WithNonPositiveQuantity_Throws()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), 10);

        var act = () => item.TryReserve(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Release_IncrementsAvailableQuantity()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), 5);
        item.TryReserve(5);

        item.Release(5);

        item.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public void Release_WithNonPositiveQuantity_Throws()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), 10);

        var act = () => item.Release(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
