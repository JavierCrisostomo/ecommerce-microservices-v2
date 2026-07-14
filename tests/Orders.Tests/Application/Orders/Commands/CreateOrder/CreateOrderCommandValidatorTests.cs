using FluentValidation.TestHelper;
using Orders.Application.Orders.Commands.CreateOrder;

namespace Orders.Tests.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderLineRequest(Guid.NewGuid(), "Producto", 10m, 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCustomerId_HasError()
    {
        var command = new CreateOrderCommand(Guid.Empty, [new CreateOrderLineRequest(Guid.NewGuid(), "Producto", 10m, 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CustomerId);
    }

    [Fact]
    public void Validate_WithNoLines_HasError()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Lines);
    }

    [Fact]
    public void Validate_WithLineHavingNonPositiveQuantity_HasError()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderLineRequest(Guid.NewGuid(), "Producto", 10m, 0)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Lines[0].Quantity");
    }

    [Fact]
    public void Validate_WithLineHavingNonPositiveUnitPrice_HasError()
    {
        var command = new CreateOrderCommand(Guid.NewGuid(), [new CreateOrderLineRequest(Guid.NewGuid(), "Producto", 0m, 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Lines[0].UnitPrice");
    }
}
