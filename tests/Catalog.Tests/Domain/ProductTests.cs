using Catalog.Domain.Entities;
using FluentAssertions;

namespace Catalog.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_NormalizesSkuAndTrimsFields()
    {
        var product = Product.Create("  book-001  ", "  Clean Architecture  ", "  Un libro  ", "  Books  ", 45.90m);

        product.Sku.Should().Be("BOOK-001");
        product.Name.Should().Be("Clean Architecture");
        product.Description.Should().Be("Un libro");
        product.Category.Should().Be("Books");
        product.Price.Should().Be(45.90m);
        product.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutSku_Throws(string sku)
    {
        var act = () => Product.Create(sku, "Name", "Description", "Category", 10m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_Throws(string name)
    {
        var act = () => Product.Create("SKU-1", name, "Description", "Category", 10m);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WithNonPositivePrice_Throws(decimal price)
    {
        var act = () => Product.Create("SKU-1", "Name", "Description", "Category", price);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
