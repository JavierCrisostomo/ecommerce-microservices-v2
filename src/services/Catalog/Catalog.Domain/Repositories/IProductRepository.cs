using Catalog.Domain.Entities;

namespace Catalog.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);
}
