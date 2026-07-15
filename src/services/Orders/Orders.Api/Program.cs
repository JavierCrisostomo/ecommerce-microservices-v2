using System.Security.Claims;
using System.Text;
using ECommerce.Observability;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orders.Api.Contracts;
using Orders.Application;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.Application.Orders.Queries.GetOrderById;
using Orders.Application.Orders.Queries.ListOrdersByCustomer;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryTracing("orders-api");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Aplica las migraciones al arrancar: evita tener que correr `dotnet ef database
// update` a mano contra cada contenedor. Reintenta porque SQL Server puede tardar
// unos segundos más en aceptar conexiones aunque el healthcheck ya haya pasado.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
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
app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = feature?.Error;

        var (statusCode, title) = exception switch
        {
            ValidationException validationException => (StatusCodes.Status400BadRequest, string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))),
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, argumentException.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { title });
    });
});

var orders = app.MapGroup("/api/orders").RequireAuthorization();

orders.MapPost("/", async (CreateOrderRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
{
    var customerId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    var lines = request.Lines
        .Select(l => new CreateOrderLineRequest(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity))
        .ToList();

    var result = await sender.Send(new CreateOrderCommand(customerId, lines), cancellationToken);
    return Results.Created($"/api/orders/{result.OrderId}", result);
});

orders.MapGet("/{orderId:guid}", async (Guid orderId, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
{
    var customerId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    var order = await sender.Send(new GetOrderByIdQuery(orderId), cancellationToken);

    if (order is null || order.CustomerId != customerId)
        return Results.NotFound();

    return Results.Ok(order);
});

orders.MapGet("/", async (int page, int pageSize, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken) =>
{
    var customerId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    var query = new ListOrdersByCustomerQuery(customerId, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize);
    var result = await sender.Send(query, cancellationToken);
    return Results.Ok(result);
});

app.Run();

public partial class Program;
