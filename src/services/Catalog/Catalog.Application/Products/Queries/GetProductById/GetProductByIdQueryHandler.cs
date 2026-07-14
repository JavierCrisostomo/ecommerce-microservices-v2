using Catalog.Application.Abstractions;
using MediatR;

namespace Catalog.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(IProductReadStore productReadStore)
    : IRequestHandler<GetProductByIdQuery, ProductSummary?>
{
    public Task<ProductSummary?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        => productReadStore.GetByIdAsync(request.ProductId, cancellationToken);
}
