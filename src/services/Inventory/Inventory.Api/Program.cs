using ECommerce.Observability;
using FluentValidation;
using Inventory.Api.Contracts;
using Inventory.Application;
using Inventory.Application.Exceptions;
using Inventory.Application.Inventory.Commands.CreateInventoryItem;
using Inventory.Application.Inventory.Queries.GetStockByProductId;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryTracing("inventory-api");

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
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
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

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = feature?.Error;

        var (statusCode, title) = exception switch
        {
            ValidationException validationException => (StatusCodes.Status400BadRequest, string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))),
            DuplicateInventoryItemException duplicateEx => (StatusCodes.Status409Conflict, duplicateEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { title });
    });
});

var inventory = app.MapGroup("/api/inventory");

inventory.MapPost("/", async (CreateInventoryItemRequest request, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new CreateInventoryItemCommand(request.ProductId, request.InitialQuantity), cancellationToken);
    return Results.Created($"/api/inventory/{result.ProductId}", result);
});

inventory.MapGet("/{productId:guid}", async (Guid productId, ISender sender, CancellationToken cancellationToken) =>
{
    var stock = await sender.Send(new GetStockByProductIdQuery(productId), cancellationToken);
    return stock is null ? Results.NotFound() : Results.Ok(stock);
});

app.Run();

public partial class Program;
