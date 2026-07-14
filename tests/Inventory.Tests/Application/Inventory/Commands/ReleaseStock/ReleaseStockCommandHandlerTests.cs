using FluentAssertions;
using Inventory.Application.Abstractions;
using Inventory.Application.Inventory.Commands.ReleaseStock;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Moq;

namespace Inventory.Tests.Application.Inventory.Commands.ReleaseStock;

public class ReleaseStockCommandHandlerTests
{
    private readonly Mock<IOrderReservationRepository> _reservationRepository = new();
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ReleaseStockCommandHandler _handler;

    public ReleaseStockCommandHandlerTests()
    {
        _handler = new ReleaseStockCommandHandler(_reservationRepository.Object, _inventoryItemRepository.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WhenReservationExists_ReleasesStockBackForEachLine()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var reservation = OrderReservation.Create(orderId, [new NewReservationLine(productId, 3)]);
        var item = InventoryItem.Create(productId, 2); // ya reservado, quedó en 2

        _reservationRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        _inventoryItemRepository.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        await _handler.Handle(new ReleaseStockCommand(orderId), CancellationToken.None);

        item.AvailableQuantity.Should().Be(5);
        reservation.Status.Should().Be(ReservationStatus.Released);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReservationDoesNotExist_DoesNothing()
    {
        var orderId = Guid.NewGuid();
        _reservationRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((OrderReservation?)null);

        await _handler.Handle(new ReleaseStockCommand(orderId), CancellationToken.None);

        _inventoryItemRepository.Verify(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReservationAlreadyReleased_IsIdempotentAndDoesNotDoubleRelease()
    {
        var orderId = Guid.NewGuid();
        var reservation = OrderReservation.Create(orderId, [new NewReservationLine(Guid.NewGuid(), 3)]);
        reservation.Release();

        _reservationRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

        await _handler.Handle(new ReleaseStockCommand(orderId), CancellationToken.None);

        _inventoryItemRepository.Verify(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
