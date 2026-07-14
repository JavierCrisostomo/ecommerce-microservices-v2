using Catalog.Application.Abstractions;
using Catalog.Application.Exceptions;
using Catalog.Domain.Entities;
using Catalog.Domain.Repositories;
using MediatR;

namespace Catalog.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository productRepository,
    IProductReadStore productReadStore,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var existing = await productRepository.GetBySkuAsync(request.Sku, cancellationToken);
        if (existing is not null)
            throw new DuplicateSkuException(request.Sku);

        var product = Product.Create(request.Sku, request.Name, request.Description, request.Category, request.Price);

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Sin bus de eventos todavía (llega en la fase de mensajería): la proyección de
        // lectura se actualiza sincrónicamente, justo después de confirmar el write model.
        await productReadStore.UpsertAsync(
            new ProductSummary(product.Id, product.Sku, product.Name, product.Category, product.Price, product.CreatedAt),
            cancellationToken);

        return new CreateProductResult(product.Id, product.Sku);
    }
}
