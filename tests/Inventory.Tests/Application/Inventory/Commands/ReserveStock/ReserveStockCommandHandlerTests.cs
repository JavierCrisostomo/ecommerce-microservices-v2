using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using Inventory.Application.Abstractions;
using Inventory.Application.Inventory.Commands.ReserveStock;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Moq;

namespace Inventory.Tests.Application.Inventory.Commands.ReserveStock;

public class ReserveStockCommandHandlerTests
{
    private readonly Mock<IOrderReservationRepository> _reservationRepository = new();
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly ReserveStockCommandHandler _handler;

    public ReserveStockCommandHandlerTests()
    {
        _handler = new ReserveStockCommandHandler(
            _reservationRepository.Object,
            _inventoryItemRepository.Object,
            _unitOfWork.Object,
            _eventPublisher.Object);

        _reservationRepository.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderReservation?)null);
    }

    [Fact]
    public async Task Handle_WhenReservationAlreadyExists_IsIdempotentAndDoesNothing()
    {
        var orderId = Guid.NewGuid();
        var existing = OrderReservation.Create(orderId, [new NewReservationLine(Guid.NewGuid(), 1)]);
        _reservationRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var command = new ReserveStockCommand(orderId, [new ReserveStockLine(Guid.NewGuid(), 1)], 10m);
        await _handler.Handle(command, CancellationToken.None);

        _inventoryItemRepository.Verify(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<StockReserved>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<StockRejected>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStockIsSufficientForAllLines_ReservesAllAndPublishesStockReserved()
    {
        var orderId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var itemA = InventoryItem.Create(productA, 10);
        var itemB = InventoryItem.Create(productB, 5);

        _inventoryItemRepository.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([itemA, itemB]);

        var command = new ReserveStockCommand(
            orderId,
            [new ReserveStockLine(productA, 4), new ReserveStockLine(productB, 5)],
            75m);

        await _handler.Handle(command, CancellationToken.None);

        itemA.AvailableQuantity.Should().Be(6);
        itemB.AvailableQuantity.Should().Be(0);

        _reservationRepository.Verify(r => r.AddAsync(It.Is<OrderReservation>(res => res.OrderId == orderId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<StockReserved>(e => e.OrderId == orderId && e.TotalAmount == 75m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStockIsInsufficientForOneLine_RejectsAndReservesNothing()
    {
        var orderId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var itemA = InventoryItem.Create(productA, 10);
        var itemB = InventoryItem.Create(productB, 2); // insuficiente para la línea de 5

        _inventoryItemRepository.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([itemA, itemB]);

        var command = new ReserveStockCommand(
            orderId,
            [new ReserveStockLine(productA, 4), new ReserveStockLine(productB, 5)],
            75m);

        await _handler.Handle(command, CancellationToken.None);

        // Todo o nada: ni siquiera la línea que sí alcanzaba debe reservarse.
        itemA.AvailableQuantity.Should().Be(10);
        itemB.AvailableQuantity.Should().Be(2);

        _reservationRepository.Verify(r => r.AddAsync(It.IsAny<OrderReservation>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<StockRejected>(e => e.OrderId == orderId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductHasNoInventoryItemAtAll_TreatsAsInsufficientStock()
    {
        var orderId = Guid.NewGuid();
        var unknownProduct = Guid.NewGuid();

        _inventoryItemRepository.Setup(r => r.GetByProductIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var command = new ReserveStockCommand(orderId, [new ReserveStockLine(unknownProduct, 1)], 10m);

        await _handler.Handle(command, CancellationToken.None);

        _eventPublisher.Verify(p => p.PublishAsync(It.Is<StockRejected>(e => e.OrderId == orderId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
