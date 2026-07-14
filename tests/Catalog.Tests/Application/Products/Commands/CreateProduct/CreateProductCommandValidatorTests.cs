using Catalog.Application.Products.Commands.CreateProduct;
using FluentValidation.TestHelper;

namespace Catalog.Tests.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateProductCommand("SKU-1", "Name", "Description", "Category", 10m));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptySku_HasError()
    {
        var result = _validator.TestValidate(new CreateProductCommand("", "Name", "Description", "Category", 10m));

        result.ShouldHaveValidationErrorFor(c => c.Sku);
    }

    [Fact]
    public void Validate_WithEmptyName_HasError()
    {
        var result = _validator.TestValidate(new CreateProductCommand("SKU-1", "", "Description", "Category", 10m));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Validate_WithEmptyCategory_HasError()
    {
        var result = _validator.TestValidate(new CreateProductCommand("SKU-1", "Name", "Description", "", 10m));

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WithNonPositivePrice_HasError(decimal price)
    {
        var result = _validator.TestValidate(new CreateProductCommand("SKU-1", "Name", "Description", "Category", price));

        result.ShouldHaveValidationErrorFor(c => c.Price);
    }
}
