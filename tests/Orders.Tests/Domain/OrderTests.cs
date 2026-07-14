using FluentAssertions;
using Orders.Domain.Entities;

namespace Orders.Tests.Domain;

public class OrderTests
{
    private static List<NewOrderLine> OneLine(int quantity = 2, decimal unitPrice = 10m)
        => [new NewOrderLine(Guid.NewGuid(), "Producto", unitPrice, quantity)];

    [Fact]
    public void Create_WithLines_ComputesTotalAndStartsPending()
    {
        var lines = new List<NewOrderLine>
        {
            new(Guid.NewGuid(), "Producto A", 10m, 2),
            new(Guid.NewGuid(), "Producto B", 5m, 3)
        };

        var order = Order.Create(Guid.NewGuid(), lines);

        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(35m); // 10*2 + 5*3
        order.Lines.Should().HaveCount(2);
        order.CancellationReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithNoLines_Throws()
    {
        var act = () => Order.Create(Guid.NewGuid(), []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNonPositiveQuantity_Throws()
    {
        var act = () => Order.Create(Guid.NewGuid(), OneLine(quantity: 0));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNonPositiveUnitPrice_Throws()
    {
        var act = () => Order.Create(Guid.NewGuid(), OneLine(unitPrice: 0));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Confirm_FromPending_MovesToConfirmed()
    {
        var order = Order.Create(Guid.NewGuid(), OneLine());

        order.Confirm();

        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_IsIdempotent()
    {
        var order = Order.Create(Guid.NewGuid(), OneLine());
        order.Confirm();

        var act = () => order.Confirm();

        act.Should().NotThrow();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void Confirm_WhenCancelled_Throws()
    {
        var order = Order.Create(Guid.NewGuid(), OneLine());
        order.Cancel("sin stock");

        var act = () => order.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromPending_MovesToCancelledWithReason()
    {
        var order = Order.Create(Guid.NewGuid(), OneLine());

        order.Cancel("Stock insuficiente");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Stock insuficiente");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_IsIdempotentAndKeepsFirstReason()
    {
        var order = Order.Create(Guid.NewGuid(), OneLine());
        order.Cancel("Primer motivo");

        var act = () => order.Cancel("Segundo motivo");

        act.Should().NotThrow();
        order.CancellationReason.Should().Be("Primer motivo");
    }

    [Fact]
    public void Cancel_WhenConfirmed_Throws()
    {
        var order = Order.Create(Guid.NewGuid(), OneLine());
        order.Confirm();

        var act = () => order.Cancel("demasiado tarde");

        act.Should().Throw<InvalidOperationException>();
    }
}
