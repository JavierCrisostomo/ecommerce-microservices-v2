using Catalog.Application.Abstractions;
using Catalog.Application.Products;
using Catalog.Application.Products.Queries.GetProductById;
using FluentAssertions;
using Moq;

namespace Catalog.Tests.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductReadStore> _productReadStore = new();
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _handler = new GetProductByIdQueryHandler(_productReadStore.Object);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ReturnsSummary()
    {
        var productId = Guid.NewGuid();
        var summary = new ProductSummary(productId, "SKU-1", "Name", "Category", 10m, DateTimeOffset.UtcNow);
        _productReadStore.Setup(s => s.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(summary);

        var result = await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.Should().Be(summary);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ReturnsNull()
    {
        var productId = Guid.NewGuid();
        _productReadStore.Setup(s => s.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync((ProductSummary?)null);

        var result = await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.Should().BeNull();
    }
}
