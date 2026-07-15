using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Domain.Repositories;
using Notifications.Infrastructure.Messaging.Consumers;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("NotificationsDb")));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderConfirmedConsumer>();
            x.AddConsumer<OrderCancelledConsumer>();

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
                cfg.ReceiveEndpoint("notifications-order-confirmed", e => e.ConfigureConsumer<OrderConfirmedConsumer>(context));
                cfg.ReceiveEndpoint("notifications-order-cancelled", e => e.ConfigureConsumer<OrderCancelledConsumer>(context));
            });
        });

        return services;
    }
}
