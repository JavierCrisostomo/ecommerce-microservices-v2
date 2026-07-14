using MediatR;

namespace Catalog.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductSummary?>;
