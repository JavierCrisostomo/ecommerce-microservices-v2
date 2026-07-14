using FluentValidation.TestHelper;
using Payments.Application.Payments.Commands.ProcessPayment;

namespace Payments.Tests.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var result = _validator.TestValidate(new ProcessPaymentCommand(Guid.NewGuid(), 50m));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOrderId_HasError()
    {
        var result = _validator.TestValidate(new ProcessPaymentCommand(Guid.Empty, 50m));

        result.ShouldHaveValidationErrorFor(c => c.OrderId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveAmount_HasError(decimal amount)
    {
        var result = _validator.TestValidate(new ProcessPaymentCommand(Guid.NewGuid(), amount));

        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }
}
