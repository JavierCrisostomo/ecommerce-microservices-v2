using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Abstractions;
using Payments.Domain.Repositories;
using Payments.Infrastructure.Gateways;
using Payments.Infrastructure.Messaging;
using Payments.Infrastructure.Messaging.Consumers;
using Payments.Infrastructure.Persistence;

namespace Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("PaymentsDb")));

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<StockReservedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqPort = ushort.Parse(configuration["RabbitMq:Port"] ?? "5672");
                cfg.Host(configuration["RabbitMq:Host"], rabbitMqPort, "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"]!);
                    h.Password(configuration["RabbitMq:Password"]!);
                });

                // Nombre de cola explícito y prefijado por servicio (ver el comentario
                // equivalente en Orders.Infrastructure para el porqué).
                cfg.ReceiveEndpoint("payments-stock-reserved", e => e.ConfigureConsumer<StockReservedConsumer>(context));
            });
        });

        return services;
    }
}
