using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductReadModel> ProductReadModels => Set<ProductReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
