using FluentValidation.TestHelper;
using Inventory.Application.Inventory.Commands.CreateInventoryItem;

namespace Inventory.Tests.Application.Inventory.Commands.CreateInventoryItem;

public class CreateInventoryItemCommandValidatorTests
{
    private readonly CreateInventoryItemCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateInventoryItemCommand(Guid.NewGuid(), 10));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyProductId_HasError()
    {
        var result = _validator.TestValidate(new CreateInventoryItemCommand(Guid.Empty, 10));

        result.ShouldHaveValidationErrorFor(c => c.ProductId);
    }

    [Fact]
    public void Validate_WithNegativeQuantity_HasError()
    {
        var result = _validator.TestValidate(new CreateInventoryItemCommand(Guid.NewGuid(), -1));

        result.ShouldHaveValidationErrorFor(c => c.InitialQuantity);
    }
}
