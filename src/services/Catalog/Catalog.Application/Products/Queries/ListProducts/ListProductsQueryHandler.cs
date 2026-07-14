using Catalog.Application.Abstractions;
using MediatR;

namespace Catalog.Application.Products.Queries.ListProducts;

public class ListProductsQueryHandler(IProductReadStore productReadStore)
    : IRequestHandler<ListProductsQuery, IReadOnlyList<ProductSummary>>
{
    public Task<IReadOnlyList<ProductSummary>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
        => productReadStore.ListAsync(request.Category, request.Page, request.PageSize, cancellationToken);
}
