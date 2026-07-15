using ECommerce.Observability;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notifications.Application;
using Notifications.Application.Notifications.Queries.GetNotificationsByOrderId;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryTracing("notifications-api");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Aplica las migraciones al arrancar: evita tener que correr `dotnet ef database
// update` a mano contra cada contenedor. Reintenta porque SQL Server puede tardar
// unos segundos más en aceptar conexiones aunque el healthcheck ya haya pasado.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            dbContext.Database.Migrate();
            break;
        }
        catch when (attempt < 10)
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/notifications/order/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
{
    var notifications = await sender.Send(new GetNotificationsByOrderIdQuery(orderId), cancellationToken);
    return Results.Ok(notifications);
});

app.Run();

public partial class Program;
