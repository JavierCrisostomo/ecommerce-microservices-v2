using Catalog.Application.Abstractions;
using Catalog.Application.Products;
using Catalog.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class ProductReadStore(CatalogDbContext dbContext) : IProductReadStore
{
    public async Task UpsertAsync(ProductSummary summary, CancellationToken cancellationToken)
    {
        var readModel = await dbContext.ProductReadModels
            .SingleOrDefaultAsync(p => p.Id == summary.Id, cancellationToken);

        if (readModel is null)
        {
            await dbContext.ProductReadModels.AddAsync(new ProductReadModel
            {
                Id = summary.Id,
                Sku = summary.Sku,
                Name = summary.Name,
                Category = summary.Category,
                Price = summary.Price,
                CreatedAt = summary.CreatedAt
            }, cancellationToken);
        }
        else
        {
            readModel.Sku = summary.Sku;
            readModel.Name = summary.Name;
            readModel.Category = summary.Category;
            readModel.Price = summary.Price;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductSummary?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var readModel = await dbContext.ProductReadModels
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);

        return readModel is null ? null : ToSummary(readModel);
    }

    public async Task<IReadOnlyList<ProductSummary>> ListAsync(
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ProductReadModels.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        var readModels = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return readModels.Select(ToSummary).ToList();
    }

    private static ProductSummary ToSummary(ProductReadModel readModel)
        => new(readModel.Id, readModel.Sku, readModel.Name, readModel.Category, readModel.Price, readModel.CreatedAt);
}
