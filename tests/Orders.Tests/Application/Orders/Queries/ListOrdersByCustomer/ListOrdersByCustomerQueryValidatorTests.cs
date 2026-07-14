using FluentValidation.TestHelper;
using Orders.Application.Orders.Queries.ListOrdersByCustomer;

namespace Orders.Tests.Application.Orders.Queries.ListOrdersByCustomer;

public class ListOrdersByCustomerQueryValidatorTests
{
    private readonly ListOrdersByCustomerQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var result = _validator.TestValidate(new ListOrdersByCustomerQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyCustomerId_HasError()
    {
        var result = _validator.TestValidate(new ListOrdersByCustomerQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(q => q.CustomerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_HasError(int pageSize)
    {
        var result = _validator.TestValidate(new ListOrdersByCustomerQuery(Guid.NewGuid(), 1, pageSize));

        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }
}
