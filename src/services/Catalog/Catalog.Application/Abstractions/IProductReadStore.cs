using Catalog.Application.Products;

namespace Catalog.Application.Abstractions;

public interface IProductReadStore
{
    Task UpsertAsync(ProductSummary summary, CancellationToken cancellationToken);

    Task<ProductSummary?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductSummary>> ListAsync(
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
