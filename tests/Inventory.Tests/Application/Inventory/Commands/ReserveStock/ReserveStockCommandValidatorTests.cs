using FluentValidation.TestHelper;
using Inventory.Application.Inventory.Commands.ReserveStock;

namespace Inventory.Tests.Application.Inventory.Commands.ReserveStock;

public class ReserveStockCommandValidatorTests
{
    private readonly ReserveStockCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var command = new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockLine(Guid.NewGuid(), 1)], 10m);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOrderId_HasError()
    {
        var command = new ReserveStockCommand(Guid.Empty, [new ReserveStockLine(Guid.NewGuid(), 1)], 10m);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OrderId);
    }

    [Fact]
    public void Validate_WithNoLines_HasError()
    {
        var command = new ReserveStockCommand(Guid.NewGuid(), [], 10m);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Lines);
    }

    [Fact]
    public void Validate_WithLineHavingNonPositiveQuantity_HasError()
    {
        var command = new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockLine(Guid.NewGuid(), 0)], 10m);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Lines[0].Quantity");
    }
}
