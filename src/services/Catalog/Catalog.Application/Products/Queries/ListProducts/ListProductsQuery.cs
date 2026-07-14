using MediatR;

namespace Catalog.Application.Products.Queries.ListProducts;

public record ListProductsQuery(string? Category, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<ProductSummary>>;
