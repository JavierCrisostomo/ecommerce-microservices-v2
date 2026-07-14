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

                // Nombres de cola explícitos y prefijados por servicio (ver el comentario
                // equivalente en Orders.Infrastructure para el porqué).
                cfg.ReceiveEndpoint("inventory-order-created", e => e.ConfigureConsumer<OrderCreatedConsumer>(context));
                cfg.ReceiveEndpoint("inventory-payment-failed", e => e.ConfigureConsumer<PaymentFailedConsumer>(context));
            });
        });

        return services;
    }
}
