using MediatR;

namespace Catalog.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Sku,
    string Name,
    string Description,
    string Category,
    decimal Price) : IRequest<CreateProductResult>;

public record CreateProductResult(Guid ProductId, string Sku);
