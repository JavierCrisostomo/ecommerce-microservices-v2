using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Abstractions;
using Orders.Domain.Repositories;
using Orders.Infrastructure.Messaging;
using Orders.Infrastructure.Messaging.Consumers;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("OrdersDb")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderReadStore, OrderReadStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });

            x.AddConsumer<PaymentCompletedConsumer>();
            x.AddConsumer<PaymentFailedConsumer>();
            x.AddConsumer<StockRejectedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqPort = ushort.Parse(configuration["RabbitMq:Port"] ?? "5672");
                cfg.Host(configuration["RabbitMq:Host"], rabbitMqPort, "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"]!);
                    h.Password(configuration["RabbitMq:Password"]!);
                });

                // Nombres de cola explícitos y prefijados por servicio: el nombre por defecto
                // de MassTransit sale del nombre de la clase del consumer, y como Orders e
                // Inventory tienen cada uno un "PaymentFailedConsumer", sin esto terminan
                // compitiendo por la misma cola en lugar de recibir cada uno su propia copia.
                cfg.ReceiveEndpoint("orders-payment-completed", e =>
                {
                    e.UseEntityFrameworkOutbox<OrdersDbContext>(context);
                    e.ConfigureConsumer<PaymentCompletedConsumer>(context);
                });
                cfg.ReceiveEndpoint("orders-payment-failed", e =>
                {
                    e.UseEntityFrameworkOutbox<OrdersDbContext>(context);
                    e.ConfigureConsumer<PaymentFailedConsumer>(context);
                });
                cfg.ReceiveEndpoint("orders-stock-rejected", e =>
                {
                    e.UseEntityFrameworkOutbox<OrdersDbContext>(context);
                    e.ConfigureConsumer<StockRejectedConsumer>(context);
                });
            });
        });

        return services;
    }
}
