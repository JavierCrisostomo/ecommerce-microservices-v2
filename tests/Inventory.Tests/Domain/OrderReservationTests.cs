using FluentAssertions;
using Inventory.Domain.Entities;

namespace Inventory.Tests.Domain;

public class OrderReservationTests
{
    [Fact]
    public void Create_BuildsLinesAndStartsReserved()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var reservation = OrderReservation.Create(orderId, [new NewReservationLine(productId, 3)]);

        reservation.OrderId.Should().Be(orderId);
        reservation.Status.Should().Be(ReservationStatus.Reserved);
        reservation.Lines.Should().ContainSingle(l => l.ProductId == productId && l.Quantity == 3);
    }

    [Fact]
    public void Release_FirstTime_ReturnsTrueAndSetsReleased()
    {
        var reservation = OrderReservation.Create(Guid.NewGuid(), [new NewReservationLine(Guid.NewGuid(), 1)]);

        var result = reservation.Release();

        result.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Released);
    }

    [Fact]
    public void Release_WhenAlreadyReleased_IsIdempotentAndReturnsFalse()
    {
        var reservation = OrderReservation.Create(Guid.NewGuid(), [new NewReservationLine(Guid.NewGuid(), 1)]);
        reservation.Release();

        var result = reservation.Release();

        result.Should().BeFalse();
        reservation.Status.Should().Be(ReservationStatus.Released);
    }
}
