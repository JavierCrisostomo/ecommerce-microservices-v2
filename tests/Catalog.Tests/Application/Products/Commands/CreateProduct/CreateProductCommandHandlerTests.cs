using Catalog.Application.Abstractions;
using Catalog.Application.Exceptions;
using Catalog.Application.Products;
using Catalog.Application.Products.Commands.CreateProduct;
using Catalog.Domain.Entities;
using Catalog.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Catalog.Tests.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IProductReadStore> _productReadStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(_productRepository.Object, _productReadStore.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WhenSkuIsFree_CreatesProductAndProjectsReadModel()
    {
        _productRepository.Setup(r => r.GetBySkuAsync("book-001", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var command = new CreateProductCommand("book-001", "Clean Architecture", "Un libro", "Books", 45.90m);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Sku.Should().Be("BOOK-001");
        result.ProductId.Should().NotBeEmpty();

        _productRepository.Verify(r => r.AddAsync(It.Is<Product>(p => p.Sku == "BOOK-001"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _productReadStore.Verify(s => s.UpsertAsync(
            It.Is<ProductSummary>(p => p.Sku == "BOOK-001" && p.Name == "Clean Architecture"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSkuAlreadyExists_ThrowsAndDoesNotPersist()
    {
        var existing = Product.Create("BOOK-001", "Existing", "Existing", "Books", 10m);
        _productRepository.Setup(r => r.GetBySkuAsync("book-001", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var command = new CreateProductCommand("book-001", "Clean Architecture", "Un libro", "Books", 45.90m);
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateSkuException>();
        _productRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _productReadStore.Verify(s => s.UpsertAsync(It.IsAny<ProductSummary>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
