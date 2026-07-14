using Catalog.Domain.Entities;
using Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        return dbContext.Products.SingleOrDefaultAsync(p => p.Sku == normalizedSku, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await dbContext.Products.AddAsync(product, cancellationToken);
    }
}
