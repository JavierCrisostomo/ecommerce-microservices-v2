using Catalog.Application.Abstractions;
using Catalog.Application.Products;
using Catalog.Application.Products.Queries.ListProducts;
using FluentAssertions;
using Moq;

namespace Catalog.Tests.Application.Products.Queries.ListProducts;

public class ListProductsQueryHandlerTests
{
    private readonly Mock<IProductReadStore> _productReadStore = new();
    private readonly ListProductsQueryHandler _handler;

    public ListProductsQueryHandlerTests()
    {
        _handler = new ListProductsQueryHandler(_productReadStore.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToReadStoreWithGivenParameters()
    {
        var expected = new List<ProductSummary>
        {
            new(Guid.NewGuid(), "SKU-1", "Name", "Books", 10m, DateTimeOffset.UtcNow)
        };
        _productReadStore.Setup(s => s.ListAsync("Books", 2, 10, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var result = await _handler.Handle(new ListProductsQuery("Books", 2, 10), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        _productReadStore.Verify(s => s.ListAsync("Books", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
