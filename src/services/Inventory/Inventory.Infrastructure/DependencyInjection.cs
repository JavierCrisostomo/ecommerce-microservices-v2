using Inventory.Application.Abstractions;
using Inventory.Domain.Repositories;
using Inventory.Infrastructure.Messaging;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("InventoryDb")));

        services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
        services.AddScoped<IOrderReservationRepository, OrderReservationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<InventoryDbContext>(o => o.UseSqlServer());

            x.AddConsumer<OrderCreatedConsumer>();
            x.AddConsumer<PaymentFailedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqPort = ushort.Parse(configuration["RabbitMq:Port"] ?? "5672");
                cfg.Host(configuration["RabbitMq:Host"], rabbitMqPort, "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"]!);
                    h.Password(configuration["RabbitMq:Password"]!);
                });

                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromMilliseconds(200),
                    maxInterval: TimeSpan.FromSeconds(5),
                    intervalDelta: TimeSpan.FromMilliseconds(200)));

                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                    cb.TripThreshold = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval = TimeSpan.FromMinutes(5);
                });

                // Nombres de cola explícitos y prefijados por servicio (ver el comentario
                // equivalente en Orders.Infrastructure para el porqué).
                cfg.ReceiveEndpoint("inventory-order-created", e =>
                {
                    e.UseEntityFrameworkOutbox<InventoryDbContext>(context);
                    e.ConfigureConsumer<OrderCreatedConsumer>(context);
                });
                cfg.ReceiveEndpoint("inventory-payment-failed", e =>
                {
                    e.UseEntityFrameworkOutbox<InventoryDbContext>(context);
                    e.ConfigureConsumer<PaymentFailedConsumer>(context);
                });
            });
        });

        return services;
    }
}
