using Catalog.Application.Abstractions;
using Catalog.Domain.Repositories;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("CatalogDb")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductReadStore, ProductReadStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
