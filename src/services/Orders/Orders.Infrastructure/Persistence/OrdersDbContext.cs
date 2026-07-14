using Orders.Domain.Entities;
using Orders.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Persistence;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderReadModel> OrderReadModels => Set<OrderReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
