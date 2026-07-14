using Catalog.Api.Contracts;
using Catalog.Application;
using Catalog.Application.Exceptions;
using Catalog.Application.Products.Commands.CreateProduct;
using Catalog.Application.Products.Queries.GetProductById;
using Catalog.Application.Products.Queries.ListProducts;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
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
            DuplicateSkuException duplicateEx => (StatusCodes.Status409Conflict, duplicateEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { title });
    });
});

var products = app.MapGroup("/api/products");

products.MapPost("/", async (CreateProductRequest request, ISender sender, CancellationToken cancellationToken) =>
{
    var command = new CreateProductCommand(request.Sku, request.Name, request.Description, request.Category, request.Price);
    var result = await sender.Send(command, cancellationToken);
    return Results.Created($"/api/products/{result.ProductId}", result);
});

products.MapGet("/{productId:guid}", async (Guid productId, ISender sender, CancellationToken cancellationToken) =>
{
    var product = await sender.Send(new GetProductByIdQuery(productId), cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

products.MapGet("/", async (string? category, int page, int pageSize, ISender sender, CancellationToken cancellationToken) =>
{
    var query = new ListProductsQuery(category, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
    var result = await sender.Send(query, cancellationToken);
    return Results.Ok(result);
});

app.Run();

public partial class Program;
