using Catalog.Application.Products.Queries.ListProducts;
using FluentValidation.TestHelper;

namespace Catalog.Tests.Application.Products.Queries.ListProducts;

public class ListProductsQueryValidatorTests
{
    private readonly ListProductsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaults_HasNoErrors()
    {
        var result = _validator.TestValidate(new ListProductsQuery(null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithPageBelowOne_HasError()
    {
        var result = _validator.TestValidate(new ListProductsQuery(null, 0, 20));

        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_HasError(int pageSize)
    {
        var result = _validator.TestValidate(new ListProductsQuery(null, 1, pageSize));

        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }
}
