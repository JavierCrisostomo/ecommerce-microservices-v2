using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Payments.Application;
using Payments.Application.Payments.Queries.GetPaymentByOrderId;
using Payments.Infrastructure;
using Payments.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

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
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
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

app.MapGet("/api/payments/order/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
{
    var payment = await sender.Send(new GetPaymentByOrderIdQuery(orderId), cancellationToken);
    return payment is null ? Results.NotFound() : Results.Ok(payment);
}).RequireAuthorization();

app.Run();

public partial class Program;
